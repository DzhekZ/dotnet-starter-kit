using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing;
using FSH.Modules.Auditing.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Auditing.Tests.Core;

/// <summary>
/// A sink that persisted part of a batch (one tenant's database down, the others fine) reports it with
/// <see cref="AuditSinkPartialFailureException"/>. The worker must then retry and dead-letter ONLY the envelopes
/// that were not written: re-sending the persisted ones inserts them twice (duplicate keys), which used to push
/// every healthy tenant's events into the DLQ along with the broken tenant's.
/// </summary>
public sealed class AuditBackgroundWorkerTests
{
    private static readonly TimeSpan WaitBudget = TimeSpan.FromSeconds(15);

    #region Partial failure

    [Fact]
    public async Task ExecuteAsync_Should_ResendOnlyFailedEnvelopes_When_SinkReportsPartialFailure()
    {
        // Arrange
        var written = NewEnvelope("tenant-a");
        var failed = NewEnvelope("tenant-b");
        var sink = new RecordingSink((call, batch) => call == 1 ? PartialFailure(batch, failed) : null);
        var dlq = new RecordingDlq();
        await using var harness = new Harness(sink, dlq, batchSize: 2);
        await harness.StartAsync();

        // Act
        await harness.PublishAsync(written, failed);
        await WaitUntilAsync(() => sink.Calls.Count >= 2, "the second sink call");

        // Assert
        sink.Calls.Count.ShouldBe(2);
        sink.Calls[0].ShouldBe([written.Id, failed.Id]);
        sink.Calls[1].ShouldBe([failed.Id]);
        dlq.Batches.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_DeadLetterOnlyFailedEnvelopes_When_PartialFailurePersistsAcrossRetries()
    {
        // Arrange
        var written = NewEnvelope("tenant-a");
        var failed = NewEnvelope("tenant-b");
        var sink = new RecordingSink((_, batch) => PartialFailure(batch, failed));
        var dlq = new RecordingDlq();
        await using var harness = new Harness(sink, dlq, batchSize: 2);
        await harness.StartAsync();

        // Act
        await harness.PublishAsync(written, failed);
        await WaitUntilAsync(() => dlq.Batches.Count >= 1, "the dead-letter write");

        // Assert
        sink.Calls.Count.ShouldBe(3);
        sink.Calls[0].ShouldBe([written.Id, failed.Id]);
        sink.Calls[1].ShouldBe([failed.Id]);
        sink.Calls[2].ShouldBe([failed.Id]);
        dlq.Batches.Count.ShouldBe(1);
        dlq.Batches[0].ShouldBe([failed.Id]);
    }

    #endregion

    #region Whole-batch failure

    [Fact]
    public async Task ExecuteAsync_Should_ResendWholeBatch_When_SinkThrowsPlainException()
    {
        // Arrange
        var first = NewEnvelope("tenant-a");
        var second = NewEnvelope("tenant-b");
        var sink = new RecordingSink((call, _) => call == 1 ? new InvalidOperationException("database unavailable") : null);
        var dlq = new RecordingDlq();
        await using var harness = new Harness(sink, dlq, batchSize: 2);
        await harness.StartAsync();

        // Act
        await harness.PublishAsync(first, second);
        await WaitUntilAsync(() => sink.Calls.Count >= 2, "the second sink call");

        // Assert
        sink.Calls[0].ShouldBe([first.Id, second.Id]);
        sink.Calls[1].ShouldBe([first.Id, second.Id]);
        dlq.Batches.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_DeadLetterWholeBatch_When_SinkKeepsThrowingPlainException()
    {
        // Arrange
        var first = NewEnvelope("tenant-a");
        var second = NewEnvelope("tenant-b");
        var sink = new RecordingSink((_, _) => new InvalidOperationException("database unavailable"));
        var dlq = new RecordingDlq();
        await using var harness = new Harness(sink, dlq, batchSize: 2);
        await harness.StartAsync();

        // Act
        await harness.PublishAsync(first, second);
        await WaitUntilAsync(() => dlq.Batches.Count >= 1, "the dead-letter write");

        // Assert
        sink.Calls.Count.ShouldBe(3);
        dlq.Batches.Count.ShouldBe(1);
        dlq.Batches[0].ShouldBe([first.Id, second.Id]);
    }

    #endregion

    #region Helpers

    private static AuditEnvelope NewEnvelope(string tenantId) => new(
        id: Guid.CreateVersion7(),
        occurredAtUtc: DateTime.UtcNow,
        receivedAtUtc: DateTime.UtcNow,
        eventType: AuditEventType.Activity,
        severity: AuditSeverity.Information,
        tenantId: tenantId,
        userId: null,
        userName: null,
        traceId: "trace",
        spanId: "span",
        correlationId: null,
        requestId: null,
        source: "worker-tests",
        tags: AuditTag.None,
        payload: new object());

    private static AuditSinkPartialFailureException PartialFailure(
        IReadOnlyList<AuditEnvelope> batch, params AuditEnvelope[] failed)
    {
        Guid[] failedIds = [.. failed.Select(e => e.Id)];
        AuditEnvelope[] notWritten = [.. batch.Where(e => failedIds.Contains(e.Id))];
        return new AuditSinkPartialFailureException(
            "tenant database unavailable", notWritten, new InvalidOperationException("tenant database unavailable"));
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string what)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        budget.CancelAfter(WaitBudget);
        try
        {
            while (!condition())
            {
                await Task.Delay(10, budget.Token);
            }
        }
        catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out after {WaitBudget.TotalSeconds:0}s waiting for {what}.");
        }
    }

    /// <summary>Runs a real worker over a real publisher; only the sink and the DLQ are test doubles.</summary>
    private sealed class Harness : IAsyncDisposable
    {
        private readonly ChannelAuditPublisher _publisher;
        private readonly AuditBackgroundWorker _worker;

        public Harness(IAuditSink sink, IAuditDlqSink dlq, int batchSize)
        {
            _publisher = new ChannelAuditPublisher(
                new HttpContextAccessor(),
                Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>(),
                TimeProvider.System);

            // A flush interval far longer than any test: only reaching the batch size triggers a flush,
            // so every test sees exactly the batch composition it published.
            _worker = new AuditBackgroundWorker(
                _publisher, sink, dlq, NullLogger<AuditBackgroundWorker>.Instance,
                batchSize, flushIntervalMs: 600_000);
        }

        public Task StartAsync() => _worker.StartAsync(TestContext.Current.CancellationToken);

        public async Task PublishAsync(params AuditEnvelope[] events)
        {
            foreach (var envelope in events)
            {
                await _publisher.PublishAsync(envelope, TestContext.Current.CancellationToken);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _worker.StopAsync(CancellationToken.None);
            _worker.Dispose();
        }
    }

    private sealed class RecordingSink(Func<int, IReadOnlyList<AuditEnvelope>, Exception?> script) : IAuditSink
    {
        private readonly object _gate = new();
        private readonly List<Guid[]> _calls = [];

        public IReadOnlyList<Guid[]> Calls
        {
            get
            {
                lock (_gate)
                {
                    return [.. _calls];
                }
            }
        }

        public Task WriteAsync(IReadOnlyList<AuditEnvelope> batch, CancellationToken ct = default)
        {
            int call;
            lock (_gate)
            {
                _calls.Add([.. batch.Select(e => e.Id)]);
                call = _calls.Count;
            }

            var failure = script(call, batch);
            return failure is null ? Task.CompletedTask : Task.FromException(failure);
        }
    }

    private sealed class RecordingDlq : IAuditDlqSink
    {
        private readonly object _gate = new();
        private readonly List<Guid[]> _batches = [];

        public IReadOnlyList<Guid[]> Batches
        {
            get
            {
                lock (_gate)
                {
                    return [.. _batches];
                }
            }
        }

        public Task WriteAsync(IReadOnlyList<AuditEnvelope> batch, CancellationToken ct = default)
        {
            lock (_gate)
            {
                _batches.Add([.. batch.Select(e => e.Id)]);
            }

            return Task.CompletedTask;
        }
    }

    #endregion
}

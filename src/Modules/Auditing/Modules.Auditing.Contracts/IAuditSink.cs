namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Destination for audit events (e.g., SQL, file, OTLP). Implementations must be efficient and batch-friendly.
/// </summary>
public interface IAuditSink
{
    /// <summary>
    /// Persists <paramref name="batch"/>. A thrown exception means the batch failed and the caller retries it whole;
    /// a sink that already persisted part of it must throw <see cref="AuditSinkPartialFailureException"/> naming the
    /// envelopes that were NOT written, so only those are retried and nothing is inserted twice.
    /// </summary>
    Task WriteAsync(IReadOnlyList<AuditEnvelope> batch, CancellationToken ct = default);
}
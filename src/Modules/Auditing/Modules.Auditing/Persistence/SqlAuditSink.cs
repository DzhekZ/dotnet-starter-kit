using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Auditing.Persistence;

/// <summary>
/// Persists audit envelopes into SQL using EF Core.
/// </summary>
/// <remarks>
/// Every tenant's events are written in their own scope, database and transaction, so one tenant's
/// failure (a dedicated database that is unreachable or not migrated yet) can neither roll back nor
/// block the others. The sink always attempts every tenant; if any failed it then throws an
/// <see cref="AuditSinkPartialFailureException"/> naming exactly the envelopes that were not
/// persisted, so the caller retries those alone instead of re-inserting the ones already written.
/// </remarks>
public sealed class SqlAuditSink : IAuditSink
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAuditSerializer _serializer;
    private readonly ILogger<SqlAuditSink> _log;

    public SqlAuditSink(IServiceScopeFactory scopeFactory, IAuditSerializer serializer, ILogger<SqlAuditSink> log)
        => (_scopeFactory, _serializer, _log) = (scopeFactory, serializer, log);

    public async Task WriteAsync(IReadOnlyList<AuditEnvelope> batch, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (batch.Count == 0) return;

        List<AuditEnvelope>? failedEnvelopes = null;
        List<Exception>? failures = null;

        // Process per-tenant so MultiTenantDbContext has an ambient tenant context.
        foreach (var group in batch.GroupBy(e => e.TenantId))
        {
            var events = group.ToList();
            try
            {
                await WriteTenantAsync(group.Key, events, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Audit write failed for tenant {TenantId}; {Count} events were not persisted.",
                    group.Key ?? "<null>", events.Count);
                (failedEnvelopes ??= []).AddRange(events);
                (failures ??= []).Add(ex);
            }
        }

        if (failedEnvelopes is not null && failures is not null)
        {
            throw new AuditSinkPartialFailureException(
                $"Audit write failed for {failures.Count} tenant group(s); {failedEnvelopes.Count} of {batch.Count} events were not persisted.",
                failedEnvelopes,
                failures.Count == 1 ? failures[0] : new AggregateException(failures));
        }
    }

    private async Task WriteTenantAsync(string? tenantId, List<AuditEnvelope> events, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();

        var tenantInfo = tenantId is null
            ? await store.GetAsync(MultitenancyConstants.Root.Id).ConfigureAwait(false)
            : await store.GetAsync(tenantId).ConfigureAwait(false);

        if (tenantInfo is null)
        {
            _log.LogWarning("Skipping audit write for tenant {TenantId} because tenant was not found.", tenantId ?? "<null>");
            return;
        }

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenantInfo);

        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        var records = events.Select(e => new AuditRecord
        {
            Id = e.Id,
            OccurredAtUtc = e.OccurredAtUtc,
            ReceivedAtUtc = e.ReceivedAtUtc,
            EventType = (int)e.EventType,
            Severity = (byte)e.Severity,
            TenantId = e.TenantId,
            UserId = e.UserId,
            UserName = e.UserName,
            TraceId = e.TraceId,
            SpanId = e.SpanId,
            CorrelationId = e.CorrelationId,
            RequestId = e.RequestId,
            Source = e.Source,
            Tags = (long)e.Tags,
            PayloadJson = _serializer.SerializePayload(e.Payload)
        }).ToList();

        db.AuditRecords.AddRange(records);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        if (_log.IsEnabled(LogLevel.Information))
        {
            _log.LogInformation("Wrote {Count} audit records for tenant {TenantId}.", records.Count, tenantInfo.Id);
        }
    }
}
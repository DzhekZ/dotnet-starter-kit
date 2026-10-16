using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Persistence;
using Integration.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Integration.Tests.Tests.Auditing;

/// <summary>
/// One flush batch carries events of many tenants, and each tenant is written to its own database. A tenant
/// whose dedicated database has no <c>audit</c> schema (still being provisioned, or restored without it) makes
/// that tenant's insert fail with <c>42P01</c>. The other tenants' events in the same batch must still be
/// persisted exactly once, and the sink must report only the broken tenant's envelopes as unwritten — before,
/// the exception aborted the loop, the worker re-sent the whole batch, and the groups that had already been
/// committed collided on their primary keys (<c>23505</c>) and were dead-lettered together with the broken one.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class SqlAuditSinkFailureIsolationTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public SqlAuditSinkFailureIsolationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task WriteAsync_Should_PersistHealthyTenants_And_ReportOnlyTheBrokenOne_When_DedicatedDatabaseHasNoAuditSchema()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var rootClient = await _auth.CreateRootAdminClientAsync(ct);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var healthyTenantId = $"audit-ok-{uniqueId}";
        var brokenTenantId = $"audit-bad-{uniqueId}";

        var defaultConnection = _factory.Services.GetRequiredService<IConfiguration>()["DatabaseOptions:ConnectionString"];
        defaultConnection.ShouldNotBeNullOrWhiteSpace();
        var brokenConnection = new NpgsqlConnectionStringBuilder(defaultConnection)
        {
            Database = $"fsh_audit_bad_{uniqueId}",
        }.ConnectionString;

        await CreateTenantAsync(rootClient, healthyTenantId, connectionString: null, ct);
        await CreateTenantAsync(rootClient, brokenTenantId, brokenConnection, ct);
        await TenantProvisioningWait.WaitForProvisioningAsync(rootClient, healthyTenantId);
        await TenantProvisioningWait.WaitForProvisioningAsync(rootClient, brokenTenantId);

        await DropAuditSchemaAsync(brokenConnection, ct);

        AuditEnvelope[] brokenEvents = [NewEnvelope(brokenTenantId), NewEnvelope(brokenTenantId)];
        AuditEnvelope[] healthyEvents = [NewEnvelope(healthyTenantId), NewEnvelope(healthyTenantId)];
        AuditEnvelope[] rootEvents = [NewEnvelope(TestConstants.RootTenantId)];

        // The broken tenant goes first and its events are interleaved with the others': the old loop gave up
        // at the first failing group, so everything after it was never attempted.
        AuditEnvelope[] batch =
        [
            brokenEvents[0], healthyEvents[0], rootEvents[0], brokenEvents[1], healthyEvents[1],
        ];
        var sink = ActivatorUtilities.CreateInstance<SqlAuditSink>(_factory.Services);

        // Act
        var failure = await Should.ThrowAsync<AuditSinkPartialFailureException>(() => sink.WriteAsync(batch, ct));

        // Assert — only the broken tenant's envelopes are reported as unwritten...
        failure.FailedEnvelopes.Select(e => e.Id).ShouldBe(brokenEvents.Select(e => e.Id), ignoreOrder: true);

        // ...and every other tenant's events were committed.
        (await PersistedIdsAsync(healthyTenantId, healthyEvents, ct))
            .ShouldBe(healthyEvents.Select(e => e.Id), ignoreOrder: true);
        (await PersistedIdsAsync(TestConstants.RootTenantId, rootEvents, ct))
            .ShouldBe(rootEvents.Select(e => e.Id), ignoreOrder: true);

        // Retrying just the reported remainder (what the worker does) touches nothing that is already stored:
        // the broken tenant fails the same way, the healthy tenants are not written a second time.
        var retry = await Should.ThrowAsync<AuditSinkPartialFailureException>(
            () => sink.WriteAsync(failure.FailedEnvelopes, ct));
        retry.FailedEnvelopes.Select(e => e.Id).ShouldBe(brokenEvents.Select(e => e.Id), ignoreOrder: true);
        (await PersistedIdsAsync(healthyTenantId, healthyEvents, ct)).Count.ShouldBe(healthyEvents.Length);
    }

    private static AuditEnvelope NewEnvelope(string tenantId) => new(
        id: Guid.CreateVersion7(),
        occurredAtUtc: DateTime.UtcNow,
        receivedAtUtc: DateTime.UtcNow,
        eventType: AuditEventType.Activity,
        severity: AuditSeverity.Information,
        tenantId: tenantId,
        userId: null,
        userName: null,
        traceId: null,
        spanId: null,
        correlationId: null,
        requestId: null,
        source: "sql-audit-sink-isolation",
        tags: AuditTag.None,
        payload: new { name = "sql-audit-sink-isolation" });

    private static string AdminEmail(string tenantId) => $"admin-{tenantId}@tenant.com";

    private static async Task CreateTenantAsync(
        HttpClient rootClient, string tenantId, string? connectionString, CancellationToken ct)
    {
        using var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString,
            adminEmail = AdminEmail(tenantId),
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        }, cancellationToken: ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    /// <summary>Leaves the tenant's database reachable and migrated for everything but auditing.</summary>
    private static async Task DropAuditSchemaAsync(string connectionString, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("DROP SCHEMA IF EXISTS audit CASCADE", connection);
        await command.ExecuteNonQueryAsync(ct);
    }

    // Tenant context is an AsyncLocal, so it is set in the same method as the DbContext call.
    private async Task<List<Guid>> PersistedIdsAsync(string tenantId, AuditEnvelope[] events, CancellationToken ct)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().GetAsync(tenantId);
        tenant.ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var wanted = events.Select(e => e.Id).ToList();
        return await db.AuditRecords
            .Where(r => wanted.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(ct);
    }
}

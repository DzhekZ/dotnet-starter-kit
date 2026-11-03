using System.Diagnostics;
using System.Net;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Events;
using ActivityKind = FSH.Modules.Auditing.Contracts.ActivityKind;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Roles.CopyRoleToTenants;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Identity.Features.v1.Roles.CopyRoleToTenants;

/// <summary>
/// Copies a custom role of the root tenant into the other tenants. Every step reuses the role service
/// as it is: creating the role, listing roles for the existence check and granting the permissions
/// (which strips root-only ones outside the root tenant and drops the permission caches). The per-tenant
/// scope is built the way <c>RolePermissionSyncHostedService</c> builds it.
/// <para>
/// Audit: the request itself is recorded by the HTTP audit, the role and claims written in each tenant by the EF audit interceptor, and this handler
/// adds one event per tenant (what came of it) and one summary event, all naming the operator and the tenant they concern, on the lane that never
/// drops. The operator is told in the bell through <see cref="RoleCopiedToTenantsIntegrationEvent"/>. Neither the audit nor the bell can fail the copy
/// that has already been done: they are logged when they fail.
/// </para>
/// </summary>
public sealed class CopyRoleToTenantsCommandHandler : ICommandHandler<CopyRoleToTenantsCommand, CopyRoleToTenantsResponse>
{
    // GetRolesAsync clamps the page size to this.
    private const int MaxRolesPageSize = 200;

    private const string EventSource = "Identity";
    private const string TenantAuditName = "role.copy-to-tenant";
    private const string SummaryAuditName = "role.copy-to-tenants";

    private readonly IRoleService _roleService;
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    private readonly ICurrentUser _currentUser;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOutboxWriter _outbox;
    private readonly IAuditPublisher _audit;
    private readonly TimeProvider _time;
    private readonly ILogger<CopyRoleToTenantsCommandHandler> _logger;

    public CopyRoleToTenantsCommandHandler(
        IRoleService roleService,
        IMultiTenantStore<AppTenantInfo> tenantStore,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        ICurrentUser currentUser,
        IServiceScopeFactory scopeFactory,
        IOutboxWriter outbox,
        IAuditPublisher audit,
        TimeProvider time,
        ILogger<CopyRoleToTenantsCommandHandler> logger)
    {
        _roleService = roleService;
        _tenantStore = tenantStore;
        _tenantAccessor = tenantAccessor;
        _currentUser = currentUser;
        _scopeFactory = scopeFactory;
        _outbox = outbox;
        _audit = audit;
        _time = time;
        _logger = logger;
    }

    public async ValueTask<CopyRoleToTenantsResponse> Handle(CopyRoleToTenantsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // A root operator's request can be scoped to another tenant through the tenant header, so the role
        // looked up below would belong to that tenant. Only a role of the root tenant is copied.
        if (_tenantAccessor.MultiTenantContext?.TenantInfo?.Id != MultitenancyConstants.Root.Id)
        {
            throw new ForbiddenException("Roles can be copied to tenants from the root tenant only.");
        }

        var source = await _roleService.GetWithPermissionsAsync(command.RoleId, cancellationToken).ConfigureAwait(false);

        // Built-in roles are seeded into every tenant already, and RoleService refuses to create them.
        if (RoleConstants.IsDefault(source.Name))
        {
            throw new CustomException(
                $"'{source.Name}' is a built-in role that every tenant already has.",
                Array.Empty<string>(),
                HttpStatusCode.BadRequest);
        }

        var tenants = await _tenantStore.GetAllAsync().ConfigureAwait(false);
        var results = new List<CopyRoleToTenantResult>();

        foreach (var tenant in tenants.Where(t => t.Id != MultitenancyConstants.Root.Id).OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await CopyToTenantAsync(tenant, source, cancellationToken).ConfigureAwait(false);
            results.Add(result);
            await AuditAsync(
                tenant.Id,
                TenantAuditName,
                new { roleId = source.Id, roleName = source.Name, tenantId = tenant.Id },
                new { outcome = result.Status, error = result.Error },
                result.Status == CopyRoleStatuses.Failed ? AuditSeverity.Warning : AuditSeverity.Information,
                cancellationToken).ConfigureAwait(false);
        }

        int created = results.Count(r => r.Status == CopyRoleStatuses.Created);
        int skipped = results.Count(r => r.Status == CopyRoleStatuses.Skipped);
        int failed = results.Count(r => r.Status == CopyRoleStatuses.Failed);

        await AuditAsync(
            MultitenancyConstants.Root.Id,
            SummaryAuditName,
            new { roleId = source.Id, roleName = source.Name, tenants = results.Count },
            new { created, skipped, failed },
            failed > 0 ? AuditSeverity.Warning : AuditSeverity.Information,
            cancellationToken).ConfigureAwait(false);
        await NotifyAsync(source, created, skipped, failed, cancellationToken).ConfigureAwait(false);

        return new CopyRoleToTenantsResponse(results);
    }

    /// <summary>
    /// Writes an event of the audit trail for the tenant it concerns, naming the operator. The tenant is named on the envelope, not left to the
    /// scope the publisher reads: the copy runs in the request of the root tenant while the events belong to the other tenants. The event rides
    /// the lane that waits, as an action somebody must be able to look up later.
    /// </summary>
    private async Task AuditAsync(
        string tenantId,
        string name,
        object request,
        object response,
        AuditSeverity severity,
        CancellationToken cancellationToken)
    {
        try
        {
            DateTime now = _time.GetUtcNow().UtcDateTime;
            Guid userId = _currentUser.GetUserId();

            var payload = new ActivityEventPayload(
                ActivityKind.Command,
                name,
                StatusCode: null,
                DurationMs: 0,
                Captured: BodyCapture.None,
                RequestSize: 0,
                ResponseSize: 0,
                RequestPreview: request,
                ResponsePreview: response);

            await _audit.PublishAsync(
                new AuditEnvelope(
                    id: Guid.CreateVersion7(),
                    occurredAtUtc: now,
                    receivedAtUtc: now,
                    eventType: AuditEventType.Activity,
                    severity: severity,
                    tenantId: tenantId,
                    userId: userId == Guid.Empty ? null : userId.ToString(),
                    userName: _currentUser.Name,
                    traceId: null,
                    spanId: null,
                    correlationId: null,
                    requestId: null,
                    source: EventSource,
                    tags: AuditTag.Guaranteed,
                    payload: payload),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "The audit event '{Event}' for tenant '{Tenant}' could not be published", name, tenantId);
        }
    }

    /// <summary>Tells the operator in the bell that the copy is done (the handler of Notifications writes the line).</summary>
    private async Task NotifyAsync(RoleDto source, int created, int skipped, int failed, CancellationToken cancellationToken)
    {
        try
        {
            Guid userId = _currentUser.GetUserId();
            await _outbox.AddAsync(
                new RoleCopiedToTenantsIntegrationEvent(
                    Id: Guid.NewGuid(),
                    OccurredOnUtc: _time.GetUtcNow().UtcDateTime,
                    TenantId: _tenantAccessor.MultiTenantContext?.TenantInfo?.Id,
                    CorrelationId: Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                    Source: EventSource,
                    RoleId: source.Id,
                    RoleName: source.Name,
                    Created: created,
                    Skipped: skipped,
                    Failed: failed,
                    RequestedBy: userId == Guid.Empty ? null : userId),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "The notification about the copied role '{Role}' could not be queued", source.Name);
        }
    }

    private async Task<CopyRoleToTenantResult> CopyToTenantAsync(AppTenantInfo tenant, RoleDto source, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
            // Keeps the operator as the author of the new role claims instead of an empty user.
            scope.ServiceProvider.GetRequiredService<ICurrentUserInitializer>()
                .SetCurrentUserId(_currentUser.GetUserId().ToString());

            var tenantRoles = scope.ServiceProvider.GetRequiredService<IRoleService>();

            var sameName = await tenantRoles.GetRolesAsync(1, MaxRolesPageSize, source.Name, cancellationToken).ConfigureAwait(false);
            if (sameName.Items.Any(r => string.Equals(r.Name, source.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return new CopyRoleToTenantResult(tenant.Id, CopyRoleStatuses.Skipped);
            }

            var created = await tenantRoles
                .CreateOrUpdateRoleAsync(string.Empty, source.Name, source.Description ?? string.Empty, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                if (source.Permissions is { Count: > 0 })
                {
                    // The tenant's role service filters root-only permissions out for a non-root tenant.
                    await tenantRoles.UpdatePermissionsAsync(created.Id, [.. source.Permissions], cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Do not leave a role without permissions behind: the next run would see it and skip the tenant.
                await tenantRoles.DeleteRoleAsync(created.Id, CancellationToken.None).ConfigureAwait(false);
                throw;
            }

            return new CopyRoleToTenantResult(tenant.Id, CopyRoleStatuses.Created);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One tenant failing must not stop the rest of the loop.
            _logger.LogError(ex, "Copying role '{Role}' to tenant '{Tenant}' failed", source.Name, tenant.Id);
            return new CopyRoleToTenantResult(
                tenant.Id,
                CopyRoleStatuses.Failed,
                ex is CustomException ? ex.Message : "Unexpected error, see the server log.");
        }
    }
}

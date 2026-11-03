using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web.Realtime;
using FSH.Modules.Identity.Contracts.Events;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>
/// Subscribes to <see cref="RoleCopiedToTenantsIntegrationEvent"/>: the operator of the root tenant copied a custom role into the other tenants.
/// They get a row in the inbox («Копирование роли завершено») and a <c>NotificationCreated</c> push to their SignalR group, the way the user who
/// deleted a file is told (<see cref="DocumentDeletedIntegrationEventHandler"/>). An event without <c>RequestedBy</c> tells nobody. The tenant of
/// the event is the ambient tenant here, as the event bus sets it before handlers are resolved.
/// <para>
/// The row of the inbox is what counts: the push is best effort. A failing push must not fail the handler, or the dispatcher would retry it and the
/// user would get the row again.
/// </para>
/// </summary>
public sealed partial class RoleCopiedToTenantsIntegrationEventHandler(
    NotificationsDbContext db,
    IHubContext<AppHub> hub,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    ILogger<RoleCopiedToTenantsIntegrationEventHandler> logger)
    : IIntegrationEventHandler<RoleCopiedToTenantsIntegrationEvent>
{
    /// <summary>The <c>Type</c> of the row for a role that was copied into the tenants.</summary>
    public const string CopiedType = "role.copied";

    public async Task HandleAsync(RoleCopiedToTenantsIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (@event.RequestedBy is null)
        {
            return;
        }

        // Fail loud on a tenant mismatch rather than write to the wrong tenant: the DbContext captures its tenant at construction.
        var ambientTenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (!string.Equals(ambientTenantId, @event.TenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Tenant context mismatch handling {nameof(RoleCopiedToTenantsIntegrationEvent)}: ambient " +
                $"'{ambientTenantId ?? "(none)"}' != event '{@event.TenantId ?? "(none)"}'. The publisher must " +
                "establish the tenant's Finbuckle context before publishing (see eventing rules).");
        }

        Notification notification = Create(@event);
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        try
        {
            await hub.Clients.Group($"user:{notification.UserId}")
                .SendAsync("NotificationCreated", new
                {
                    id = notification.Id,
                    type = notification.Type,
                    title = notification.Title,
                    body = notification.Body,
                    link = notification.Link,
                    source = notification.Source,
                    createdAtUtc = notification.CreatedAtUtc,
                }, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogPushFailed(ex, notification.UserId, @event.RoleId);
        }
    }

    /// <summary>The row of the inbox for the operator who copied the role; it leads to the role.</summary>
    internal static Notification Create(RoleCopiedToTenantsIntegrationEvent copied)
    {
        ArgumentNullException.ThrowIfNull(copied);
        if (copied.RequestedBy is not { } userId)
        {
            throw new ArgumentException("The event names no user to tell.", nameof(copied));
        }

        return Notification.Create(
            userId: userId.ToString(),
            type: CopiedType,
            title: "Копирование роли завершено",
            body: Body(copied),
            link: $"/roles/{copied.RoleId}",
            source: copied.Source,
            metadata: new
            {
                roleId = copied.RoleId,
                roleName = copied.RoleName,
                created = copied.Created,
                skipped = copied.Skipped,
                failed = copied.Failed,
            });
    }

    internal static string Body(RoleCopiedToTenantsIntegrationEvent copied)
    {
        ArgumentNullException.ThrowIfNull(copied);

        string name = string.IsNullOrWhiteSpace(copied.RoleName) ? "Роль" : $"Роль «{copied.RoleName.Trim()}»";
        string counts = $"создана в тенантах: {copied.Created}, уже была: {copied.Skipped}";
        return copied.Failed > 0
            ? $"{name}: {counts}, не удалось: {copied.Failed}. Подробности — в журнале аудита."
            : $"{name}: {counts}.";
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The push of the notification about the copied role {RoleId} to user {UserId} failed; the row of the inbox is stored")]
    private partial void LogPushFailed(Exception exception, string userId, string roleId);
}

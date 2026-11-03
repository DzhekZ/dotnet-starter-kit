using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Identity.Contracts.Events;

/// <summary>
/// Raised through the outbox when a custom role of the root tenant has been copied into the other tenants. Notifications tells
/// <paramref name="RequestedBy"/> in the bell («Копирование роли завершено»). <paramref name="TenantId"/> is the root tenant, the one the
/// operator works in. The counts are those of the tenants visited: created, left alone because they had a role of that name, failed.
/// </summary>
public sealed record RoleCopiedToTenantsIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    string RoleId,
    string RoleName,
    int Created,
    int Skipped,
    int Failed,
    Guid? RequestedBy = null) : IIntegrationEvent;

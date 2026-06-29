using FSH.Framework.Core.Domain;

namespace FSH.Modules.Profile.Domain.Events;

public sealed record ProfileCreatedDomainEvent(
    Guid ProfileId,
    string Sku,
    string Name,
    Guid EventId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);

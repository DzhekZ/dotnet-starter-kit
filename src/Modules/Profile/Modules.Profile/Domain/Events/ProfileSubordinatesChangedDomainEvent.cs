using FSH.Framework.Core.Domain;

namespace FSH.Modules.Profile.Domain.Events;

public sealed record ProfileSubordinatesChangedDomainEvent(
    Guid ProfileId,
    int OldAmount,
    int NewAmount,
    Guid EventId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);

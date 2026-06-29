using FSH.Framework.Core.Domain;

namespace FSH.Modules.Profile.Domain.Events;

public sealed record ProfilePriceChangedDomainEvent(
    Guid ProfileId,
    decimal OldAmount,
    decimal NewAmount,
    string Currency,
    Guid EventId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);

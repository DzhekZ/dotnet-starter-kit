using FSH.Framework.Core.Domain;

namespace FSH.Modules.Profile.Domain.Events;

public sealed record ProductPriceChangedDomainEvent(
    Guid ProductId,
    decimal OldAmount,
    decimal NewAmount,
    string Currency,
    Guid EventId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);

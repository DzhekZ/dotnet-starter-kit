using FSH.Framework.Core.Domain;

namespace FSH.Modules.Profile.Domain.Events;

public sealed record ProfileStockAdjustedDomainEvent(
    Guid ProfileId,
    int OldStock,
    int NewStock,
    int Delta,
    Guid EventId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);

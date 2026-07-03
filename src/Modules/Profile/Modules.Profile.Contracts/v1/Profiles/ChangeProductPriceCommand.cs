using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles;

public sealed record ChangeProductPriceCommand(
    Guid ProductId,
    decimal Amount,
    string Currency) : ICommand<Guid>;

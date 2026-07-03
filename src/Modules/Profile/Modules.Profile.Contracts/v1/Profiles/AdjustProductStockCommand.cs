using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles;

public sealed record AdjustProductStockCommand(
    Guid ProductId,
    int Delta) : ICommand<int>;

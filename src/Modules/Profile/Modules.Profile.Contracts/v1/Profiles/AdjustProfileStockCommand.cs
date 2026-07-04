using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles;

public sealed record AdjustProfileStockCommand(
    Guid ProductId,
    int Delta) : ICommand<int>;

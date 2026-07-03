using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles;

public sealed record RestoreProductCommand(Guid ProductId) : ICommand<Guid>;

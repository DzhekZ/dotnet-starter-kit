using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles;

public sealed record RestoreProfileCommand(Guid ProfileId) : ICommand<Guid>;

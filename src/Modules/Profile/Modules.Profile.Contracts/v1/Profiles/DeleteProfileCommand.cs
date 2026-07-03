using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles;

public sealed record DeleteProfileCommand(Guid ProfileId) : ICommand<Unit>;

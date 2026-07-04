using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles;

public sealed record ChangeProfileSubordinatesCommand(
    Guid ProfileId,
    int Amount) : ICommand<Guid>;

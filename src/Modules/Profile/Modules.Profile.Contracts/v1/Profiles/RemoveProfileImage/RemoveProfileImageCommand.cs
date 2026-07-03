using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles.RemoveProfileImage;

public sealed record RemoveProfileImageCommand(Guid ProfileId, Guid ImageId) : ICommand<Unit>;

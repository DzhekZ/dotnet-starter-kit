using FSH.Modules.Profile.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles;

public sealed record GetProfileByIdQuery(Guid ProfileId) : IQuery<ProfileDto>;

using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.Dtos;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Profiles.GetProfileById;

public sealed class GetProfileByIdQueryHandler(ProfileDbContext dbContext)
    : IQueryHandler<GetProfileByIdQuery, ProfileDto>
{
    public async ValueTask<ProfileDto> Handle(GetProfileByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var profile = await dbContext.ProfileItems
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.ProfileId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Profile {query.ProfileId} not found.");

        return profile.ToDto();
    }
}

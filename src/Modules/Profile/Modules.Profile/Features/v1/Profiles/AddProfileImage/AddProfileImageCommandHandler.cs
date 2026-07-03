using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.Dtos;
using FSH.Modules.Profile.Contracts.v1.Profiles.AddProfileImage;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Profiles.AddProfileImage;

public sealed class AddProfileImageCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<AddProfileImageCommand, ProfileImageDto>
{
    public async ValueTask<ProfileImageDto> Handle(AddProfileImageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var profile = await dbContext.ProfileItems
            .FirstOrDefaultAsync(p => p.Id == command.ProfileId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Profile {command.ProfileId} not found.");

        var image = profile.AddImage(command.FileAssetId, command.Url);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ProfileImageDto(image.Id, image.FileAssetId, image.Url, image.IsThumbnail, image.SortOrder, image.CreatedAtUtc);
    }
}

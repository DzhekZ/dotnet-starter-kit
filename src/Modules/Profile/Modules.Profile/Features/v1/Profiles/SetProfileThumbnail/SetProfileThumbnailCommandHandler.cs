using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Profiles.SetProfileThumbnail;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Profiles.SetProfileThumbnail;

public sealed class SetProfileThumbnailCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<SetProfileThumbnailCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetProfileThumbnailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var profile = await dbContext.ProfileItems
            .FirstOrDefaultAsync(p => p.Id == command.ProfileId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Profile {command.ProfileId} not found.");

        // Domain throws InvalidOperationException for unknown imageId; translate to a
        // framework-aware 404 so the API surfaces NotFound rather than a 500.
        if (!profile.Images.Any(i => i.Id == command.ImageId))
        {
            throw new NotFoundException($"Image {command.ImageId} not found on profile {command.ProfileId}.");
        }

        profile.SetThumbnail(command.ImageId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

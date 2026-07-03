using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Profiles.RemoveProfileImage;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Profiles.RemoveProfileImage;

public sealed class RemoveProfileImageCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<RemoveProfileImageCommand, Unit>
{
    public async ValueTask<Unit> Handle(RemoveProfileImageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var profile = await dbContext.ProfileItems
            .FirstOrDefaultAsync(p => p.Id == command.ProfileId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProfileId} not found.");

        // Domain throws InvalidOperationException for unknown imageId; translate to 404.
        if (!profile.Images.Any(i => i.Id == command.ImageId))
        {
            throw new NotFoundException($"Image {command.ImageId} not found on profile {command.ProfileId}.");
        }

        profile.RemoveImage(command.ImageId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

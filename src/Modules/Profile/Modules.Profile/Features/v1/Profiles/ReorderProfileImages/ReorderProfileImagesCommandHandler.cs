using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Profiles.ReorderProfileImages;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Profiles.ReorderProfileImages;

public sealed class ReorderProfileImagesCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<ReorderProfileImagesCommand, Unit>
{
    public async ValueTask<Unit> Handle(ReorderProfileImagesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var profile = await dbContext.ProfileItems
            .FirstOrDefaultAsync(p => p.Id == command.Profiled, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Profile {command.Profiled} not found.");

        profile.ReorderImages(command.OrderedImageIds);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

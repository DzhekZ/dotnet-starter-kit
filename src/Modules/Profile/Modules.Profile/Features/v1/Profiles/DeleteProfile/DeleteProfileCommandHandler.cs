using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Profiles.DeleteProfile;

public sealed class DeleteProfileCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<DeleteProfileCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteProfileCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // IgnoreAutoIncludes is load-bearing: if Product.Images (AutoInclude'd) load here, Remove() cascades Deleted onto them
        // and the soft-delete interceptor (rescues only owned refs) HARD-deletes them. Untracked keeps the delete a pure UPDATE so rows survive restore.
        var product = await dbContext.ProfileItems
            .IgnoreAutoIncludes()
            .FirstOrDefaultAsync(p => p.Id == command.ProfileId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProfileId} not found.");

        dbContext.ProfileItems.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

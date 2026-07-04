using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Profiles.ChangeProfileSubordinates;

public sealed class ChangeProfileSubordinatesCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<ChangeProfileSubordinatesCommand, Guid>
{
    public async ValueTask<Guid> Handle(ChangeProfileSubordinatesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await dbContext.ProfileItems
            .FirstOrDefaultAsync(p => p.Id == command.ProfileId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Profile {command.ProfileId} not found.");

        product.ChangeSubordinates(command.Amount);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return product.Id;
    }
}

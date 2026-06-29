using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Modules.Profile.Contracts.v1.Positions;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Positions.RestorePosition;

public sealed class RestorePositionCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<RestorePositionCommand, Guid>
{
    public async ValueTask<Guid> Handle(RestorePositionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Disable only the SoftDelete filter so we can load a deleted row;
        // tenant scoping stays in force, so cross-tenant restores cannot leak.
        var position = await dbContext.Positions
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .FirstOrDefaultAsync(b => b.Id == command.PositionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Position {command.PositionId} not found.");

        position.Restore();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return position.Id;
    }
}

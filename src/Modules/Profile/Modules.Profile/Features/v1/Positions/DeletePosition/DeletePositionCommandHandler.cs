using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Positions;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Positions.DeletePosition;

public sealed class DeletePositionCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<DeletePositionCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeletePositionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var position = await dbContext.Positions
            .FirstOrDefaultAsync(b => b.Id == command.PositionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Profile {command.PositionId} not found.");

        dbContext.Positions.Remove(position);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

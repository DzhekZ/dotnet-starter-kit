using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Positions;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Positions.UpdatePosition;

public sealed class UpdatePositionCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<UpdatePositionCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdatePositionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var position = await dbContext.Positions
            .FirstOrDefaultAsync(b => b.Id == command.PositionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Brand {command.PositionId} not found.");

        position.Update(command.Name, command.Description);

        bool slugTaken = await dbContext.Positions
            .AnyAsync(b => b.Slug == position.Slug && b.Id != position.Id, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"Another position with name '{command.Name}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return position.Id;
    }
}

using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Positions;
using FSH.Modules.Profile.Data;
using FSH.Modules.Profile.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Positions.CreatePosition;

public sealed class CreatePositionCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<CreatePositionCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePositionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var position = Position.Create(command.Name, command.Description);

        bool slugTaken = await dbContext.Positions
            .AnyAsync(b => b.Slug == position.Slug, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"A position with name '{command.Name}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        dbContext.Positions.Add(position);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return position.Id;
    }
}

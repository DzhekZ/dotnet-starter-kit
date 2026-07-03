using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using FSH.Modules.Profile.Data;
using FSH.Modules.Profile.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.CreateSubdivision;

public sealed class CreateSubdivisionCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<CreateSubdivisionCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateSubdivisionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ParentSubdivisionId is { } parentId)
        {
            bool parentExists = await dbContext.Subdivisions
                .AnyAsync(c => c.Id == parentId, cancellationToken)
                .ConfigureAwait(false);
            if (!parentExists)
            {
                throw new NotFoundException($"Parent subdivision {parentId} not found.");
            }
        }

        var subdivision = Subdivision.Create(command.Name, command.Description, command.TypeSubdivision, command.ParentSubdivisionId);

        bool slugTaken = await dbContext.Subdivisions
            .AnyAsync(c => c.Slug == subdivision.Slug 
                            && c.ParentSubdivisionId == subdivision.ParentSubdivisionId
                            && c.TypeSubdivision == subdivision.TypeSubdivision, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"A subdivision with name '{command.Name}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        dbContext.Subdivisions.Add(subdivision);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return subdivision.Id;
    }
}

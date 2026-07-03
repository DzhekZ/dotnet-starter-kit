using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.UpdateSubdivision;

public sealed class UpdateSubdivisionCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<UpdateSubdivisionCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateSubdivisionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subdivision = await dbContext.Subdivisions
            .FirstOrDefaultAsync(c => c.Id == command.SubdivisionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Subdivision {command.SubdivisionId} not found.");

        if (command.ParentSubdivisionId is { } parentId)
        {
            if (parentId == subdivision.Id)
            {
                throw new CustomException(
                    "A subdivision cannot be its own parent.",
                    (IEnumerable<string>?)null,
                    HttpStatusCode.BadRequest);
            }

            // Walk parent chain to detect cycles (parent → ancestor of self)
            var visited = new HashSet<Guid> { subdivision.Id };
            Guid? cursor = parentId;
            while (cursor is { } cur)
            {
                if (!visited.Add(cur))
                {
                    throw new CustomException(
                        "Setting this parent would create a cycle.",
                        (IEnumerable<string>?)null,
                        HttpStatusCode.BadRequest);
                }
                cursor = await dbContext.Subdivisions
                    .Where(c => c.Id == cur)
                    .Select(c => c.ParentSubdivisionId)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        subdivision.Update(command.Name, command.Description, command.TypeSubdivision, command.ParentSubdivisionId);

        bool slugTaken = await dbContext.Subdivisions
            .AnyAsync(c => c.Slug == subdivision.Slug
                            && c.TypeSubdivision == subdivision.TypeSubdivision
                            && c.ParentSubdivisionId == subdivision.ParentSubdivisionId
                            && c.Id != subdivision.Id, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"Another subdivision with name '{command.Name}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return subdivision.Id;
    }
}

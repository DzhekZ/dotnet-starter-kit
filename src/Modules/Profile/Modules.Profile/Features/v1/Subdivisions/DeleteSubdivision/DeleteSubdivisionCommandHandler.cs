using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.DeleteSubdivision;

public sealed class DeleteSubdivisionCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<DeleteSubdivisionCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteSubdivisionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subdivision = await dbContext.Subdivisions
            .FirstOrDefaultAsync(c => c.Id == command.SubdivisionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Subdivision {command.SubdivisionId} not found.");

        bool hasChildren = await dbContext.Subdivisions
            .AnyAsync(c => c.ParentSubdivisionId == subdivision.Id, cancellationToken)
            .ConfigureAwait(false);
        if (hasChildren)
        {
            throw new CustomException(
                "Cannot delete a subdivision that has child Subdivisions. Move or remove the children first.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        dbContext.Subdivisions.Remove(subdivision);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

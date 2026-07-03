using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.RestoreSubdivision;

public sealed class RestoreSubdivisionCommandHandler(ProfileDbContext dbContext)
    : ICommandHandler<RestoreSubdivisionCommand, Guid>
{
    public async ValueTask<Guid> Handle(RestoreSubdivisionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subdivision = await dbContext.Subdivisions
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .FirstOrDefaultAsync(c => c.Id == command.SubdivisionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Subdivision {command.SubdivisionId} not found.");

        subdivision.Restore();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return subdivision.Id;
    }
}

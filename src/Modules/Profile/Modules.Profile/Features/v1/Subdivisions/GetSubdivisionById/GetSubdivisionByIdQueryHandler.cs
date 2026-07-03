using FSH.Framework.Core.Exceptions;
using FSH.Modules.Profile.Contracts.Dtos;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.GetSubdivisionById;

public sealed class GetSubdivisionByIdQueryHandler(ProfileDbContext dbContext)
    : IQueryHandler<GetSubdivisionByIdQuery, SubdivisionDto>
{
    public async ValueTask<SubdivisionDto> Handle(GetSubdivisionByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var c = await dbContext.Subdivisions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.SubdivisionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Subdivision {query.SubdivisionId} not found.");

        return new SubdivisionDto(c.Id, c.Name, c.Slug, c.Description, c.ParentSubdivisionId, c.CreatedAtUtc, c.UpdatedAtUtc, c.DeletedOnUtc, c.DeletedBy);
    }
}

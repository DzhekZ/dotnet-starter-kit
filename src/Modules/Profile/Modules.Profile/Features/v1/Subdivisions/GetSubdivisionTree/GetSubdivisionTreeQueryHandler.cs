using FSH.Modules.Profile.Contracts.Dtos;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.GetSubdivisionTree;

public sealed class GetSubdivisionTreeQueryHandler(ProfileDbContext dbContext)
    : IQueryHandler<GetSubdivisionTreeQuery, IReadOnlyList<SubdivisionTreeNodeDto>>
{
    public async ValueTask<IReadOnlyList<SubdivisionTreeNodeDto>> Handle(GetSubdivisionTreeQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var all = await dbContext.Subdivisions
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byParent = all.ToLookup(c => c.ParentSubdivisionId);

        IReadOnlyList<SubdivisionTreeNodeDto> Build(Guid? parentId)
        {
            return byParent[parentId]
                .Select(c => new SubdivisionTreeNodeDto(c.Id, c.Name, c.Slug, c.Description, Build(c.Id)))
                .ToList();
        }

        return Build(null);
    }
}

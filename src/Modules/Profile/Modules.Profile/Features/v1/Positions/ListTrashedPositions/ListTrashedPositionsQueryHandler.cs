using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Profile.Contracts.Dtos;
using FSH.Modules.Profile.Contracts.v1.Positions;
using FSH.Modules.Profile.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Positions.ListTrashedPositions;

public sealed class ListTrashedPositionsQueryHandler(ProfileDbContext dbContext)
    : IQueryHandler<ListTrashedPositionsQuery, PagedResponse<PositionDto>>
{
    public async ValueTask<PagedResponse<PositionDto>> Handle(
        ListTrashedPositionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        // Bypasses ONLY the soft-delete filter; Finbuckle tenant scoping stays in force, so a
        // tenant sees only its own trashed rows. Most-recently-deleted first.
        var q = dbContext.Positions
            .AsNoTracking()
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .Where(b => b.IsDeleted)
            .OrderByDescending(b => b.DeletedOnUtc);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await q
            .Skip((page - 1) * size)
            .Take(size)
            .Select(b => new PositionDto(
                b.Id, b.Name, b.Slug, b.Description,
                b.CreatedAtUtc, b.UpdatedAtUtc, b.DeletedOnUtc, b.DeletedBy))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PositionDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size),
        };
    }
}

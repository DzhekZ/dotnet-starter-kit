using FSH.Framework.Shared.Persistence;
using FSH.Modules.Profile.Contracts.Dtos;
using FSH.Modules.Profile.Contracts.v1.Positions;
using FSH.Modules.Profile.Data;
using FSH.Modules.Profile.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Positions.SearchPositions;

public sealed class SearchPositionsQueryHandler(ProfileDbContext dbContext)
    : IQueryHandler<SearchPositionsQuery, PagedResponse<PositionDto>>
{
    public async ValueTask<PagedResponse<PositionDto>> Handle(SearchPositionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var q = dbContext.Positions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(b =>
                EF.Functions.ILike(b.Name, $"%{term}%") ||
                EF.Functions.ILike(b.Slug, $"%{term}%"));
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var positions = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PositionDto>
        {
            Items = positions
                .Select(b => new PositionDto(b.Id, b.Name, b.Slug, b.Description, b.CreatedAtUtc, b.UpdatedAtUtc, b.DeletedOnUtc, b.DeletedBy))
                .ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }

    // Whitelist + safe default: unknown columns/directions fall back to (name asc) so callers
    // can't trigger a server error or probe the entity shape via reflection-style sort keys.
    private static IQueryable<Position> ApplySort(IQueryable<Position> q, string? sortBy, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "SLUG" => desc ? q.OrderByDescending(b => b.Slug) : q.OrderBy(b => b.Slug),
            "CREATEDATUTC" or "CREATED" => desc
                ? q.OrderByDescending(b => b.CreatedAtUtc)
                : q.OrderBy(b => b.CreatedAtUtc),
            _ => desc ? q.OrderByDescending(b => b.Name) : q.OrderBy(b => b.Name),
        };
    }
}

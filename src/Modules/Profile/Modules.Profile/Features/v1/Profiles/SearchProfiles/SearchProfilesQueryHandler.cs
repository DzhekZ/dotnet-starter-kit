using FSH.Framework.Shared.Persistence;
using FSH.Modules.Profile.Contracts.Dtos;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using FSH.Modules.Profile.Data;
using FSH.Modules.Profile.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Profile.Features.v1.Profiles.SearchProfiles;

public sealed class SearchProfilesQueryHandler(ProfileDbContext dbContext)
    : IQueryHandler<SearchProfilesQuery, PagedResponse<ProfileDto>>
{
    public async ValueTask<PagedResponse<ProfileDto>> Handle(SearchProfilesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var q = dbContext.ProfileItems.AsNoTracking().AsQueryable();

        if (query.PositionId is { } positionId)
        {
            q = q.Where(p => p.PositionId == positionId);
        }

        if (query.SubdivisionId is { } subdivisionId)
        {
            q = q.Where(p => p.SubdivisionId == subdivisionId);
        }

        if (query.IsActive is { } active)
        {
            q = q.Where(p => p.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(p =>
                EF.Functions.ILike(p.Name, $"%{term}%") ||
                EF.Functions.ILike(p.CodePerson??string.Empty, $"%{term}%") ||
                EF.Functions.ILike(p.Slug, $"%{term}%"));
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var products = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<ProfileDto>
        {
            Items = products.Select(p => p.ToDto()).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }

    private static IQueryable<ProfileItem> ApplySort(IQueryable<ProfileItem> q, string? sortBy, string? sortDir)
    {
        // Default to descending unless caller explicitly opts into ascending —
        // admins typically want newest-first when they don't pick a direction.
        bool desc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "NAME" => desc ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name),
            "SLUG" => desc ? q.OrderByDescending(p => p.Slug) : q.OrderBy(p => p.Slug),
            "TABNUM" => desc
                ? q.OrderByDescending(p => p.PersonnelNumber)
                : q.OrderBy(p => p.PersonnelNumber),
            _ => desc
                ? q.OrderByDescending(p => p.CreatedAtUtc)
                : q.OrderBy(p => p.CreatedAtUtc),
        };
    }
}

using FSH.Framework.Shared.Persistence;
using FSH.Modules.Profile.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Subdivisions;

/// <summary>
/// Search for categories with pagination and sorting.
/// </summary>
/// <param name="Search">Search term.</param>
/// <param name="ParentSubdivisionId">Optional parent subdivision ID filter.</param>
/// <param name="PageNumber">Page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="SortBy">Sort column. One of: name | slug | createdAtUtc.</param>
/// <param name="SortDir">Sort direction. One of: asc | desc.</param>
public sealed record SearchSubdivisionsQuery(
    string? Search = null,
    Guid? ParentSubdivisionId = null,
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<SubdivisionDto>>;

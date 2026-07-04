using FSH.Framework.Shared.Persistence;
using FSH.Modules.Profile.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles;

/// <summary>
/// Search for profiles with pagination and sorting.
/// </summary>
/// <param name="Search">Search term.</param>
/// <param name="PositionId">Optional position filter.</param>
/// <param name="SubdivisionId">Optional subdivision filter.</param>
/// <param name="IsActive">Optional active status filter.</param>
/// <param name="PageNumber">Page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="SortBy">Sort column. One of: name | slug | createdAtUtc | tabnum (personcode).</param>
/// <param name="SortDir">Sort direction. One of: asc | desc.</param>
public sealed record SearchProfilesQuery(
    string? Search = null,
    Guid? PositionId = null,
    Guid? SubdivisionId = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<ProfileDto>>;

using FSH.Framework.Shared.Persistence;
using FSH.Modules.Profile.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Positions;

/// <summary>
/// Lists soft-deleted brands. Bypasses the global IsDeleted query filter.
/// </summary>
public sealed record ListTrashedPositionsQuery(int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResponse<PositionDto>>;

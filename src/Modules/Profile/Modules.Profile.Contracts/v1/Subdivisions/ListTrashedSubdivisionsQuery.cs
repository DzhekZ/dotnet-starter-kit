using FSH.Framework.Shared.Persistence;
using FSH.Modules.Profile.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Subdivisions;

public sealed record ListTrashedSubdivisionsQuery(int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResponse<SubdivisionDto>>;

using FSH.Framework.Shared.Persistence;
using FSH.Modules.Profile.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles;

public sealed record ListTrashedProductsQuery(int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResponse<ProfileDto>>;

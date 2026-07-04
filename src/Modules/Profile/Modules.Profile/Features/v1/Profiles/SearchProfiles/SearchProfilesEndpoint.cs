using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.SearchProfiles;

public static class SearchProfilesEndpoint
{
    internal static RouteHandlerBuilder MapSearchProfilesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/profiles",
                (string? search, Guid? positionId, Guid? subdivisionId, bool? isActive,
                 int pageNumber, int pageSize, string? sortBy, string? sortDir,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new SearchProfilesQuery(
                            search,
                            positionId,
                            subdivisionId,
                            isActive,
                            pageNumber == 0 ? 1 : pageNumber,
                            pageSize == 0 ? 20 : pageSize,
                            sortBy,
                            sortDir),
                        ct))
            .WithName("SearchProfiles")
            .WithSummary("Search profiles (paged, filter by brand/category/active, sortable)")
            .RequirePermission(ProfilePermissions.Profiles.View);
    }
}

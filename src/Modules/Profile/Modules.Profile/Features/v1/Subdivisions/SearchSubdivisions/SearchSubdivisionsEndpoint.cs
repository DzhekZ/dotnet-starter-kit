using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.SearchSubdivisions;

public static class SearchSubdivisionsEndpoint
{
    internal static RouteHandlerBuilder MapSearchSubdivisionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/subdivisions",
                (string? search, Guid? parentCategoryId, int pageNumber, int pageSize,
                 string? sortBy, string? sortDir,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new SearchSubdivisionsQuery(
                            search,
                            parentCategoryId,
                            pageNumber == 0 ? 1 : pageNumber,
                            pageSize == 0 ? 50 : pageSize,
                            sortBy,
                            sortDir),
                        ct))
            .WithName("SearchSubdivisions")
            .WithSummary("Search subdivisions (paged, filter by parent, sortable)")
            .RequirePermission(ProfilePermissions.Subdivisions.View);
    }
}

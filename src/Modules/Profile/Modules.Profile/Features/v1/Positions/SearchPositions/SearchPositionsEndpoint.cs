using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Positions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Positions.SearchPositions;

public static class SearchPositionsEndpoint
{
    internal static RouteHandlerBuilder MapSearchPositionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/positions",
                (string? search, int pageNumber, int pageSize, string? sortBy, string? sortDir,
                 IMediator mediator, CancellationToken ct) =>
                    mediator.Send(
                        new SearchPositionsQuery(
                            search,
                            pageNumber == 0 ? 1 : pageNumber,
                            pageSize == 0 ? 20 : pageSize,
                            sortBy,
                            sortDir),
                        ct))
            .WithName("SearchPositions")
            .WithSummary("Search positions (paged, sortable)")
            .RequirePermission(ProfilePermissions.Positions.View);
    }
}

using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Modules.Profile.Contracts.v1.Positions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Positions.ListTrashedPositions;

public static class ListTrashedPositionsEndpoint
{
    internal static RouteHandlerBuilder MapListTrashedPositionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/positions/trash",
                async (int? pageNumber, int? pageSize, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListTrashedPositionsQuery(pageNumber ?? 1, pageSize ?? 20), ct)))
            .WithName("ListTrashedPositions")
            .WithSummary("List soft-deleted positions")
            .RequirePermission(ProfilePermissions.Positions.Restore);
    }
}

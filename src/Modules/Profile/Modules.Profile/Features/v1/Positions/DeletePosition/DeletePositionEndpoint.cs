using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Positions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Positions.DeletePosition;

public static class DeletePositionEndpoint
{
    internal static RouteHandlerBuilder MapDeletePositionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/positions/{brandId:guid}",
                async (Guid brandId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeletePositionCommand(brandId), ct);
                    return Results.NoContent();
                })
            .WithName("DeletePosition")
            .WithSummary("Delete a position")
            .RequirePermission(ProfilePermissions.Positions.Delete);
    }
}

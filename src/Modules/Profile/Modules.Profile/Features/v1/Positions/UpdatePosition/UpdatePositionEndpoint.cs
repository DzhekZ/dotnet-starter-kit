using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Positions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Positions.UpdatePosition;

public static class UpdatePositionEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePositionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/positions/{brandId:guid}",
                async (Guid brandId, UpdatePositionCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { PositionId = brandId };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("UpdatePosition")
            .WithSummary("Update a position")
            .RequirePermission(ProfilePermissions.Positions.Update);
    }
}

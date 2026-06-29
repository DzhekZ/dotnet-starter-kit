using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Modules.Profile.Contracts.v1.Positions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Positions.RestorePosition;

public static class RestorePositionEndpoint
{
    internal static RouteHandlerBuilder MapRestorePositionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/positions/{brandId:guid}/restore",
                async (Guid brandId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new RestorePositionCommand(brandId), ct)))
            .WithName("RestorePosition")
            .WithSummary("Restore a soft-deleted position")
            .RequirePermission(ProfilePermissions.Positions.Restore)
            .WithIdempotency();
    }
}

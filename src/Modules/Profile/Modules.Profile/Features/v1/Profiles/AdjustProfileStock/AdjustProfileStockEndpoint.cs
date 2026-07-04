using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.AdjustProfileStock;

public static class AdjustProfileStockEndpoint
{
    internal static RouteHandlerBuilder MapAdjustProfileStockEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPatch("/profiles/{productId:guid}/stock",
                async (Guid productId, AdjustProfileStockCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { ProductId = productId };
                    return Results.Ok(new { stock = await mediator.Send(command, ct) });
                })
            .WithName("AdjustProfileStock")
            .WithSummary("Adjust profile stock by a delta (+/-)")
            .RequirePermission(ProfilePermissions.Profiles.AdjustStock);
    }
}

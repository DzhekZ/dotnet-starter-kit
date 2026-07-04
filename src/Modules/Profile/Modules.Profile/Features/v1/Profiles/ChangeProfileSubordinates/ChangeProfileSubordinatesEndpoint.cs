using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.ChangeProfileSubordinates;

public static class ChangeProfileSubordinatesEndpoint
{
    internal static RouteHandlerBuilder MapChangeProductPriceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPatch("/profiles/{productId:guid}/subordinates",
                async (Guid productId, ChangeProfileSubordinatesCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { ProfileId = productId };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("ChangeProfileSubordinates")
            .WithSummary("Change a profile's subordinates")
            .RequirePermission(ProfilePermissions.Profiles.Update);
    }
}

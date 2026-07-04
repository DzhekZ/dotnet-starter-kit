using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.UpdateProfile;

public static class UpdateProfileEndpoint
{
    internal static RouteHandlerBuilder MapUpdateProfileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/profiles/{productId:guid}",
                async (Guid productId, UpdateProfileCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { ProfileId = productId };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("UpdateProfile")
            .WithSummary("Update a profile")
            .RequirePermission(ProfilePermissions.Profiles.Update);
    }
}

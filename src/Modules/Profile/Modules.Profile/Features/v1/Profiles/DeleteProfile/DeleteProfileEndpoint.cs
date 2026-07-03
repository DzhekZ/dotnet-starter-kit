using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.DeleteProfile;

public static class DeleteProfileEndpoint
{
    internal static RouteHandlerBuilder MapDeleteProfileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/profiles/{productId:guid}",
                async (Guid productId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteProfileCommand(productId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteProfile")
            .WithSummary("Delete a profile")
            .RequirePermission(ProfilePermissions.Profiles.Delete);
    }
}

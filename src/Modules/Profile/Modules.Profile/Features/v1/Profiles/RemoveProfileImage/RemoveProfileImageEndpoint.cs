using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Modules.Profile.Contracts.v1.Profiles.RemoveProfileImage;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.RemoveProfileImage;

public static class RemoveProfileImageEndpoint
{
    internal static RouteHandlerBuilder MapRemoveProductImageEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/profiles/{productId:guid}/images/{imageId:guid}",
                async (Guid productId, Guid imageId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new RemoveProfileImageCommand(productId, imageId), ct);
                    return Results.NoContent();
                })
            .WithName("RemoveProfileImage")
            .WithSummary("Detach an image from a profile")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProfilePermissions.Profiles.Update);
}

using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Modules.Profile.Contracts.v1.Profiles.SetProfileThumbnail;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.SetProfileThumbnail;

public static class SetProfileThumbnailEndpoint
{
    internal static RouteHandlerBuilder MapSetProfileThumbnailEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/profiles/{productId:guid}/images/{imageId:guid}/thumbnail",
                async (Guid productId, Guid imageId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new SetProfileThumbnailCommand(productId, imageId), ct);
                    return Results.NoContent();
                })
            .WithName("SetProfileThumbnail")
            .WithSummary("Promote a profile image to thumbnail (cover)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProfilePermissions.Profiles.Update);
}

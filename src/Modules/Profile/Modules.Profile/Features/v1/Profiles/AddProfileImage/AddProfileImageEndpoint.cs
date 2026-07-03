using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Modules.Profile.Contracts.Dtos;
using FSH.Modules.Profile.Contracts.v1.Profiles.AddProfileImage;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.AddProfileImage;

public static class AddProfileImageEndpoint
{
    internal static RouteHandlerBuilder MapAddProfileImageEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/profiles/{productId:guid}/images",
                async (Guid productId, [FromBody] AddImageBody body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new AddProfileImageCommand(productId, body.FileAssetId, body.Url),
                        ct)))
            .WithName("AddProfileImage")
            .WithSummary("Attach an image to a profile")
            .Produces<ProfileImageDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProfilePermissions.Profiles.Update);

    // Body shape: ProfileId comes from the route, so we only accept FileAssetId + Url in the body.
    public sealed record AddImageBody(Guid? FileAssetId, string Url);
}

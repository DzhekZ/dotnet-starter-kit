using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Modules.Profile.Contracts.v1.Profiles.ReorderProfileImages;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.ReorderProfileImages;

public static class ReorderProfileImagesEndpoint
{
    internal static RouteHandlerBuilder MapReorderProfileImagesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/profiles/{productId:guid}/images/order",
                async (Guid productId, [FromBody] ReorderBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ReorderProfileImagesCommand(productId, body.OrderedImageIds), ct);
                    return Results.NoContent();
                })
            .WithName("ReorderProfileImages")
            .WithSummary("Set the sort order of a profile's images")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProfilePermissions.Profiles.Update);

    public sealed record ReorderBody(IReadOnlyList<Guid> OrderedImageIds);
}

using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.RestoreProfile;

public static class RestoreProfileEndpoint
{
    internal static RouteHandlerBuilder MapRestoreProfileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/profiles/{productId:guid}/restore",
                async (Guid productId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new RestoreProfileCommand(productId), ct)))
            .WithName("RestoreProfile")
            .WithSummary("Restore a soft-deleted profile")
            .RequirePermission(ProfilePermissions.Profiles.Restore)
            .WithIdempotency();
    }
}

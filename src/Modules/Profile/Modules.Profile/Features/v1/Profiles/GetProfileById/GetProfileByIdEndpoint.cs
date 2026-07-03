using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.GetProfileById;

public static class GetProfileByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetProfileByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/profiles/{productId:guid}",
                (Guid productId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetProfileByIdQuery(productId), ct))
            .WithName("GetProfileById")
            .WithSummary("Get a profile by id")
            .RequirePermission(ProfilePermissions.Profiles.View);
    }
}

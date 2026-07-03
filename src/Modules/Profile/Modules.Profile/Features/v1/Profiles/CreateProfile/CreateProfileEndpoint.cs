using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.CreateProfile;

public static class CreateProfileEndpoint
{
    internal static RouteHandlerBuilder MapCreateProfileEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/profiles",
                async (CreateProfileCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateProfile")
            .WithSummary("Create a profile")
            .RequirePermission(ProfilePermissions.Profiles.Create)
            .WithIdempotency();
    }
}

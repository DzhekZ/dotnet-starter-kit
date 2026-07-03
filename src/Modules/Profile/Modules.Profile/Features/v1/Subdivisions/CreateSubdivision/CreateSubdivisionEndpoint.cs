using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.CreateSubdivision;

public static class CreateSubdivisionEndpoint
{
    internal static RouteHandlerBuilder MapCreateSubdivisionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/subdivisions",
                async (CreateSubdivisionCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateSubdivision")
            .WithSummary("Create a subdivision")
            .RequirePermission(ProfilePermissions.Subdivisions.Create)
            .WithIdempotency();
    }
}

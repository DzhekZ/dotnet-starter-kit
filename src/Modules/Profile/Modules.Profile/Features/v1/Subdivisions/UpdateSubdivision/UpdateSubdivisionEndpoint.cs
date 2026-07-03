using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.UpdateSubdivision;

public static class UpdateSubdivisionEndpoint
{
    internal static RouteHandlerBuilder MapUpdateSubdivisionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/subdivisions/{categoryId:guid}",
                async (Guid categoryId, UpdateSubdivisionCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { SubdivisionId = categoryId };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("UpdateSubdivision")
            .WithSummary("Update a subdivision")
            .RequirePermission(ProfilePermissions.Subdivisions.Update);
    }
}

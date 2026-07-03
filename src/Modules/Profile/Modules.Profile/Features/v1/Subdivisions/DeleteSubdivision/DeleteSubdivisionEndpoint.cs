using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.DeleteSubdivision;

public static class DeleteSubdivisionEndpoint
{
    internal static RouteHandlerBuilder MapDeleteSubdivisionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/subdivisions/{categoryId:guid}",
                async (Guid categoryId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteSubdivisionCommand(categoryId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteSubdivision")
            .WithSummary("Delete a subdivision")
            .RequirePermission(ProfilePermissions.Subdivisions.Delete);
    }
}

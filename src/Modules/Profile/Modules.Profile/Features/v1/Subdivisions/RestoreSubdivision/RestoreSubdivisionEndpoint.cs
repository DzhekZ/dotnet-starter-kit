using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.RestoreSubdivision;

public static class RestoreSubdivisionEndpoint
{
    internal static RouteHandlerBuilder MapRestoreSubdivisionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/subdivisions/{categoryId:guid}/restore",
                async (Guid categoryId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new RestoreSubdivisionCommand(categoryId), ct)))
            .WithName("RestoreSubdivision")
            .WithSummary("Restore a soft-deleted subdivision")
            .RequirePermission(ProfilePermissions.Subdivisions.Restore)
            .WithIdempotency();
    }
}

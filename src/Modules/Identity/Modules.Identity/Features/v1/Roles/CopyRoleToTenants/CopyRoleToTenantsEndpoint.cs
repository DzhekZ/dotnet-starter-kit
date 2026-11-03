using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.v1.Roles.CopyRoleToTenants;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Roles.CopyRoleToTenants;

public static class CopyRoleToTenantsEndpoint
{
    public static RouteHandlerBuilder MapCopyRoleToTenantsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/roles/{id:guid}/copy-to-tenants", async (string id, IMediator mediator, CancellationToken cancellationToken) =>
            TypedResults.Ok(await mediator.Send(new CopyRoleToTenantsCommand(id), cancellationToken)))
        .WithName("CopyRoleToTenants")
        .WithSummary("Copy role to tenants")
        .RequirePermission(IdentityPermissions.Roles.CopyToTenants)
        .WithDescription("Root tenant only. Creates a copy of a custom role, with its permissions, in every tenant that has no role of that name. Tenants that already have it are left untouched; root-only permissions are not copied.")
        .Produces<CopyRoleToTenantsResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}

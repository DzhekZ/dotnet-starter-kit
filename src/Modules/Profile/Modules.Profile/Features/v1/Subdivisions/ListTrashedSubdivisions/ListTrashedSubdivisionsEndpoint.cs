using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.ListTrashedSubdivisions;

public static class ListTrashedSubdivisionsEndpoint
{
    internal static RouteHandlerBuilder MapListTrashedSubdivisionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/subdivisions/trash",
                async (int? pageNumber, int? pageSize, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListTrashedSubdivisionsQuery(pageNumber ?? 1, pageSize ?? 20), ct)))
            .WithName("ListTrashedSubdivisions")
            .WithSummary("List soft-deleted subdivisions")
            .RequirePermission(ProfilePermissions.Subdivisions.Restore);
    }
}

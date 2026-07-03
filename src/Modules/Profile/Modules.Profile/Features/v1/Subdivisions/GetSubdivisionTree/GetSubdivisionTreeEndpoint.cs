using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.GetSubdivisionTree;

public static class GetSubdivisionTreeEndpoint
{
    internal static RouteHandlerBuilder MapGetSubdivisionTreeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/subdivisions/tree",
                (IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetSubdivisionTreeQuery(), ct))
            .WithName("GetSubdivisionTree")
            .WithSummary("Get the full subdivision tree")
            .RequirePermission(ProfilePermissions.Subdivisions.View);
    }
}

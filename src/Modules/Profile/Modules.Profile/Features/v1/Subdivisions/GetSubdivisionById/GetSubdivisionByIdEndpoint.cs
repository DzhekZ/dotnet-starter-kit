using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.GetSubdivisionById;

public static class GetSubdivisionByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetSubdivisionByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/subdivisions/{categoryId:guid}",
                (Guid categoryId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetSubdivisionByIdQuery(categoryId), ct))
            .WithName("GetSubdivisionById")
            .WithSummary("Get a subdivision by id")
            .RequirePermission(ProfilePermissions.Subdivisions.View);
    }
}

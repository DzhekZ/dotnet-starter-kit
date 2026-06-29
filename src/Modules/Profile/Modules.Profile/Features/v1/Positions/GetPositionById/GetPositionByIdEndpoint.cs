using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.v1.Positions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Positions.GetPositionById;

public static class GetPositionByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPositionByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/positions/{brandId:guid}",
                (Guid brandId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetPositionByIdQuery(brandId), ct))
            .WithName("GetBrandById")
            .WithSummary("Get a brand by id")
            .RequirePermission(ProfilePermissions.Positions.View);
    }
}

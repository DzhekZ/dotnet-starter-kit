using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Profile.Contracts.Authorization;
using FSH.Modules.Profile.Contracts.v1.Profiles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Profile.Features.v1.Profiles.ListTrashedProfiles;

public static class ListTrashedProfilesEndpoint
{
    internal static RouteHandlerBuilder MapListTrashedProfilesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/profiles/trash",
                async (int? pageNumber, int? pageSize, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListTrashedProfilesQuery(pageNumber ?? 1, pageSize ?? 20), ct)))
            .WithName("ListTrashedProfiles")
            .WithSummary("List soft-deleted profiles")
            .RequirePermission(ProfilePermissions.Profiles.Restore);
    }
}

using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Roles.CopyRoleToTenants;

/// <summary>Copies a custom role of the root tenant into every other tenant that does not have a role of that name.</summary>
public sealed record CopyRoleToTenantsCommand(string RoleId) : ICommand<CopyRoleToTenantsResponse>;

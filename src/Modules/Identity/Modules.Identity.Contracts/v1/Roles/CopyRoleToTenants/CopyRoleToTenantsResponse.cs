namespace FSH.Modules.Identity.Contracts.v1.Roles.CopyRoleToTenants;

public sealed record CopyRoleToTenantsResponse(IReadOnlyList<CopyRoleToTenantResult> Results);

/// <param name="TenantId">The tenant the role was copied to.</param>
/// <param name="Status">One of <see cref="CopyRoleStatuses"/>.</param>
/// <param name="Error">What went wrong; only set when <paramref name="Status"/> is <see cref="CopyRoleStatuses.Failed"/>.</param>
public sealed record CopyRoleToTenantResult(string TenantId, string Status, string? Error = null);

public static class CopyRoleStatuses
{
    /// <summary>The tenant had no role of that name; it was created with the permissions of the source role.</summary>
    public const string Created = nameof(Created);

    /// <summary>The tenant already has a role of that name; it was left untouched.</summary>
    public const string Skipped = nameof(Skipped);

    public const string Failed = nameof(Failed);
}

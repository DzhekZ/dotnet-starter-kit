using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// Takes a tenant out of the shared host's tenant store once a test is done with it.
/// <para>
/// Every integration test shares one host, and a job that walks all tenants sees whatever earlier tests left behind.
/// A tenant that was made to fail on purpose (an unreachable dedicated database, a schema dropped under it) fails such
/// a job for as long as the host lives — <c>MigrateLegacyPublicFileKeysJob</c> throws when any tenant fails — and
/// xUnit v3 runs the classes in a different order every run, so an unrelated test breaks only when it happens to run
/// after the one that left the tenant. Tests that create such a tenant remove it again.
/// </para>
/// </summary>
internal static class TenantCleanup
{
    public static async Task RemoveAsync(IServiceProvider services, string tenantId)
    {
        ArgumentNullException.ThrowIfNull(services);

        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>().RemoveAsync(tenantId);
    }
}

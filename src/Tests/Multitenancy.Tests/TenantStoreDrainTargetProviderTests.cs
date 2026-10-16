using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Multitenancy.Tests;

public class TenantStoreDrainTargetProviderTests
{
    private const string AcmeConnection = "Host=acme;Database=acme;Username=u;Password=p";

    [Fact]
    public async Task GetTargetsAsync_Should_ReturnDefaultPlusOneTargetPerDistinctConnectionString()
    {
        var provider = new TenantStoreDrainTargetProvider(StoreWith(
            Tenant("shared-a", connectionString: null),
            Tenant("shared-b", connectionString: null),
            Tenant("acme", AcmeConnection),
            Tenant("acme-2", AcmeConnection),
            Tenant("gone", "Host=gone;Database=gone;Username=u;Password=p", isActive: false)));

        var targets = await provider.GetTargetsAsync(CancellationToken.None);

        targets.Count.ShouldBe(2, "the default pass plus one per distinct active connection string");
        targets.ShouldContain(t => t.ConnectionString == null, "the default database is always drained");
        targets.Count(t => t.ConnectionString == AcmeConnection)
            .ShouldBe(1, "two tenants sharing a database must be drained once, not twice");
        targets.ShouldNotContain(t => t.TenantId == "gone", "inactive tenants are not drained");
    }

    [Fact]
    public async Task GetTargetsAsync_Should_ReturnOnlyTheDefault_When_NoTenantHasADedicatedDatabase()
    {
        var provider = new TenantStoreDrainTargetProvider(StoreWith(
            Tenant("shared-a", connectionString: null),
            Tenant("shared-b", connectionString: "   ")));

        var targets = await provider.GetTargetsAsync(CancellationToken.None);

        targets.ShouldHaveSingleItem().ConnectionString.ShouldBeNull(
            "a whitespace connection string is not a dedicated database");
    }

    private static IMultiTenantStore<AppTenantInfo> StoreWith(params AppTenantInfo[] tenants)
    {
        var store = Substitute.For<IMultiTenantStore<AppTenantInfo>>();
        store.GetAllAsync().Returns(tenants.AsEnumerable());
        return store;
    }

    private static AppTenantInfo Tenant(string id, string? connectionString, bool isActive = true) =>
        new(id, id, id)
        {
            ConnectionString = connectionString ?? string.Empty,
            IsActive = isActive,
        };
}

using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Eventing.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts.Events;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Eventing;

/// <summary>
/// Covers plan Phase 4. The outbox is only "transactional" if its row shares the caller's
/// transaction; before this, AddAsync committed on its own, so a business write that later rolled
/// back still published its event — and a crash between the two writes lost it.
///
/// Enlistment depends on every context in the scope sharing one DbConnection, which is asserted
/// directly here too: it is the load-bearing precondition, and a silent regression to
/// per-context connections would leave these rollbacks passing for the wrong reason.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class OutboxAtomicityTests
{
    private readonly FshWebApplicationFactory _factory;

    public OutboxAtomicityTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ModuleContextAndEventingContext_Should_ShareOneConnection()
    {
        var (scope, tenant) = await NewScopeAsync();
        using var scopeHandle = scope;
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);


        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var eventing = scope.ServiceProvider.GetRequiredService<EventingDbContext>();

        ReferenceEquals(catalog.Database.GetDbConnection(), eventing.Database.GetDbConnection())
            .ShouldBeTrue("a shared DbConnection is what lets the outbox write join the business transaction");
    }

    [Fact]
    public async Task OutboxWrite_Should_RollBack_With_TheBusinessTransaction()
    {
        var (scope, tenant) = await NewScopeAsync();
        using var scopeHandle = scope;
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

        var brand = Brand.Create($"atomicity-{Guid.CreateVersion7():N}", null, null);
        var eventId = Guid.CreateVersion7();

        await using (var transaction = await catalog.Database.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            catalog.Brands.Add(brand);
            await catalog.SaveChangesAsync(TestContext.Current.CancellationToken);

            await store.AddAsync(NewEvent(eventId), TestContext.Current.CancellationToken);

            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }

        await AssertOutboxRowAsync(eventId, shouldExist: false,
            "the outbox row must not survive a rolled-back business transaction");
        await AssertBrandAsync(brand.Id, shouldExist: false);
    }

    [Fact]
    public async Task OutboxWrite_Should_Commit_With_TheBusinessTransaction()
    {
        var (scope, tenant) = await NewScopeAsync();
        using var scopeHandle = scope;
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

        var brand = Brand.Create($"atomicity-{Guid.CreateVersion7():N}", null, null);
        var eventId = Guid.CreateVersion7();

        await using (var transaction = await catalog.Database.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            catalog.Brands.Add(brand);
            await catalog.SaveChangesAsync(TestContext.Current.CancellationToken);

            await store.AddAsync(NewEvent(eventId), TestContext.Current.CancellationToken);

            await transaction.CommitAsync(TestContext.Current.CancellationToken);
        }

        await AssertOutboxRowAsync(eventId, shouldExist: true,
            "enlisting must not swallow the write — a committed transaction keeps both rows");
        await AssertBrandAsync(brand.Id, shouldExist: true);
    }

    [Fact]
    public async Task OutboxWrite_Should_CommitOnItsOwn_When_OutsideATransaction()
    {
        var (scope, tenant) = await NewScopeAsync();
        using var scopeHandle = scope;
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        var eventId = Guid.CreateVersion7();

        await store.AddAsync(NewEvent(eventId), TestContext.Current.CancellationToken);

        await AssertOutboxRowAsync(eventId, shouldExist: true,
            "with no ambient transaction the store must behave exactly as it always has");
    }

    [Fact]
    public async Task OutboxWrite_Should_NotBeBlockedByTheStaleHandle_When_ASecondWriteFollowsACompletedTransaction()
    {
        var (scope, tenant) = await NewScopeAsync();
        using var scopeHandle = scope;
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

        var insideId = Guid.CreateVersion7();
        await using (var transaction = await catalog.Database.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            await store.AddAsync(NewEvent(insideId), TestContext.Current.CancellationToken);
            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }

        // The eventing context is still holding the transaction it joined; a naive implementation
        // would now fail or silently write into a completed transaction.
        var afterId = Guid.CreateVersion7();
        await store.AddAsync(NewEvent(afterId), TestContext.Current.CancellationToken);

        await AssertOutboxRowAsync(insideId, shouldExist: false, "rolled back with its transaction");
        await AssertOutboxRowAsync(afterId, shouldExist: true, "a later write must stand on its own again");
    }

    private static InvoiceIssuedIntegrationEvent NewEvent(Guid id) => new(
        id,
        DateTime.UtcNow,
        TestConstants.RootTenantId,
        $"corr-{id:N}",
        "Billing",
        Guid.CreateVersion7(),
        "INV-ATOMIC",
        10.00m,
        "USD",
        DateTime.UtcNow.AddDays(7),
        2026,
        8);

    private async Task AssertOutboxRowAsync(Guid eventId, bool shouldExist, string because)
    {
        // A fresh scope: the assertion must read committed state, not the writing context's tracker.
        var (scope, tenant) = await NewScopeAsync();
        using var scopeHandle = scope;
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var eventing = scope.ServiceProvider.GetRequiredService<EventingDbContext>();

        (await eventing.OutboxMessages.AsNoTracking().AnyAsync(m => m.Id == eventId))
            .ShouldBe(shouldExist, because);
    }

    private async Task AssertBrandAsync(Guid brandId, bool shouldExist)
    {
        var (scope, tenant) = await NewScopeAsync();
        using var scopeHandle = scope;
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        (await catalog.Brands.AsNoTracking().AnyAsync(b => b.Id == brandId))
            .ShouldBe(shouldExist, "the business row and the outbox row must share one fate");
    }

    /// <summary>
    /// Returns a scope plus the root tenant, leaving the caller to install the tenant context
    /// INLINE. Finbuckle keeps that context in an AsyncLocal, so a set inside an awaited helper
    /// does not flow back out — the multi-tenant entities in these tests would then fail with
    /// "MultiTenant Entity cannot be changed if TenantInfo is null".
    /// </summary>
    private async Task<(IServiceScope Scope, AppTenantInfo Tenant)> NewScopeAsync()
    {
        var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider
            .GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId);
        return (scope, tenant!);
    }
}

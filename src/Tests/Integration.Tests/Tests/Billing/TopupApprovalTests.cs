using System.Text.Json;
using System.Text.Json.Serialization;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Contracts.Dtos;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Billing;

/// <summary>
/// Integration tests for the operator-facing top-up request endpoints:
///   GET  /api/v1/billing/wallet/topup-requests  (cross-tenant for root, own-tenant for others)
///   POST /api/v1/billing/wallet/topup-requests/{id}/approve
///   POST /api/v1/billing/wallet/topup-requests/{id}/reject
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TopupApprovalTests
{
    private const string BillingBasePath = "/api/v1/billing";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TopupApprovalTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── Approve: happy path ──────────────────────────────────────────

    [Fact]
    public async Task Approve_Should_GenerateTopupInvoice_And_MarkRequestInvoiced()
    {
        // Arrange: seed a Pending TopupRequest for root tenant (inline tenant context per pattern).
        var requestId = await SeedPendingTopupRequestAsync(TestConstants.RootTenantId, 250m);

        // Act: as ROOT, approve the request.
        using var rootClient = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        using var approveResp = await rootClient.PostAsJsonAsync($"{BillingBasePath}/wallet/topup-requests/{requestId}/approve", new { note = "approved in integration test" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert: 200 + a non-empty invoiceId.
        approveResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var invoiceId = await approveResp.DeserializeAsync<Guid>(ct: TestContext.Current.CancellationToken);
        invoiceId.ShouldNotBe(Guid.Empty);

        // Assert: the request is now Invoiced with InvoiceId set.
        await InspectDirectAsync(TestConstants.RootTenantId, async db =>
        {
            var request = await db.TopupRequests.FindAsync(requestId);
            request.ShouldNotBeNull();
            request.Status.ShouldBe(TopupRequestStatus.Invoiced, "request must be Invoiced after approval");
            request.InvoiceId.ShouldBe(invoiceId, "request.InvoiceId must match the returned invoice id");
        });

        // Assert: an Issued Topup invoice exists for root tenant.
        await InspectDirectAsync(TestConstants.RootTenantId, async db =>
        {
            var invoice = await db.Invoices.FindAsync(invoiceId);
            invoice.ShouldNotBeNull();
            invoice.TenantId.ShouldBe(TestConstants.RootTenantId);
            invoice.Purpose.ShouldBe(InvoicePurpose.Topup);
            invoice.Status.ShouldBe(InvoiceStatus.Issued);
        });
    }

    // ─── Reject: happy path ───────────────────────────────────────────

    [Fact]
    public async Task Reject_Should_MarkRequestRejected_And_ReturnRequestId()
    {
        // Arrange: seed a Pending request.
        var requestId = await SeedPendingTopupRequestAsync(TestConstants.RootTenantId, 100m);

        // Act: as ROOT, reject the request.
        using var rootClient = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        using var rejectResp = await rootClient.PostAsJsonAsync($"{BillingBasePath}/wallet/topup-requests/{requestId}/reject", new { reason = "rejected in integration test" }, cancellationToken: TestContext.Current.CancellationToken);

        rejectResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var returnedId = await rejectResp.DeserializeAsync<Guid>(ct: TestContext.Current.CancellationToken);
        returnedId.ShouldBe(requestId);

        // Assert: request is now Rejected.
        await InspectDirectAsync(TestConstants.RootTenantId, async db =>
        {
            var request = await db.TopupRequests.FindAsync(requestId);
            request.ShouldNotBeNull();
            request.Status.ShouldBe(TopupRequestStatus.Rejected);
            request.DecisionNote.ShouldBe("rejected in integration test");
        });
    }

    // ─── Reject: double-reject on non-Pending returns 409 ────────────

    [Fact]
    public async Task Reject_Should_Return409Conflict_When_RequestIsAlreadyRejected()
    {
        // Arrange: seed a Pending request and perform a successful first rejection.
        var requestId = await SeedPendingTopupRequestAsync(TestConstants.RootTenantId, 120m);

        using var rootClient = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);

        // First reject — must succeed.
        using var firstRejectResp = await rootClient.PostAsJsonAsync($"{BillingBasePath}/wallet/topup-requests/{requestId}/reject", new { reason = "first rejection" }, cancellationToken: TestContext.Current.CancellationToken);
        firstRejectResp.StatusCode.ShouldBe(HttpStatusCode.OK, "first rejection must succeed");

        // Act: attempt a second rejection on the now-Rejected request.
        using var secondRejectResp = await rootClient.PostAsJsonAsync($"{BillingBasePath}/wallet/topup-requests/{requestId}/reject", new { reason = "duplicate rejection" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert: 409 Conflict — not 500.
        secondRejectResp.StatusCode.ShouldBe(HttpStatusCode.Conflict,
            "rejecting a non-Pending request must return 409 Conflict, not 500");
    }

    // ─── Cross-tenant: non-root cannot see other tenants' requests ────

    [Fact]
    public async Task GetTopupRequests_Should_HideOtherTenantsRequests_When_CallerIsNotRoot()
    {
        // Arrange: seed a Pending request for root tenant (TenantA).
        var rootRequestId = await SeedPendingTopupRequestAsync(TestConstants.RootTenantId, 150m);

        // Arrange: provision a fresh TenantB.
        using var rootClient = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tenantBId = $"topup-iso-{uniqueId}";
        var tenantBAdminEmail = $"topup-admin-{uniqueId}@tenant.com";
        await CreateTenantAsync(rootClient, tenantBId, tenantBAdminEmail);
        await TenantProvisioningWait.WaitForProvisioningAsync(rootClient, tenantBId);

        using var tenantBClient = await CreateTenantAdminClientWithRetryAsync(
            tenantBAdminEmail, TestConstants.DefaultPassword, tenantBId);

        // Act: as TenantB admin, GET /wallet/topup-requests (admin endpoint, own-tenant scoped).
        using var listResp = await tenantBClient.GetAsync($"{BillingBasePath}/wallet/topup-requests?pageNumber=1&pageSize=100", TestContext.Current.CancellationToken);
        listResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await ParseAsync<PagedResponse<TopupRequestDto>>(listResp);

        // Assert: root's request does NOT appear.
        page.Items.ShouldNotContain(r => r.Id == rootRequestId,
            "a non-root tenant must not see another tenant's top-up requests");

        // Sanity: root CAN see the request via the same endpoint.
        using var rootListResp = await rootClient.GetAsync($"{BillingBasePath}/wallet/topup-requests?pageNumber=1&pageSize=100", TestContext.Current.CancellationToken);
        rootListResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rootPage = await ParseAsync<PagedResponse<TopupRequestDto>>(rootListResp);
        rootPage.Items.ShouldContain(r => r.Id == rootRequestId,
            "root must be able to see its own request via the admin list endpoint");
    }

    // ─── List: root cross-tenant filter by tenantId ───────────────────

    [Fact]
    public async Task GetTopupRequests_Should_FilterByTenantId_When_CallerIsRoot()
    {
        // Arrange: seed a request for root.
        var requestId = await SeedPendingTopupRequestAsync(TestConstants.RootTenantId, 75m);

        using var rootClient = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);

        // Act: filter by root tenantId.
        using var filteredResp = await rootClient.GetAsync($"{BillingBasePath}/wallet/topup-requests?tenantId={TestConstants.RootTenantId}&pageNumber=1&pageSize=100", TestContext.Current.CancellationToken);
        filteredResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await ParseAsync<PagedResponse<TopupRequestDto>>(filteredResp);

        // Assert: the seeded request is present and all items belong to root.
        page.Items.ShouldContain(r => r.Id == requestId);
        page.Items.ShouldAllBe(r => r.TenantId == TestConstants.RootTenantId);
    }

    // ─── helpers ──────────────────────────────────────────────────────

    private async Task<Guid> SeedPendingTopupRequestAsync(string tenantId, decimal amount)
    {
        Guid id = Guid.Empty;
        await SeedDirectAsync(tenantId, async db =>
        {
            var request = TopupRequest.Create(tenantId, amount, "USD", "integration-test-seed", "test-user");
            db.TopupRequests.Add(request);
            await db.SaveChangesAsync();
            id = request.Id;
        });
        return id;
    }

    private async Task SeedDirectAsync(string tenantId, Func<BillingDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();

        // Finbuckle context must be set INLINE (AsyncLocal; lost across awaited helpers).
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();

        // BillingDbContext is not tenant-filtered so we only need a valid tenant in the store.
        // Fall back to root context for synthetic/cross-tenant seeding (root is always present).
        var tenant = await tenantStore.GetAsync(tenantId)
            ?? await tenantStore.GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        await action(db);
    }

    private async Task InspectDirectAsync(string tenantId, Func<BillingDbContext, Task> action)
        => await SeedDirectAsync(tenantId, action);

    private static async Task<T> ParseAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Expected success, got {(int)response.StatusCode} {response.StatusCode}. Body: {json}");
        }
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
               ?? throw new InvalidOperationException(
                   $"Failed to deserialize to {typeof(T).Name}. Body: {json}");
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
            }
            catch (HttpRequestException) when (i < maxRetries - 1)
            {
                await Task.Delay(1000);
            }
        }

        return await _auth.CreateAuthenticatedClientAsync(email, password, tenant);
    }
}

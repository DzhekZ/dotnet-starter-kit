using System.Text.Json;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.Authorization;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Integration.Tests.Tests.Auditing;

namespace Integration.Tests.Tests.Roles;

/// <summary>
/// <c>POST /identity/roles/{id}/copy-to-tenants</c>: a custom role of the root tenant is created, with its permissions, in the tenants that lack
/// it; the tenants that have it are left alone; root-only permissions stay behind; the operator is told in the bell and the work is in the audit
/// trail of each tenant it touched.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class RoleCopyToTenantsTests
{
    // A real IsRoot permission (MultitenancyPermissions.Tenants.Update).
    private const string RootPermission = "Permissions.Tenants.Update";
    private const string PlainPermission = IdentityPermissions.Groups.View;

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public RoleCopyToTenantsTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task CopyToTenants_Should_CreateTheRoleOnceWithoutRootPermissions_AuditItAndTellTheOperator()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"cp-{unique}";
        var adminEmail = $"cp-admin-{unique}@tenant.com";
        var roleName = $"Copied-{unique}";

        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await TenantProvisioningWait.WaitForProvisioningAsync(rootClient, tenantId);
        using var tenantAdmin = await CreateTenantAdminClientWithRetryAsync(adminEmail, TestConstants.DefaultPassword, tenantId);

        var roleId = await CreateRoleAsync(rootClient, roleName);
        await SetRolePermissionsAsync(rootClient, roleId, RootPermission, PlainPermission);

        // First copy: created in the new tenant.
        var first = await CopyAsync(rootClient, roleId);
        first.Single(r => r.TenantId == tenantId).Status.ShouldBe("Created");
        first.ShouldNotContain(r => r.TenantId == TestConstants.RootTenantId);

        var copiedPermissions = await GetRolePermissionsByNameAsync(tenantAdmin, roleName);
        copiedPermissions.ShouldContain(PlainPermission);
        copiedPermissions.ShouldNotContain(RootPermission, Case.Sensitive, "root-only permissions are not copied into another tenant");

        // Second copy: the tenant has the role now, so it is left alone.
        var second = await CopyAsync(rootClient, roleId);
        second.Single(r => r.TenantId == tenantId).Status.ShouldBe("Skipped");

        // Audit: the tenant sees its own event of the copy, made by the operator.
        var audited = await AuditTestHelper.PollForAuditAsync(
            tenantAdmin,
            a => a.EventType == AuditEventType.Activity
                 && a.Source == "Identity"
                 && a.TenantId == tenantId
                 && a.Tags.HasFlag(AuditTag.Guaranteed));
        audited.UserId.ShouldNotBeNullOrEmpty();

        // Bell: the outbox delivers the event, then the operator's inbox has the line.
        await OutboxDrain.DrainAsync(_factory.Services);
        var titles = await ListNotificationTitlesAsync(rootClient);
        titles.ShouldContain("Копирование роли завершено");
    }

    [Fact]
    public async Task CopyToTenants_Should_Return403_When_CallerIsATenantAdmin()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"cp-{unique}";
        var adminEmail = $"cp-admin-{unique}@tenant.com";

        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await TenantProvisioningWait.WaitForProvisioningAsync(rootClient, tenantId);
        using var tenantAdmin = await CreateTenantAdminClientWithRetryAsync(adminEmail, TestConstants.DefaultPassword, tenantId);

        // The permission is root-only, so not even the admin of a tenant holds it.
        var response = await tenantAdmin.PostAsync(
            $"{TestConstants.IdentityBasePath}/roles/{Guid.NewGuid()}/copy-to-tenants", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CopyToTenants_Should_Return400_When_RoleIsBuiltIn()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var adminRoleId = await FindRoleIdAsync(rootClient, "Admin");

        var response = await rootClient.PostAsync(
            $"{TestConstants.IdentityBasePath}/roles/{adminRoleId}/copy-to-tenants", content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<List<CopyResultRow>> CopyAsync(HttpClient rootClient, string roleId)
    {
        var response = await rootClient.PostAsync(
            $"{TestConstants.IdentityBasePath}/roles/{roleId}/copy-to-tenants", content: null, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return (await response.DeserializeAsync<CopyResponse>()).Results;
    }

    private static async Task<string> CreateRoleAsync(HttpClient client, string name)
    {
        var resp = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles",
            new { id = string.Empty, name, description = "copy-to-tenants test role" });
        resp.IsSuccessStatusCode.ShouldBeTrue($"Create role failed: {await resp.Content.ReadAsStringAsync()}");
        return (await resp.DeserializeAsync<RoleRow>()).Id;
    }

    private static async Task SetRolePermissionsAsync(HttpClient client, string roleId, params string[] permissions)
    {
        var resp = await client.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/{roleId}/permissions", new { roleId, permissions });
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
    }

    private static async Task<string> FindRoleIdAsync(HttpClient client, string name)
    {
        var resp = await client.GetAsync($"{TestConstants.IdentityBasePath}/roles?PageNumber=1&PageSize=200", TestContext.Current.CancellationToken);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await resp.DeserializeAsync<RolePage>()).Items.Single(r => r.Name == name).Id;
    }

    private static async Task<string> GetRolePermissionsByNameAsync(HttpClient tenantClient, string roleName)
    {
        var roleId = await FindRoleIdAsync(tenantClient, roleName);
        var resp = await tenantClient.GetAsync($"{TestConstants.IdentityBasePath}/{roleId}/permissions", TestContext.Current.CancellationToken);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await resp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<List<string>> ListNotificationTitlesAsync(HttpClient client)
    {
        var resp = await client.GetAsync("/api/v1/notifications/?pageNumber=1&pageSize=50", TestContext.Current.CancellationToken);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var titles = new List<string>();
        Collect(json.RootElement, titles);
        return titles;

        // Cyrillic is escaped in the raw body, so the titles are read as values rather than searched as text.
        static void Collect(JsonElement element, List<string> into)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (property.NameEquals("title") && property.Value.ValueKind == JsonValueKind.String)
                        {
                            into.Add(property.Value.GetString()!);
                        }
                        else
                        {
                            Collect(property.Value, into);
                        }
                    }

                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        Collect(item, into);
                    }

                    break;
            }
        }
    }

    private async Task<HttpClient> CreateTenantAdminClientWithRetryAsync(
        string email, string password, string tenant, int maxRetries = 30)
    {
        for (var i = 0; i < maxRetries; i++)
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

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var resp = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Tenant {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer"
        });
        resp.StatusCode.ShouldBe(HttpStatusCode.Created, await resp.Content.ReadAsStringAsync());
    }

    private sealed record RoleRow
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }

    private sealed record RolePage
    {
        public List<RoleRow> Items { get; init; } = [];
    }

    private sealed record CopyResultRow
    {
        public string TenantId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    private sealed record CopyResponse
    {
        public List<CopyResultRow> Results { get; init; } = [];
    }
}

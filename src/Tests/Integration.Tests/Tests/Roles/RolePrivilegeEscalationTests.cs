using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Identity.Claims;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests.Tests.Roles;

/// <summary>
/// Privilege-escalation guard. A NON-root tenant admin legitimately holds Roles.Create/Update, but
/// must NOT be able to grant ROOT-only permissions (Permissions.Tenants.* / Permissions.Platform.*)
/// to a role. <c>RoleService.FilterRootPermissions</c> previously stripped only names with a
/// "Permissions.Root." prefix — which matches NO real root permission — so the root permission was
/// persisted and the caller could escalate to operating on every tenant. These tests encode that
/// exact attack and the root-operator's legitimate counterpart.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class RolePrivilegeEscalationTests
{
    // A real IsRoot permission (MultitenancyPermissions.Tenants.Update — flagged IsRoot: true).
    private const string RootPermission = "Permissions.Tenants.Update";
    private const string AllowedPermission = IdentityPermissions.Groups.View;

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public RolePrivilegeEscalationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetPermissions_Should_OmitRootPermission_When_ClaimPlantedOnNonRootTenantRole()
    {
        // A root-only claim that reached a non-root tenant's Admin role outside the API (direct SQL, restored
        // backup) must not take effect: the effective permission set drops it.
        using var rootClient = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"esc-{unique}";
        var adminEmail = $"esc-admin-{unique}@tenant.com";
        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await TenantProvisioningWait.WaitForProvisioningAsync(rootClient, tenantId);

        // Planted BEFORE the tenant admin's first permission read, so the cache cannot predate the claim.
        // The Finbuckle tenant context is set inline: it is AsyncLocal and an awaited helper would lose it.
        using (var scope = _factory.Services.CreateScope())
        {
            var tenant = await scope.ServiceProvider
                .GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
                .GetAsync(tenantId);
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant!);

            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var adminRole = await db.Roles.SingleAsync(r => r.Name == RoleConstants.Admin, TestContext.Current.CancellationToken);
            db.RoleClaims.Add(new FshRoleClaim
            {
                RoleId = adminRole.Id,
                ClaimType = ClaimConstants.Permission,
                ClaimValue = RootPermission,
                CreatedBy = "test",
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var tenantAdmin = await CreateTenantAdminClientWithRetryAsync(
            adminEmail, TestConstants.DefaultPassword, tenantId);

        var response = await tenantAdmin.GetAsync($"{TestConstants.IdentityBasePath}/permissions");
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var effective = await response.DeserializeAsync<List<string>>();

        effective.ShouldNotContain(RootPermission,
            "a root-only claim on a non-root tenant's role must not take effect");
        effective.ShouldContain(AllowedPermission,
            "the tenant admin's ordinary permissions must be unaffected");
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_StripRootPermissions_When_CallerIsNonRootTenantAdmin()
    {
        using var rootClient = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var unique = Guid.NewGuid().ToString("N")[..8];
        var tenantId = $"esc-{unique}";
        var adminEmail = $"esc-admin-{unique}@tenant.com";
        await CreateTenantAsync(rootClient, tenantId, adminEmail);
        await TenantProvisioningWait.WaitForProvisioningAsync(rootClient, tenantId);

        using var tenantAdmin = await CreateTenantAdminClientWithRetryAsync(
            adminEmail, TestConstants.DefaultPassword, tenantId);

        // The tenant admin creates a role and attempts to grant it a ROOT-only permission alongside a
        // legitimate non-root one.
        var roleId = await CreateRoleAsync(tenantAdmin, $"EscRole-{unique}");
        await SetRolePermissionsAsync(tenantAdmin, roleId, RootPermission, AllowedPermission);

        var persisted = await GetRolePermissionsBodyAsync(tenantAdmin, roleId);
        persisted.Contains(RootPermission, StringComparison.Ordinal).ShouldBeFalse(
            "a non-root tenant admin must not be able to grant a root-only permission to a role");
        persisted.Contains(AllowedPermission, StringComparison.Ordinal).ShouldBeTrue(
            "non-root permissions in the same request must still be applied");
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_AllowRootPermissions_When_CallerIsRootOperator()
    {
        // Sanity counterpart: the root operator CAN assign root-only permissions; the filter only
        // restricts non-root callers.
        using var rootClient = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var unique = Guid.NewGuid().ToString("N")[..8];

        var roleId = await CreateRoleAsync(rootClient, $"RootEscRole-{unique}");
        await SetRolePermissionsAsync(rootClient, roleId, RootPermission);

        var persisted = await GetRolePermissionsBodyAsync(rootClient, roleId);
        persisted.Contains(RootPermission, StringComparison.Ordinal).ShouldBeTrue(
            "the root operator may assign root-only permissions");
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<string> CreateRoleAsync(HttpClient client, string name)
    {
        var resp = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles",
            new { id = string.Empty, name, description = "escalation test role" });
        resp.IsSuccessStatusCode.ShouldBeTrue($"Create role failed: {await resp.Content.ReadAsStringAsync()}");
        var role = await resp.DeserializeAsync<RoleRow>();
        return role.Id;
    }

    private static async Task SetRolePermissionsAsync(HttpClient client, string roleId, params string[] permissions)
    {
        var resp = await client.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/{roleId}/permissions", new { roleId, permissions });
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
    }

    private static async Task<string> GetRolePermissionsBodyAsync(HttpClient client, string roleId)
    {
        var resp = await client.GetAsync($"{TestConstants.IdentityBasePath}/{roleId}/permissions");
        resp.IsSuccessStatusCode.ShouldBeTrue($"Get role permissions failed: {await resp.Content.ReadAsStringAsync()}");
        return await resp.Content.ReadAsStringAsync();
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
}

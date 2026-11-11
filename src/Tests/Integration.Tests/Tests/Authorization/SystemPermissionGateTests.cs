using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Integration.Tests.Tests.Authorization;

/// <summary>
/// The tenant dashboard's Live activity and System health pages are backed by
/// <c>POST /api/v1/sse/token</c> (<see cref="SystemPermissions.Activity.View"/>) and
/// <c>GET /api/v1/health</c> (<see cref="SystemPermissions.Health.View"/>). Both permissions are
/// Basic, so the built-in roles keep access while a narrow custom role without them is refused.
/// The anonymous orchestrator probes (<c>/health/live</c>, <c>/health/ready</c>) stay open.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class SystemPermissionGateTests
{
    private const string SseTokenPath = "/api/v1/sse/token";
    private const string HealthReportPath = "/api/v1/health";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public SystemPermissionGateTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public void ActivityAndHealthPermissions_Should_BeRegistered_AsBasic()
    {
        var basic = PermissionConstants.Basic.Select(p => p.Name).ToList();

        basic.ShouldContain(SystemPermissions.Activity.View);
        basic.ShouldContain(SystemPermissions.Health.View);
    }

    [Fact]
    public async Task SseToken_Should_Return403_When_UserLacksActivityView()
    {
        using var client = await CreateUserClientAsync(roleName: null);

        var response = await client.PostAsync(SseTokenPath, content: null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task HealthReport_Should_Return403_When_UserLacksHealthView()
    {
        using var client = await CreateUserClientAsync(roleName: null);

        var response = await client.GetAsync(HealthReportPath, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task HealthReport_Should_Return401_When_Anonymous()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.GetAsync(HealthReportPath, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SseTokenAndHealthReport_Should_Succeed_For_BasicRole()
    {
        using var client = await CreateUserClientAsync(RoleConstants.Basic);

        var token = await client.PostAsync(SseTokenPath, content: null, TestContext.Current.CancellationToken);
        var health = await client.GetAsync(HealthReportPath, TestContext.Current.CancellationToken);

        token.StatusCode.ShouldBe(HttpStatusCode.OK);
        health.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReport_Should_ReturnPerCheckResults_For_Admin()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);

        var response = await client.GetAsync(HealthReportPath, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("\"status\":");
        body.ShouldContain("\"results\":");
    }

    [Fact]
    public async Task ReadinessProbe_Should_StayAnonymous()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Seeds a confirmed, active root-tenant user — with <paramref name="roleName"/> when given,
    /// otherwise with no role at all — and returns a client signed in as them. The Finbuckle
    /// tenant context is set inline because it is AsyncLocal.
    /// </summary>
    private async Task<HttpClient> CreateUserClientAsync(string? roleName)
    {
        const string password = TestConstants.DefaultPassword;
        var handle = $"sysperm-{Guid.NewGuid().ToString("N")[..8]}";
        var email = $"{handle}@example.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var tenant = await scope.ServiceProvider
                .GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
                .GetAsync(TestConstants.RootTenantId);
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
            var user = new FshUser
            {
                FirstName = "SystemPerm",
                LastName = "Probe",
                Email = email,
                UserName = handle,
                EmailConfirmed = true,
                IsActive = true,
            };

            var created = await userManager.CreateAsync(user, password);
            created.Succeeded.ShouldBeTrue(
                $"Seeding user failed: {string.Join(", ", created.Errors.Select(e => e.Description))}");

            if (roleName is not null)
            {
                var assigned = await userManager.AddToRoleAsync(user, roleName);
                assigned.Succeeded.ShouldBeTrue(
                    $"Assigning role {roleName} failed: {string.Join(", ", assigned.Errors.Select(e => e.Description))}");
            }
        }

        return await _auth.CreateAuthenticatedClientAsync(email, password, ct: TestContext.Current.CancellationToken);
    }
}

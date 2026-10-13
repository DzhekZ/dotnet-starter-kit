using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Roles;

[Collection(FshCollectionDefinition.Name)]
public sealed class RolePermissionTests
{
    private readonly AuthHelper _auth;

    public RolePermissionTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task GetRolePermissions_Should_ReturnOk_When_RoleExists()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);

        // Find the Admin role
        var rolesResponse = await client.GetAsync($"{TestConstants.IdentityBasePath}/roles", TestContext.Current.CancellationToken);
        var page = await rolesResponse.DeserializeAsync<PagedResponse<RoleDto>>(ct: TestContext.Current.CancellationToken);
        var adminRole = page.Items.First(r => r.Name == "Admin");

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/{adminRole.Id}/permissions", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateRolePermissions_Should_ReturnOk_When_PermissionsAreValid()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Create a role to assign permissions to
        var createResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/roles", new
        {
            id = string.Empty,
            name = $"PermRole-{uniqueId}",
            description = "Role for permission testing"
        }, cancellationToken: TestContext.Current.CancellationToken);
        var createdRole = await createResponse.DeserializeAsync<RoleDto>(ct: TestContext.Current.CancellationToken);

        var response = await client.PutAsJsonAsync($"{TestConstants.IdentityBasePath}/{createdRole.Id}/permissions", new
            {
                roleId = createdRole.Id,
                permissions = new[] { "Permissions.AuditTrails.View" }
            }, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

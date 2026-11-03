using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Services;

namespace Identity.Tests.Services;

/// <summary>
/// Tests for UserPermissionService.WithoutRootOnlyPermissions - a root-only permission claim that
/// reached a non-root tenant's role (outside the API) must not make it into the effective set.
/// </summary>
public sealed class UserPermissionServiceRootFilterTests
{
    private static readonly FshPermission RootPermission = new("Identity tests root probe", "Probe", "IdentityTestsRoot", IsRoot: true);
    private static readonly FshPermission PlainPermission = new("Identity tests plain probe", "Probe", "IdentityTestsPlain");

    public UserPermissionServiceRootFilterTests()
    {
        // The registry is process-wide and skips duplicates by name, so repeated registration is harmless.
        PermissionConstants.Register([RootPermission, PlainPermission]);
    }

    #region Happy Path

    [Fact]
    public void WithoutRootOnlyPermissions_Should_KeepRootPermissions_When_TenantIsRoot()
    {
        // Arrange
        List<string> permissions = [RootPermission.Name, PlainPermission.Name];

        // Act
        var result = UserPermissionService.WithoutRootOnlyPermissions(permissions, MultitenancyConstants.Root.Id);

        // Assert
        result.ShouldBe([RootPermission.Name, PlainPermission.Name], ignoreOrder: true);
    }

    [Fact]
    public void WithoutRootOnlyPermissions_Should_DropRootPermissions_When_TenantIsNotRoot()
    {
        // Arrange
        List<string> permissions = [RootPermission.Name, PlainPermission.Name];

        // Act
        var result = UserPermissionService.WithoutRootOnlyPermissions(permissions, "acme");

        // Assert
        result.ShouldBe([PlainPermission.Name]);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ROOT")]
    public void WithoutRootOnlyPermissions_Should_DropRootPermissions_When_TenantIsUnknownOrDiffersInCase(string? tenantId)
    {
        // Arrange
        List<string> permissions = [RootPermission.Name, PlainPermission.Name];

        // Act
        var result = UserPermissionService.WithoutRootOnlyPermissions(permissions, tenantId);

        // Assert
        result.ShouldBe([PlainPermission.Name]);
    }

    [Fact]
    public void WithoutRootOnlyPermissions_Should_ReturnEmpty_When_OnlyRootPermissionsInNonRootTenant()
    {
        // Act
        var result = UserPermissionService.WithoutRootOnlyPermissions([RootPermission.Name], "acme");

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region Exception

    [Fact]
    public void WithoutRootOnlyPermissions_Should_Throw_When_PermissionsIsNull()
    {
        Should.Throw<ArgumentNullException>(() => UserPermissionService.WithoutRootOnlyPermissions(null!, "acme"));
    }

    #endregion
}

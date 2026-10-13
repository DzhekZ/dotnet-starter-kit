using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Groups;

[Collection(FshCollectionDefinition.Name)]
public sealed class GroupMembershipTests
{
    private readonly AuthHelper _auth;

    public GroupMembershipTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task AddUsersToGroup_Should_ReturnOk_When_UsersExist()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Create a user and a group
        var userResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "GroupMember",
            lastName = "Test",
            email = $"member-{uniqueId}@example.com",
            userName = $"member-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        }, cancellationToken: TestContext.Current.CancellationToken);
        var user = await userResponse.DeserializeAsync<RegisterResult>(ct: TestContext.Current.CancellationToken);

        var groupResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/groups", new
        {
            name = $"MemberGroup-{uniqueId}",
            description = "Membership test group",
            isDefault = false,
            roleIds = new List<string>()
        }, cancellationToken: TestContext.Current.CancellationToken);
        var group = await groupResponse.DeserializeAsync<GroupDto>(ct: TestContext.Current.CancellationToken);

        // Add user to group
        var response = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/groups/{group.Id}/members", new { userIds = new[] { user.UserId } }, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetGroupMembers_Should_ReturnOk_When_GroupHasMembers()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Create user, group, and add membership
        var userResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "GroupQuery",
            lastName = "Test",
            email = $"grpquery-{uniqueId}@example.com",
            userName = $"grpquery-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        }, cancellationToken: TestContext.Current.CancellationToken);
        var user = await userResponse.DeserializeAsync<RegisterResult>(ct: TestContext.Current.CancellationToken);

        var groupResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/groups", new
        {
            name = $"QueryGroup-{uniqueId}",
            description = "Query members test",
            isDefault = false,
            roleIds = new List<string>()
        }, cancellationToken: TestContext.Current.CancellationToken);
        var group = await groupResponse.DeserializeAsync<GroupDto>(ct: TestContext.Current.CancellationToken);

        await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/groups/{group.Id}/members", new { userIds = new[] { user.UserId } }, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/groups/{group.Id}/members?pageNumber=1&pageSize=10", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveUserFromGroup_Should_ReturnNoContent_When_UserIsInGroup()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Create user, group, add, then remove
        var userResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = "RemoveMe",
            lastName = "Test",
            email = $"remove-{uniqueId}@example.com",
            userName = $"remove-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        }, cancellationToken: TestContext.Current.CancellationToken);
        var user = await userResponse.DeserializeAsync<RegisterResult>(ct: TestContext.Current.CancellationToken);

        var groupResponse = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/groups", new
        {
            name = $"RemoveGroup-{uniqueId}",
            description = "Removal test",
            isDefault = false,
            roleIds = new List<string>()
        }, cancellationToken: TestContext.Current.CancellationToken);
        var group = await groupResponse.DeserializeAsync<GroupDto>(ct: TestContext.Current.CancellationToken);

        await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/groups/{group.Id}/members", new { userIds = new[] { user.UserId } }, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync($"{TestConstants.IdentityBasePath}/groups/{group.Id}/members/{user.UserId}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}

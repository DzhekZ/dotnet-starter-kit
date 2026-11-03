using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web.Realtime;
using FSH.Modules.Identity.Contracts.Events;
using FSH.Modules.Notifications.Domain;
using FSH.Modules.Notifications.IntegrationEventHandlers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Notifications.Tests.IntegrationEventHandlers;

/// <summary>
/// A custom role copied into the tenants: what the line in the bell of the operator says, and that an event without an operator tells nobody. The
/// database is not reached here (the row is written by <c>SaveChanges</c>, which the integration tests cover).
/// </summary>
public sealed class RoleCopiedToTenantsIntegrationEventHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private const string RoleId = "3f2c9f1e-0000-0000-0000-000000000001";

    private static RoleCopiedToTenantsIntegrationEvent Event(string roleName = "Reviewer", int created = 3, int skipped = 1, int failed = 0, bool nobody = false)
        => new(Guid.NewGuid(), DateTime.UtcNow, "root", "correlation", "Identity", RoleId, roleName, created, skipped, failed, nobody ? null : UserId);

    [Fact]
    public void Create_Should_TellTheOperatorThatTheCopyIsDone()
    {
        Notification row = RoleCopiedToTenantsIntegrationEventHandler.Create(Event());

        row.UserId.ShouldBe(UserId.ToString());
        row.Type.ShouldBe("role.copied");
        row.Source.ShouldBe("Identity");
        row.Title.ShouldBe("Копирование роли завершено");
        row.Body.ShouldBe("Роль «Reviewer»: создана в тенантах: 3, уже была: 1.");
        row.Link.ShouldBe($"/roles/{RoleId}");
        row.ReadAtUtc.ShouldBeNull();
        using JsonDocument metadata = JsonDocument.Parse(row.MetadataJson ?? "{}");
        metadata.RootElement.GetProperty("roleId").GetString().ShouldBe(RoleId);
        metadata.RootElement.GetProperty("created").GetInt32().ShouldBe(3);
        metadata.RootElement.GetProperty("skipped").GetInt32().ShouldBe(1);
        metadata.RootElement.GetProperty("failed").GetInt32().ShouldBe(0);
    }

    [Fact]
    public void Body_Should_PointToTheAuditTrail_When_SomeTenantsFailed()
        => RoleCopiedToTenantsIntegrationEventHandler.Body(Event(failed: 2))
            .ShouldBe("Роль «Reviewer»: создана в тенантах: 3, уже была: 1, не удалось: 2. Подробности — в журнале аудита.");

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Body_Should_NotNameARoleThatHasNoName(string name)
        => RoleCopiedToTenantsIntegrationEventHandler.Body(Event(roleName: name)).ShouldStartWith("Роль:");

    [Fact]
    public void Create_Should_Refuse_AnEventThatNamesNoUser()
        => Should.Throw<ArgumentException>(() => RoleCopiedToTenantsIntegrationEventHandler.Create(Event(nobody: true)));

    [Fact]
    public async Task HandleAsync_Should_TellNobodyAndTouchNothing_WhenNobodyAskedForTheCopy()
    {
        var hub = Substitute.For<IHubContext<AppHub>>();
        var handler = new RoleCopiedToTenantsIntegrationEventHandler(
            null!,
            hub,
            Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>(),
            NullLogger<RoleCopiedToTenantsIntegrationEventHandler>.Instance);

        await handler.HandleAsync(Event(nobody: true), TestContext.Current.CancellationToken);

        hub.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Throw_When_TheAmbientTenantIsNotTheOneOfTheEvent()
    {
        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        var context = Substitute.For<IMultiTenantContext<AppTenantInfo>>();
        context.TenantInfo.Returns(new AppTenantInfo("acme", "acme", "acme"));
        accessor.MultiTenantContext.Returns(context);
        var handler = new RoleCopiedToTenantsIntegrationEventHandler(
            null!,
            Substitute.For<IHubContext<AppHub>>(),
            accessor,
            NullLogger<RoleCopiedToTenantsIntegrationEventHandler>.Instance);

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(Event(), TestContext.Current.CancellationToken));
    }
}

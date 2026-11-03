using System.Net;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Events;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Roles.CopyRoleToTenants;
using FSH.Modules.Identity.Features.v1.Roles.CopyRoleToTenants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Identity.Tests.Handlers;

/// <summary>
/// Tests for CopyRoleToTenantsCommandHandler - copies a custom root-tenant role into the tenants that lack it.
/// The root-tenant role service and the per-tenant role service are separate substitutes.
/// </summary>
public sealed class CopyRoleToTenantsCommandHandlerTests
{
    private const string RoleId = "role-1";

    private static readonly RoleDto SourceRole = new()
    {
        Id = RoleId,
        Name = "Reviewer",
        Description = "Reviews documents",
        Permissions = ["Permissions.Documents.View", "Permissions.Tenants.Update"],
    };

    private readonly IRoleService _rootRoles = Substitute.For<IRoleService>();
    private readonly IRoleService _tenantRoles = Substitute.For<IRoleService>();
    private readonly IMultiTenantStore<AppTenantInfo> _store = Substitute.For<IMultiTenantStore<AppTenantInfo>>();
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
    private readonly ICurrentUserInitializer _userInitializer = Substitute.For<ICurrentUserInitializer>();
    private readonly IOutboxWriter _outbox = Substitute.For<IOutboxWriter>();
    private readonly IAuditPublisher _audit = Substitute.For<IAuditPublisher>();
    private readonly List<AuditEnvelope> _audited = [];
    private readonly Guid _operatorId = Guid.NewGuid();
    private readonly CopyRoleToTenantsCommandHandler _sut;

    public CopyRoleToTenantsCommandHandlerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IMultiTenantContextSetter>());
        services.AddSingleton(_userInitializer);
        services.AddSingleton(_tenantRoles);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.GetUserId().Returns(_operatorId);
        currentUser.Name.Returns("operator");
#pragma warning disable CA2012 // NSubstitute set-up: the ValueTask is the call being configured
        _audit.When(a => a.PublishAsync(Arg.Any<IAuditEvent>(), Arg.Any<CancellationToken>()))
            .Do(call => _audited.Add((AuditEnvelope)call.Arg<IAuditEvent>()));
#pragma warning restore CA2012

        SetCurrentTenant(MultitenancyConstants.Root.Id);
        _rootRoles.GetWithPermissionsAsync(RoleId, Arg.Any<CancellationToken>()).Returns(SourceRole);
        _tenantRoles.GetRolesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Page());
        _tenantRoles.CreateOrUpdateRoleAsync(string.Empty, SourceRole.Name, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RoleDto { Id = "copy-1", Name = SourceRole.Name });

        _sut = new CopyRoleToTenantsCommandHandler(
            _rootRoles,
            _store,
            _accessor,
            currentUser,
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            _outbox,
            _audit,
            TimeProvider.System,
            NullLogger<CopyRoleToTenantsCommandHandler>.Instance);
    }

    private static PagedResponse<RoleDto> Page(params string[] names) =>
        new() { Items = [.. names.Select(n => new RoleDto { Id = n, Name = n })] };

    private static AppTenantInfo Tenant(string id) => new(id, id, id);

    private void SetCurrentTenant(string tenantId) =>
        _accessor.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(Tenant(tenantId)));

    private void SetTenants(params string[] ids) =>
        _store.GetAllAsync().Returns(ids.Select(Tenant).ToList());

    #region Happy Path

    [Fact]
    public async Task Handle_Should_CreateRoleWithPermissions_When_TenantHasNoRoleOfThatName()
    {
        // Arrange
        SetTenants(MultitenancyConstants.Root.Id, "acme");

        // Act
        var result = await _sut.Handle(new CopyRoleToTenantsCommand(RoleId), TestContext.Current.CancellationToken);

        // Assert
        result.Results.ShouldBe([new CopyRoleToTenantResult("acme", CopyRoleStatuses.Created)]);
        await _tenantRoles.Received(1).CreateOrUpdateRoleAsync(
            string.Empty, "Reviewer", "Reviews documents", Arg.Any<CancellationToken>());
        // The tenant's role service is what drops the root-only permission, so the handler hands over all of them.
        await _tenantRoles.Received(1).UpdatePermissionsAsync(
            "copy-1",
            Arg.Is<List<string>>(p => p.SequenceEqual(SourceRole.Permissions!)),
            Arg.Any<CancellationToken>());
        _userInitializer.Received(1).SetCurrentUserId(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_SkipTenant_When_RoleOfThatNameExistsIgnoringCase()
    {
        // Arrange
        SetTenants("acme");
        _tenantRoles.GetRolesAsync(Arg.Any<int>(), Arg.Any<int>(), "Reviewer", Arg.Any<CancellationToken>())
            .Returns(Page("REVIEWER"));

        // Act
        var result = await _sut.Handle(new CopyRoleToTenantsCommand(RoleId), TestContext.Current.CancellationToken);

        // Assert
        result.Results.ShouldBe([new CopyRoleToTenantResult("acme", CopyRoleStatuses.Skipped)]);
        await _tenantRoles.DidNotReceiveWithAnyArgs().CreateOrUpdateRoleAsync(default!, default!, default!, default);
        await _tenantRoles.DidNotReceiveWithAnyArgs().UpdatePermissionsAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_Should_NotUpdatePermissions_When_SourceRoleHasNone()
    {
        // Arrange
        SetTenants("acme");
        _rootRoles.GetWithPermissionsAsync(RoleId, Arg.Any<CancellationToken>())
            .Returns(SourceRole with { Permissions = [] });

        // Act
        var result = await _sut.Handle(new CopyRoleToTenantsCommand(RoleId), TestContext.Current.CancellationToken);

        // Assert
        result.Results.Single().Status.ShouldBe(CopyRoleStatuses.Created);
        await _tenantRoles.DidNotReceiveWithAnyArgs().UpdatePermissionsAsync(default!, default!, default);
    }

    #endregion

    #region Audit and bell

    [Fact]
    public async Task Handle_Should_AuditEveryTenantAndTheSummary_NamingTheOperatorAndTheTenant()
    {
        // Arrange - "acme" gets the role, "beta" already has it.
        SetTenants("acme", "beta");
        _tenantRoles.GetRolesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Page(), Page("Reviewer"));

        // Act
        await _sut.Handle(new CopyRoleToTenantsCommand(RoleId), TestContext.Current.CancellationToken);

        // Assert
        _audited.Select(e => (e.TenantId, Name: ((ActivityEventPayload)e.Payload).Name)).ShouldBe(
        [
            ("acme", "role.copy-to-tenant"),
            ("beta", "role.copy-to-tenant"),
            (MultitenancyConstants.Root.Id, "role.copy-to-tenants"),
        ]);
        _audited.ShouldAllBe(e =>
            e.UserId == _operatorId.ToString()
            && e.UserName == "operator"
            && e.EventType == AuditEventType.Activity
            && e.Source == "Identity"
            && e.Tags.HasFlag(AuditTag.Guaranteed));
        _audited[0].Severity.ShouldBe(AuditSeverity.Information);
    }

    [Fact]
    public async Task Handle_Should_AuditAFailedTenantAsAWarning()
    {
        // Arrange
        SetTenants("acme");
        _tenantRoles.UpdatePermissionsAsync("copy-1", Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<string>(new CustomException("permission store is down")));

        // Act
        await _sut.Handle(new CopyRoleToTenantsCommand(RoleId), TestContext.Current.CancellationToken);

        // Assert - the tenant event and the summary both warn.
        _audited.Select(e => e.Severity).ShouldBe([AuditSeverity.Warning, AuditSeverity.Warning]);
    }

    [Fact]
    public async Task Handle_Should_QueueTheBellEventForTheOperatorWithTheCounts()
    {
        // Arrange - one created, one skipped.
        SetTenants("acme", "beta");
        _tenantRoles.GetRolesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Page(), Page("Reviewer"));

        // Act
        await _sut.Handle(new CopyRoleToTenantsCommand(RoleId), TestContext.Current.CancellationToken);

        // Assert
        await _outbox.Received(1).AddAsync(
            Arg.Is<IIntegrationEvent>(e =>
                e is RoleCopiedToTenantsIntegrationEvent
                && ((RoleCopiedToTenantsIntegrationEvent)e).TenantId == MultitenancyConstants.Root.Id
                && ((RoleCopiedToTenantsIntegrationEvent)e).RoleId == RoleId
                && ((RoleCopiedToTenantsIntegrationEvent)e).RoleName == "Reviewer"
                && ((RoleCopiedToTenantsIntegrationEvent)e).Created == 1
                && ((RoleCopiedToTenantsIntegrationEvent)e).Skipped == 1
                && ((RoleCopiedToTenantsIntegrationEvent)e).Failed == 0
                && ((RoleCopiedToTenantsIntegrationEvent)e).RequestedBy == _operatorId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_StillReturnTheResults_When_AuditAndBellFail()
    {
        // Arrange
        SetTenants("acme");
#pragma warning disable CA2012 // NSubstitute set-up: the ValueTask is the call being configured
        _audit.When(a => a.PublishAsync(Arg.Any<IAuditEvent>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("channel closed"));
#pragma warning restore CA2012
        _outbox.AddAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException("outbox down")));

        // Act
        var result = await _sut.Handle(new CopyRoleToTenantsCommand(RoleId), TestContext.Current.CancellationToken);

        // Assert - the copy was done; the side channels only log.
        result.Results.ShouldBe([new CopyRoleToTenantResult("acme", CopyRoleStatuses.Created)]);
    }

    #endregion

    #region Exception

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_CurrentTenantIsNotRoot()
    {
        // Arrange
        SetCurrentTenant("acme");

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(
            () => _sut.Handle(new CopyRoleToTenantsCommand(RoleId), TestContext.Current.CancellationToken).AsTask());
        await _rootRoles.DidNotReceiveWithAnyArgs().GetWithPermissionsAsync(default!, default);
        _audited.ShouldBeEmpty();
        await _outbox.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Basic")]
    public async Task Handle_Should_ThrowBadRequest_When_RoleIsBuiltIn(string name)
    {
        // Arrange
        SetTenants("acme");
        _rootRoles.GetWithPermissionsAsync(RoleId, Arg.Any<CancellationToken>()).Returns(SourceRole with { Name = name });

        // Act
        var ex = await Should.ThrowAsync<CustomException>(
            () => _sut.Handle(new CopyRoleToTenantsCommand(RoleId), TestContext.Current.CancellationToken).AsTask());

        // Assert
        ex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await _tenantRoles.DidNotReceiveWithAnyArgs().CreateOrUpdateRoleAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task Handle_Should_ReportFailureRemoveRoleAndContinue_When_PermissionsFailForOneTenant()
    {
        // Arrange - tenants are visited in ordinal order: "acme" fails, "beta" succeeds.
        SetTenants("beta", "acme");
        _tenantRoles.UpdatePermissionsAsync("copy-1", Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromException<string>(new CustomException("permission store is down")),
                _ => Task.FromResult("permissions updated"));

        // Act
        var result = await _sut.Handle(new CopyRoleToTenantsCommand(RoleId), TestContext.Current.CancellationToken);

        // Assert
        result.Results.ShouldBe(
        [
            new CopyRoleToTenantResult("acme", CopyRoleStatuses.Failed, "permission store is down"),
            new CopyRoleToTenantResult("beta", CopyRoleStatuses.Created),
        ]);
        await _tenantRoles.Received(1).DeleteRoleAsync("copy-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_HideDetails_When_FailureIsNotACustomException()
    {
        // Arrange
        SetTenants("acme");
        _tenantRoles.GetRolesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<PagedResponse<RoleDto>>(new InvalidOperationException("connection string leaked")));

        // Act
        var result = await _sut.Handle(new CopyRoleToTenantsCommand(RoleId), TestContext.Current.CancellationToken);

        // Assert
        var failed = result.Results.Single();
        failed.Status.ShouldBe(CopyRoleStatuses.Failed);
        failed.Error.ShouldNotBeNull().ShouldNotContain("connection string");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_Should_ReturnNoResults_When_RootIsTheOnlyTenant()
    {
        // Arrange
        SetTenants(MultitenancyConstants.Root.Id);

        // Act
        var result = await _sut.Handle(new CopyRoleToTenantsCommand(RoleId), TestContext.Current.CancellationToken);

        // Assert
        result.Results.ShouldBeEmpty();
    }

    #endregion
}

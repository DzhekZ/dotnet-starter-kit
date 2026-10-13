using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Tickets.Data;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Tickets;

[Collection(FshCollectionDefinition.Name)]
public sealed class TicketsEndpointTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public TicketsEndpointTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── happy path ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateTicket_Should_AssignSequentialNumber_And_StartOpen_When_NoAssignee()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var title = UniqueTitle("Open");

        var ticketId = await CreateAsync(client, title);

        var fetched = await GetAsync(client, ticketId);
        fetched.Number.ShouldStartWith("TK-");
        fetched.Title.ShouldBe(title);
        fetched.Status.ShouldBe("Open");
        fetched.AssignedToUserId.ShouldBeNull();
        fetched.CommentCount.ShouldBe(0);
    }

    [Fact]
    public async Task CreateTicket_Should_StartInProgress_When_AssignedAtCreation()
    {
        // Tickets created with an assignee skip the Open state (an owner is already driving it).
        // The aggregate's Create() encodes this rule.
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var assigneeId = Guid.NewGuid();

        var createResponse = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = UniqueTitle("Assigned"),
            description = (string?)null,
            priority = "High",
            assignedToUserId = assigneeId,
        }, cancellationToken: TestContext.Current.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ticketId = await createResponse.DeserializeAsync<Guid>(ct: TestContext.Current.CancellationToken);

        var fetched = await GetAsync(client, ticketId);
        fetched.Status.ShouldBe("InProgress");
        fetched.AssignedToUserId.ShouldBe(assigneeId);
        fetched.Priority.ShouldBe("High");
    }

    [Fact]
    public async Task SearchTickets_Should_FilterBy_Status()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var openTitle = UniqueTitle("OpenSearch");
        var openId = await CreateAsync(client, openTitle);

        var resolvedTitle = UniqueTitle("ResolvedSearch");
        var resolvedId = await CreateAsync(client, resolvedTitle);
        await ResolveAsync(client, resolvedId, "fixed");

        var resolvedListResponse = await client.GetAsync($"{TestConstants.TicketsBasePath}/tickets?status=Resolved&pageSize=200", TestContext.Current.CancellationToken);
        var resolvedPage = await resolvedListResponse.DeserializeAsync<PagedResult<TicketDto>>(ct: TestContext.Current.CancellationToken);
        resolvedPage.Items.ShouldContain(t => t.Id == resolvedId);
        resolvedPage.Items.ShouldNotContain(t => t.Id == openId,
            "Status=Resolved must not include Open tickets.");
    }

    // ─── state machine ───────────────────────────────────────────────

    [Fact]
    public async Task AssignTicket_Should_TransitionTo_InProgress_From_Open()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var ticketId = await CreateAsync(client, UniqueTitle("ToBeAssigned"));
        var assigneeId = Guid.NewGuid();

        var assignResponse = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}/assign", new { assigneeUserId = assigneeId }, cancellationToken: TestContext.Current.CancellationToken);
        assignResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await GetAsync(client, ticketId);
        fetched.Status.ShouldBe("InProgress");
        fetched.AssignedToUserId.ShouldBe(assigneeId);
    }

    [Fact]
    public async Task UnassignTicket_Should_TransitionBack_To_Open()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var ticketId = await CreateAsync(client, UniqueTitle("Unassign"));
        await AssignAsync(client, ticketId, Guid.NewGuid());

        var unassignResponse = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}/assign", new { assigneeUserId = (Guid?)null }, cancellationToken: TestContext.Current.CancellationToken);
        unassignResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await GetAsync(client, ticketId);
        fetched.Status.ShouldBe("Open");
        fetched.AssignedToUserId.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveTicket_Should_PersistNote_And_SetResolvedAt()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var ticketId = await CreateAsync(client, UniqueTitle("ResolveMe"));

        await ResolveAsync(client, ticketId, "Root cause: missing config flag.");

        var fetched = await GetAsync(client, ticketId);
        fetched.Status.ShouldBe("Resolved");
        fetched.ResolutionNote.ShouldBe("Root cause: missing config flag.");
        fetched.ResolvedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReopenTicket_Should_ClearResolution_And_RestoreInProgress_When_StillAssigned()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var ticketId = await CreateAsync(client, UniqueTitle("Reopen"));
        await AssignAsync(client, ticketId, Guid.NewGuid());
        await ResolveAsync(client, ticketId, "shipped fix");

        var reopenResponse = await client.PostAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}/reopen", content: null, cancellationToken: TestContext.Current.CancellationToken);
        reopenResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await GetAsync(client, ticketId);
        // Reopened tickets fall back to their assignment state (still assigned here, so InProgress);
        // resolution metadata is wiped so a future resolve gets its own audit window.
        fetched.Status.ShouldBe("InProgress");
        fetched.ResolutionNote.ShouldBeNull();
        fetched.ResolvedAtUtc.ShouldBeNull();
    }

    // ─── comments ────────────────────────────────────────────────────

    [Fact]
    public async Task AddComment_Should_Persist_And_BumpCommentCount()
    {
        // Previously failed: TicketComment.Id lacked ValueGeneratedNever(), so EF tracked the nav-collection child
        // as Modified (UPDATE 0 rows) not Added → DbUpdateConcurrencyException. Fixed in TicketCommentConfiguration.
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var ticketId = await CreateAsync(client, UniqueTitle("Commentable"));

        var commentResponse = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}/comments", new { body = "First comment from integration test." }, cancellationToken: TestContext.Current.CancellationToken);
        commentResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var commentsResponse = await client.GetAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}/comments", TestContext.Current.CancellationToken);
        var comments = await commentsResponse.DeserializeAsync<TicketCommentDto[]>(ct: TestContext.Current.CancellationToken);
        comments.Length.ShouldBe(1);
        comments[0].Body.ShouldBe("First comment from integration test.");

        var fetched = await GetAsync(client, ticketId);
        fetched.CommentCount.ShouldBe(1);
    }

    // ─── soft delete + restore ───────────────────────────────────────

    [Fact]
    public async Task DeleteTicket_Should_HideFromSearch_But_Show_In_Trash()
    {
        // No DELETE endpoint exists yet, but EF's interceptor soft-deletes on Remove, so we drive it
        // via the DbContext through the test factory's service scope. (Stays valid once a DELETE endpoint ships.)
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var ticketId = await CreateAsync(client, UniqueTitle("Soft"));

        await SoftDeleteTicketAsync(ticketId);

        var search = await client.GetAsync($"{TestConstants.TicketsBasePath}/tickets?pageNumber=1&pageSize=200", TestContext.Current.CancellationToken);
        var page = await search.DeserializeAsync<PagedResult<TicketDto>>(ct: TestContext.Current.CancellationToken);
        page.Items.ShouldNotContain(t => t.Id == ticketId,
            "Search must not include soft-deleted tickets.");

        var trashResponse = await client.GetAsync($"{TestConstants.TicketsBasePath}/tickets/trash?pageNumber=1&pageSize=50", TestContext.Current.CancellationToken);
        trashResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var trash = await trashResponse.DeserializeAsync<PagedResult<TicketDto>>(ct: TestContext.Current.CancellationToken);
        var trashed = trash.Items.FirstOrDefault(t => t.Id == ticketId);
        trashed.ShouldNotBeNull();
        trashed.DeletedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task RestoreTicket_Should_BringBack_DeletedTicket()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);
        var ticketId = await CreateAsync(client, UniqueTitle("Restorable"));

        await SoftDeleteTicketAsync(ticketId);

        var restoreResponse = await client.PostAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}/restore", content: null, cancellationToken: TestContext.Current.CancellationToken);
        restoreResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await GetAsync(client, ticketId);
        fetched.DeletedOnUtc.ShouldBeNull();
    }

    // ─── auth gating ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateTicket_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title = UniqueTitle("Unauthed"),
        }, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTicketById_Should_Return404_When_TicketDoesNotExist()
    {
        using var client = await _auth.CreateRootAdminClientAsync(TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"{TestConstants.TicketsBasePath}/tickets/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string UniqueTitle(string prefix) =>
        $"Ticket-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    private static async Task<Guid> CreateAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync($"{TestConstants.TicketsBasePath}/tickets", new
        {
            title,
            description = (string?)null,
            priority = "Medium",
            assignedToUserId = (Guid?)null,
        });
        response.EnsureSuccessStatusCode();
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<TicketDto> GetAsync(HttpClient client, Guid ticketId)
    {
        var response = await client.GetAsync($"{TestConstants.TicketsBasePath}/tickets/{ticketId}");
        response.EnsureSuccessStatusCode();
        return await response.DeserializeAsync<TicketDto>();
    }

    private static async Task AssignAsync(HttpClient client, Guid ticketId, Guid? assigneeUserId)
    {
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/assign",
            new { assigneeUserId });
        response.EnsureSuccessStatusCode();
    }

    private static async Task ResolveAsync(HttpClient client, Guid ticketId, string resolutionNote)
    {
        var response = await client.PostAsJsonAsync(
            $"{TestConstants.TicketsBasePath}/tickets/{ticketId}/resolve",
            new { resolutionNote });
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Soft-deletes a ticket directly via the DbContext, since a DELETE
    /// endpoint isn't part of the initial Tickets MVP. The framework's
    /// AuditableEntitySaveChangesInterceptor turns Remove() into a soft
    /// delete (sets IsDeleted, DeletedOnUtc, DeletedBy).
    /// </summary>
    private async Task SoftDeleteTicketAsync(Guid ticketId)
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await store.GetAsync(MultitenancyConstants.Root.Id)
            ?? throw new InvalidOperationException("Root tenant not found.");

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var dbContext = scope.ServiceProvider.GetRequiredService<TicketsDbContext>();
        var ticket = await dbContext.Tickets.FirstAsync(t => t.Id == ticketId);
        dbContext.Tickets.Remove(ticket);
        await dbContext.SaveChangesAsync();
    }
}

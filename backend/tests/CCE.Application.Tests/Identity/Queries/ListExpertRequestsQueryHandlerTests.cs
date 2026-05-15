using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Queries.ListExpertRequests;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Identity.Queries;

public class ListExpertRequestsQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_requests_exist()
    {
        var db = BuildDb(System.Array.Empty<ExpertRegistrationRequest>(), System.Array.Empty<User>());
        var sut = new ListExpertRequestsQueryHandler(db);

        var result = await sut.Handle(new ListExpertRequestsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_requests_with_requester_user_names_populated()
    {
        var clock = new FakeSystemClock();
        var aliceId = System.Guid.NewGuid();
        var bobId = System.Guid.NewGuid();

        var aliceRequest = ExpertRegistrationRequest.Submit(aliceId, "سيرة أليس", "Alice Bio", new[] { "energy", "solar" }, clock);
        var bobRequest = ExpertRegistrationRequest.Submit(bobId, "سيرة بوب", "Bob Bio", new[] { "wind" }, clock);

        var users = new[]
        {
            BuildUser(aliceId, "alice@cce.local", "alice"),
            BuildUser(bobId, "bob@cce.local", "bob"),
        };

        var db = BuildDb(new[] { aliceRequest, bobRequest }, users);
        var sut = new ListExpertRequestsQueryHandler(db);

        var result = await sut.Handle(new ListExpertRequestsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);

        var aliceItem = result.Items.Single(i => i.RequestedById == aliceId);
        aliceItem.RequestedByUserName.Should().Be("alice");
        aliceItem.RequestedBioEn.Should().Be("Alice Bio");
        aliceItem.RequestedTags.Should().BeEquivalentTo(new[] { "energy", "solar" });
        aliceItem.Status.Should().Be(ExpertRegistrationStatus.Pending);

        var bobItem = result.Items.Single(i => i.RequestedById == bobId);
        bobItem.RequestedByUserName.Should().Be("bob");
    }

    [Fact]
    public async Task Status_filter_restricts_results()
    {
        var clock = new FakeSystemClock();
        var aliceId = System.Guid.NewGuid();
        var adminId = System.Guid.NewGuid();

        var pendingRequest = ExpertRegistrationRequest.Submit(aliceId, "سيرة", "Bio", new[] { "energy" }, clock);
        var approvedRequest = ExpertRegistrationRequest.Submit(aliceId, "سيرة 2", "Bio 2", new[] { "solar" }, clock);
        approvedRequest.Approve(adminId, clock);

        var users = new[] { BuildUser(aliceId, "alice@cce.local", "alice") };
        var db = BuildDb(new[] { pendingRequest, approvedRequest }, users);
        var sut = new ListExpertRequestsQueryHandler(db);

        var result = await sut.Handle(
            new ListExpertRequestsQuery(Status: ExpertRegistrationStatus.Pending),
            CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().Status.Should().Be(ExpertRegistrationStatus.Pending);
    }

    [Fact]
    public async Task RequestedById_filter_restricts_results()
    {
        var clock = new FakeSystemClock();
        var aliceId = System.Guid.NewGuid();
        var bobId = System.Guid.NewGuid();

        var aliceRequest = ExpertRegistrationRequest.Submit(aliceId, "سيرة أليس", "Alice Bio", new[] { "energy" }, clock);
        var bobRequest = ExpertRegistrationRequest.Submit(bobId, "سيرة بوب", "Bob Bio", new[] { "wind" }, clock);

        var users = new[]
        {
            BuildUser(aliceId, "alice@cce.local", "alice"),
            BuildUser(bobId, "bob@cce.local", "bob"),
        };

        var db = BuildDb(new[] { aliceRequest, bobRequest }, users);
        var sut = new ListExpertRequestsQueryHandler(db);

        var result = await sut.Handle(
            new ListExpertRequestsQuery(RequestedById: aliceId),
            CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().RequestedById.Should().Be(aliceId);
    }

    private static ICceDbContext BuildDb(
        IEnumerable<ExpertRegistrationRequest> requests,
        IEnumerable<User> users)
    {
        var db = Substitute.For<ICceDbContext>();
        db.ExpertRegistrationRequests.Returns(requests.AsQueryable());
        db.Users.Returns(users.AsQueryable());
        return db;
    }

    private static User BuildUser(System.Guid id, string email, string userName) =>
        new()
        {
            Id = id,
            Email = email,
            UserName = userName,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = userName.ToUpperInvariant(),
        };
}

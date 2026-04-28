using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Queries.ListStateRepAssignments;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;
using Microsoft.AspNetCore.Identity;

namespace CCE.Application.Tests.Identity.Queries;

public class ListStateRepAssignmentsQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_assignments()
    {
        var db = BuildDb(System.Array.Empty<StateRepresentativeAssignment>(), System.Array.Empty<User>());
        var sut = new ListStateRepAssignmentsQueryHandler(db);

        var result = await sut.Handle(new ListStateRepAssignmentsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task Returns_active_assignments_with_user_names()
    {
        var clock = new FakeSystemClock();
        var aliceId = System.Guid.NewGuid();
        var bobId = System.Guid.NewGuid();
        var assignerId = System.Guid.NewGuid();
        var country1 = System.Guid.NewGuid();
        var country2 = System.Guid.NewGuid();

        var aliceA = StateRepresentativeAssignment.Assign(aliceId, country1, assignerId, clock);
        var bobA = StateRepresentativeAssignment.Assign(bobId, country2, assignerId, clock);
        var users = new[]
        {
            BuildUser(aliceId, "alice@cce.local", "alice"),
            BuildUser(bobId, "bob@cce.local", "bob"),
        };

        var db = BuildDb(new[] { aliceA, bobA }, users);
        var sut = new ListStateRepAssignmentsQueryHandler(db);

        var result = await sut.Handle(new ListStateRepAssignmentsQuery(), CancellationToken.None);

        result.Total.Should().Be(2);
        var aliceItem = result.Items.Single(i => i.UserId == aliceId);
        aliceItem.UserName.Should().Be("alice");
        aliceItem.IsActive.Should().BeTrue();
        aliceItem.RevokedOn.Should().BeNull();
    }

    [Fact]
    public async Task UserId_filter_restricts_to_that_user()
    {
        var clock = new FakeSystemClock();
        var aliceId = System.Guid.NewGuid();
        var bobId = System.Guid.NewGuid();
        var assignerId = System.Guid.NewGuid();

        var aliceA = StateRepresentativeAssignment.Assign(aliceId, System.Guid.NewGuid(), assignerId, clock);
        var bobA = StateRepresentativeAssignment.Assign(bobId, System.Guid.NewGuid(), assignerId, clock);

        var users = new[]
        {
            BuildUser(aliceId, "alice@cce.local", "alice"),
            BuildUser(bobId, "bob@cce.local", "bob"),
        };
        var db = BuildDb(new[] { aliceA, bobA }, users);
        var sut = new ListStateRepAssignmentsQueryHandler(db);

        var result = await sut.Handle(new ListStateRepAssignmentsQuery(UserId: aliceId), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().UserId.Should().Be(aliceId);
    }

    [Fact]
    public async Task CountryId_filter_restricts_to_that_country()
    {
        var clock = new FakeSystemClock();
        var aliceId = System.Guid.NewGuid();
        var country1 = System.Guid.NewGuid();
        var country2 = System.Guid.NewGuid();

        var assignment1 = StateRepresentativeAssignment.Assign(aliceId, country1, aliceId, clock);
        var assignment2 = StateRepresentativeAssignment.Assign(aliceId, country2, aliceId, clock);
        var users = new[] { BuildUser(aliceId, "alice@cce.local", "alice") };

        var db = BuildDb(new[] { assignment1, assignment2 }, users);
        var sut = new ListStateRepAssignmentsQueryHandler(db);

        var result = await sut.Handle(new ListStateRepAssignmentsQuery(CountryId: country1), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().CountryId.Should().Be(country1);
    }

    [Fact]
    public async Task Active_false_with_in_memory_db_returns_all_assignments()
    {
        // For in-memory test queryables there's no soft-delete filter to bypass —
        // the test verifies the handler uses the right code path without throwing.
        // EF integration coverage of the IgnoreQueryFilters branch is left to a
        // future SQL Server-backed handler test.
        var clock = new FakeSystemClock();
        var aliceId = System.Guid.NewGuid();
        var assignment = StateRepresentativeAssignment.Assign(aliceId, System.Guid.NewGuid(), aliceId, clock);
        assignment.Revoke(aliceId, clock); // marks IsDeleted=true

        var users = new[] { BuildUser(aliceId, "alice@cce.local", "alice") };
        var db = BuildDb(new[] { assignment }, users);
        var sut = new ListStateRepAssignmentsQueryHandler(db);

        var result = await sut.Handle(new ListStateRepAssignmentsQuery(Active: false), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.Single().IsActive.Should().BeFalse();
        result.Items.Single().RevokedOn.Should().NotBeNull();
    }

    private static ICceDbContext BuildDb(
        IEnumerable<StateRepresentativeAssignment> assignments,
        IEnumerable<User> users)
    {
        var db = Substitute.For<ICceDbContext>();
        db.StateRepresentativeAssignments.Returns(assignments.AsQueryable());
        db.Users.Returns(users.AsQueryable());
        db.Roles.Returns(System.Array.Empty<Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<IdentityUserRole<System.Guid>>().AsQueryable());
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

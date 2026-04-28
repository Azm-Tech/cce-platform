using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Queries.ListUsers;
using CCE.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace CCE.Application.Tests.Identity.Queries;

public class ListUsersQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_users_exist()
    {
        var db = BuildDb(users: System.Array.Empty<User>(), roles: System.Array.Empty<Role>(), userRoles: System.Array.Empty<IdentityUserRole<System.Guid>>());
        var sut = new ListUsersQueryHandler(db);

        var result = await sut.Handle(new ListUsersQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_users_with_their_role_names()
    {
        var aliceId = System.Guid.NewGuid();
        var bobId = System.Guid.NewGuid();
        var superAdminRoleId = System.Guid.NewGuid();
        var contentManagerRoleId = System.Guid.NewGuid();

        var users = new[]
        {
            BuildUser(aliceId, "alice@cce.local", "alice"),
            BuildUser(bobId, "bob@cce.local", "bob"),
        };
        var roles = new[]
        {
            BuildRole(superAdminRoleId, "SuperAdmin"),
            BuildRole(contentManagerRoleId, "ContentManager"),
        };
        var userRoles = new[]
        {
            new IdentityUserRole<System.Guid> { UserId = aliceId, RoleId = superAdminRoleId },
            new IdentityUserRole<System.Guid> { UserId = aliceId, RoleId = contentManagerRoleId },
            new IdentityUserRole<System.Guid> { UserId = bobId, RoleId = contentManagerRoleId },
        };

        var db = BuildDb(users, roles, userRoles);
        var sut = new ListUsersQueryHandler(db);

        var result = await sut.Handle(new ListUsersQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);

        var alice = result.Items.Single(u => u.UserName == "alice");
        alice.Roles.Should().BeEquivalentTo(new[] { "SuperAdmin", "ContentManager" });
        alice.IsActive.Should().BeTrue();

        var bob = result.Items.Single(u => u.UserName == "bob");
        bob.Roles.Should().BeEquivalentTo(new[] { "ContentManager" });
    }

    [Fact]
    public async Task Search_filters_by_username_or_email_substring()
    {
        var users = new[]
        {
            BuildUser(System.Guid.NewGuid(), "alice@cce.local", "alice"),
            BuildUser(System.Guid.NewGuid(), "bob@example.com", "bob"),
        };
        var db = BuildDb(users, System.Array.Empty<Role>(), System.Array.Empty<IdentityUserRole<System.Guid>>());
        var sut = new ListUsersQueryHandler(db);

        var result = await sut.Handle(new ListUsersQuery(Search: "cce.local"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().UserName.Should().Be("alice");
    }

    [Fact]
    public async Task Role_filter_restricts_to_users_in_that_role()
    {
        var aliceId = System.Guid.NewGuid();
        var bobId = System.Guid.NewGuid();
        var superAdminRoleId = System.Guid.NewGuid();
        var contentManagerRoleId = System.Guid.NewGuid();

        var users = new[]
        {
            BuildUser(aliceId, "alice@cce.local", "alice"),
            BuildUser(bobId, "bob@cce.local", "bob"),
        };
        var roles = new[]
        {
            BuildRole(superAdminRoleId, "SuperAdmin"),
            BuildRole(contentManagerRoleId, "ContentManager"),
        };
        var userRoles = new[]
        {
            new IdentityUserRole<System.Guid> { UserId = aliceId, RoleId = superAdminRoleId },
            new IdentityUserRole<System.Guid> { UserId = bobId, RoleId = contentManagerRoleId },
        };

        var db = BuildDb(users, roles, userRoles);
        var sut = new ListUsersQueryHandler(db);

        var result = await sut.Handle(new ListUsersQuery(Role: "SuperAdmin"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().UserName.Should().Be("alice");
    }

    private static ICceDbContext BuildDb(
        IEnumerable<User> users,
        IEnumerable<Role> roles,
        IEnumerable<IdentityUserRole<System.Guid>> userRoles)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Users.Returns(users.AsQueryable());
        db.Roles.Returns(roles.AsQueryable());
        db.UserRoles.Returns(userRoles.AsQueryable());
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

    private static Role BuildRole(System.Guid id, string name) =>
        new() { Id = id, Name = name, NormalizedName = name.ToUpperInvariant() };
}

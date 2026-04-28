using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Queries.GetUserById;
using CCE.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace CCE.Application.Tests.Identity.Queries;

public class GetUserByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_null_when_user_not_found()
    {
        var db = BuildDb(System.Array.Empty<User>(), System.Array.Empty<Role>(), System.Array.Empty<IdentityUserRole<System.Guid>>());
        var sut = new GetUserByIdQueryHandler(db);

        var result = await sut.Handle(new GetUserByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_user_detail_with_role_names_and_is_active_true()
    {
        var aliceId = System.Guid.NewGuid();
        var superAdminRoleId = System.Guid.NewGuid();
        var users = new[] { BuildUser(aliceId, "alice@cce.local", "alice") };
        var roles = new[] { BuildRole(superAdminRoleId, "SuperAdmin") };
        var userRoles = new[] { new IdentityUserRole<System.Guid> { UserId = aliceId, RoleId = superAdminRoleId } };

        var db = BuildDb(users, roles, userRoles);
        var sut = new GetUserByIdQueryHandler(db);

        var result = await sut.Handle(new GetUserByIdQuery(aliceId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(aliceId);
        result.UserName.Should().Be("alice");
        result.Email.Should().Be("alice@cce.local");
        result.Roles.Should().BeEquivalentTo(new[] { "SuperAdmin" });
        result.IsActive.Should().BeTrue();
        result.LocalePreference.Should().Be("ar");
    }

    [Fact]
    public async Task Returns_is_active_false_when_lockout_active()
    {
        var aliceId = System.Guid.NewGuid();
        var future = System.DateTimeOffset.UtcNow.AddYears(1);
        var alice = BuildUser(aliceId, "alice@cce.local", "alice");
        alice.LockoutEnabled = true;
        alice.LockoutEnd = future;

        var db = BuildDb(new[] { alice }, System.Array.Empty<Role>(), System.Array.Empty<IdentityUserRole<System.Guid>>());
        var sut = new GetUserByIdQueryHandler(db);

        var result = await sut.Handle(new GetUserByIdQuery(aliceId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
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

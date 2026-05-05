using System.Security.Claims;
using CCE.Api.Common.Authorization;
using CCE.Domain;

namespace CCE.Api.IntegrationTests.Authorization;

public class RoleToPermissionClaimsTransformerTests
{
    [Fact]
    public async Task Anonymous_principal_is_returned_unchanged()
    {
        var anon = new ClaimsPrincipal(new ClaimsIdentity());
        var sut = new RoleToPermissionClaimsTransformer();

        var result = await sut.TransformAsync(anon);

        result.Should().BeSameAs(anon);
    }

    [Fact]
    public async Task Unknown_role_does_not_add_any_permissions()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("roles", "NotARealRole") },
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleToPermissionClaimsTransformer();

        var result = await sut.TransformAsync(principal);

        var groups = result.FindAll("groups").Select(c => c.Value).ToList();
        groups.Should().BeEmpty();
    }

    [Fact]
    public async Task Idempotent_when_already_transformed()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("roles", "cce-admin") },
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleToPermissionClaimsTransformer();

        var first = await sut.TransformAsync(principal);
        var firstCount = first.FindAll("groups").Count();

        var second = await sut.TransformAsync(first);
        var secondCount = second.FindAll("groups").Count();

        secondCount.Should().Be(firstCount, "second transform must short-circuit");
    }

    [Fact]
    public async Task EntraId_roles_claim_cce_admin_expands_to_full_permission_set()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("roles", "cce-admin") },
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleToPermissionClaimsTransformer();

        var result = await sut.TransformAsync(principal);

        var permissions = result.FindAll("groups").Select(c => c.Value).ToHashSet();
        permissions.Should().Contain(Permissions.System_Health_Read);
        permissions.Should().Contain(Permissions.User_Create);
        permissions.Should().Contain(Permissions.Role_Assign);
    }

    [Fact]
    public async Task EntraId_roles_claim_cce_user_grants_community_writes_but_not_admin_actions()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("roles", "cce-user") },
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleToPermissionClaimsTransformer();

        var result = await sut.TransformAsync(principal);

        var permissions = result.FindAll("groups").Select(c => c.Value).ToHashSet();
        permissions.Should().Contain(Permissions.Community_Post_Create);
        permissions.Should().Contain(Permissions.Community_Post_Reply);
        permissions.Should().NotContain(Permissions.Role_Assign);
        permissions.Should().NotContain(Permissions.User_Create);
    }
}

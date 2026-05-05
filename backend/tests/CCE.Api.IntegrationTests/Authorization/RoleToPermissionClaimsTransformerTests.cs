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
    public async Task Legacy_SuperAdmin_groups_claim_expands_to_admin_permission_set()
    {
        // Legacy Keycloak `groups` claim — still consumed during Sub-11 phases
        // 00-03 coexistence; Phase 04 cutover removes the legacy branch.
        var identity = new ClaimsIdentity(
            new[] { new Claim("groups", "SuperAdmin") },
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleToPermissionClaimsTransformer();

        var result = await sut.TransformAsync(principal);

        var permissionClaims = result.FindAll("groups").Select(c => c.Value).ToHashSet();
        permissionClaims.Should().Contain(Permissions.User_Read);
        permissionClaims.Should().Contain(Permissions.Role_Assign);
        // Role-name preserved alongside the expanded permissions.
        permissionClaims.Should().Contain("SuperAdmin");
    }

    [Fact]
    public async Task Slash_prefixed_keycloak_group_paths_are_normalized()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("groups", "/SuperAdmin") },
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleToPermissionClaimsTransformer();

        var result = await sut.TransformAsync(principal);

        result.FindAll("groups").Select(c => c.Value).Should().Contain(Permissions.User_Read);
    }

    [Fact]
    public async Task Unknown_role_group_does_not_add_any_permissions()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("groups", "NotARealRole") },
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleToPermissionClaimsTransformer();

        var result = await sut.TransformAsync(principal);

        var groups = result.FindAll("groups").Select(c => c.Value).ToList();
        groups.Should().ContainSingle(g => g == "NotARealRole");
        groups.Should().NotContain(g => g.Contains('.'));
    }

    [Fact]
    public async Task Idempotent_when_already_transformed()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("groups", "SuperAdmin") },
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleToPermissionClaimsTransformer();

        var first = await sut.TransformAsync(principal);
        var firstCount = first.FindAll("groups").Count();

        var second = await sut.TransformAsync(first);
        var secondCount = second.FindAll("groups").Count();

        secondCount.Should().Be(firstCount, "second transform must short-circuit");
    }

    // ─── Sub-11 Phase 03 — Entra ID `roles` claim (deferred from Phase 00) ───

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
        permissions.Should().NotContain(Permissions.Role_Assign); // admin-only
        permissions.Should().NotContain(Permissions.User_Create); // admin-only
    }
}

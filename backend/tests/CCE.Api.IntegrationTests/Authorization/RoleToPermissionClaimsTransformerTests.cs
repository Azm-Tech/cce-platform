using System.Security.Claims;
using CCE.Api.Common.Authorization;
using CCE.Application.Identity.Auth.Common;
using CCE.Domain;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace CCE.Api.IntegrationTests.Authorization;

public class RoleToPermissionClaimsTransformerTests
{
    private static RoleToPermissionClaimsTransformer CreateSut(IPermissionService? permissions = null)
    {
        permissions ??= Substitute.For<IPermissionService>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IPermissionService)).Returns(permissions);

        var serviceScope = Substitute.For<IServiceScope>();
        serviceScope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(serviceScope);

        return new RoleToPermissionClaimsTransformer(scopeFactory);
    }

    [Fact]
    public async Task Anonymous_principal_is_returned_unchanged()
    {
        var anon = new ClaimsPrincipal(new ClaimsIdentity());
        var sut = CreateSut();

        var result = await sut.TransformAsync(anon);

        result.Should().BeSameAs(anon);
    }

    [Fact]
    public async Task Unknown_role_does_not_add_any_permissions()
    {
        var permissions = Substitute.For<IPermissionService>();
        permissions.GetRolePermissionsAsync("NotARealRole", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([]));

        var identity = new ClaimsIdentity(
            [new Claim("roles", "NotARealRole")],
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = CreateSut(permissions);

        var result = await sut.TransformAsync(principal);

        var groups = result.FindAll("groups").Select(c => c.Value).ToList();
        groups.Should().BeEmpty();
    }

    [Fact]
    public async Task Idempotent_when_already_transformed()
    {
        var permissions = Substitute.For<IPermissionService>();
        permissions.GetRolePermissionsAsync("cce-admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([Permissions.User_Create, Permissions.Role_Assign]));

        var identity = new ClaimsIdentity(
            [new Claim("roles", "cce-admin")],
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = CreateSut(permissions);

        var first = await sut.TransformAsync(principal);
        var firstCount = first.FindAll("groups").Count();

        var second = await sut.TransformAsync(first);
        var secondCount = second.FindAll("groups").Count();

        secondCount.Should().Be(firstCount, "second transform must short-circuit");
    }

    [Fact]
    public async Task EntraId_roles_claim_cce_admin_expands_to_full_permission_set()
    {
        var permissions = Substitute.For<IPermissionService>();
        permissions.GetRolePermissionsAsync("cce-admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([
                Permissions.System_Health_Read,
                Permissions.User_Create,
                Permissions.Role_Assign,
            ]));

        var identity = new ClaimsIdentity(
            [new Claim("roles", "cce-admin")],
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = CreateSut(permissions);

        var result = await sut.TransformAsync(principal);

        var permissionSet = result.FindAll("groups").Select(c => c.Value).ToHashSet();
        permissionSet.Should().Contain(Permissions.System_Health_Read);
        permissionSet.Should().Contain(Permissions.User_Create);
        permissionSet.Should().Contain(Permissions.Role_Assign);
    }

    [Fact]
    public async Task EntraId_roles_claim_cce_user_grants_community_writes_but_not_admin_actions()
    {
        var permissions = Substitute.For<IPermissionService>();
        permissions.GetRolePermissionsAsync("cce-user", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([
                Permissions.Community_Post_Create,
                Permissions.Community_Post_Reply,
            ]));

        var identity = new ClaimsIdentity(
            [new Claim("roles", "cce-user")],
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = CreateSut(permissions);

        var result = await sut.TransformAsync(principal);

        var permissionSet = result.FindAll("groups").Select(c => c.Value).ToHashSet();
        permissionSet.Should().Contain(Permissions.Community_Post_Create);
        permissionSet.Should().Contain(Permissions.Community_Post_Reply);
        permissionSet.Should().NotContain(Permissions.Role_Assign);
        permissionSet.Should().NotContain(Permissions.User_Create);
    }
}

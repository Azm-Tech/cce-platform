using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using CCE.Seeder;
using CCE.Seeder.Seeders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.Tests.Seeder;

public class RolesAndPermissionsSeederTests
{
    private static CceDbContext NewContext() =>
        new(new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task First_run_creates_5_roles_with_permissions()
    {
        using var ctx = NewContext();
        var seeder = new RolesAndPermissionsSeeder(ctx, NullLogger<RolesAndPermissionsSeeder>.Instance);

        await seeder.SeedAsync();

        var roles = await ctx.Set<Role>().ToListAsync();
        roles.Should().HaveCount(5);
        roles.Select(r => r.Name).Should().Contain(new[] { "cce-admin", "cce-editor",
            "cce-reviewer", "cce-expert", "cce-user" });

        var claims = await ctx.Set<IdentityRoleClaim<System.Guid>>().ToListAsync();
        claims.Should().NotBeEmpty();
        claims.Where(c => c.ClaimType == "permission").Should().NotBeEmpty();
    }

    [Fact]
    public async Task Second_run_is_idempotent()
    {
        using var ctx = NewContext();
        var seeder = new RolesAndPermissionsSeeder(ctx, NullLogger<RolesAndPermissionsSeeder>.Instance);

        await seeder.SeedAsync();
        var afterFirstRoles = await ctx.Set<Role>().CountAsync();
        var afterFirstClaims = await ctx.Set<IdentityRoleClaim<System.Guid>>().CountAsync();

        await seeder.SeedAsync();
        var afterSecondRoles = await ctx.Set<Role>().CountAsync();
        var afterSecondClaims = await ctx.Set<IdentityRoleClaim<System.Guid>>().CountAsync();

        afterSecondRoles.Should().Be(afterFirstRoles);
        afterSecondClaims.Should().Be(afterFirstClaims);
    }

    [Fact]
    public async Task CceAdmin_has_System_Health_Read_claim()
    {
        using var ctx = NewContext();
        var seeder = new RolesAndPermissionsSeeder(ctx, NullLogger<RolesAndPermissionsSeeder>.Instance);
        await seeder.SeedAsync();

        var roleId = DeterministicGuid.From("role:cce-admin");
        var hasClaim = await ctx.Set<IdentityRoleClaim<System.Guid>>()
            .AnyAsync(c => c.RoleId == roleId
                           && c.ClaimType == "permission"
                           && c.ClaimValue == "System.Health.Read");
        hasClaim.Should().BeTrue();
    }
}

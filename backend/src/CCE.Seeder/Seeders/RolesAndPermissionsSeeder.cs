using CCE.Domain;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

public sealed class RolesAndPermissionsSeeder : ISeeder
{
    private static readonly string[] SeededRoleNames =
    {
        "SuperAdmin", "ContentManager", "StateRepresentative",
        "CommunityExpert", "RegisteredUser",
    };

    private readonly CceDbContext _ctx;
    private readonly ILogger<RolesAndPermissionsSeeder> _logger;

    public RolesAndPermissionsSeeder(CceDbContext ctx, ILogger<RolesAndPermissionsSeeder> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public int Order => 10;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in SeededRoleNames)
        {
            var roleId = DeterministicGuid.From($"role:{roleName}");
            var existing = await _ctx.Set<Role>().FindAsync(new object[] { roleId }, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                _ctx.Set<Role>().Add(new Role(roleName)
                {
                    Id = roleId,
                    NormalizedName = roleName.ToUpperInvariant(),
                });
                _logger.LogInformation("Seeded role {Role}", roleName);
            }

            var permissions = GetPermissionsForRole(roleName);
            foreach (var permission in permissions)
            {
                var claimExists = await _ctx.Set<IdentityRoleClaim<System.Guid>>()
                    .AnyAsync(c => c.RoleId == roleId
                                   && c.ClaimType == "permission"
                                   && c.ClaimValue == permission, cancellationToken)
                    .ConfigureAwait(false);
                if (!claimExists)
                {
                    _ctx.Set<IdentityRoleClaim<System.Guid>>().Add(new IdentityRoleClaim<System.Guid>
                    {
                        RoleId = roleId,
                        ClaimType = "permission",
                        ClaimValue = permission,
                    });
                }
            }
        }
        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        "SuperAdmin" => RolePermissionMap.SuperAdmin,
        "ContentManager" => RolePermissionMap.ContentManager,
        "StateRepresentative" => RolePermissionMap.StateRepresentative,
        "CommunityExpert" => RolePermissionMap.CommunityExpert,
        "RegisteredUser" => RolePermissionMap.RegisteredUser,
        _ => System.Array.Empty<string>(),
    };
}

using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

/// <summary>
/// Seeds user-level permission claims based on each user's current role.
/// For every user, copies the role's permission claims into AspNetUserClaims.
/// Idempotent — skips claims that already exist.
///
/// Order = 16 runs after DemoUsersSeeder (15) so demo users also get seeded.
/// </summary>
public sealed class UserClaimsSeeder : ISeeder
{
    private readonly CceDbContext _ctx;
    private readonly ILogger<UserClaimsSeeder> _logger;

    public UserClaimsSeeder(CceDbContext ctx, ILogger<UserClaimsSeeder> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public int Order => 16;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var userRoles = await _ctx.Set<IdentityUserRole<System.Guid>>()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var roleClaims = await _ctx.Set<IdentityRoleClaim<System.Guid>>()
            .Where(rc => rc.ClaimType == "permission")
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var roleClaimsByRole = roleClaims
            .GroupBy(rc => rc.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(rc => rc.ClaimValue!).ToHashSet());

        var existingUserClaims = await _ctx.Set<IdentityUserClaim<System.Guid>>()
            .Where(uc => uc.ClaimType == "permission")
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingUserClaimSet = existingUserClaims
            .Select(uc => (uc.UserId, uc.ClaimValue))
            .ToHashSet();

        var usersGrouped = userRoles
            .GroupBy(ur => ur.UserId)
            .ToDictionary(g => g.Key, g => g.Select(ur => ur.RoleId).ToHashSet());

        var totalAdded = 0;

        foreach (var (userId, roleIds) in usersGrouped)
        {
            var permissionsForUser = roleIds
                .Where(roleClaimsByRole.ContainsKey)
                .SelectMany(rid => roleClaimsByRole[rid])
                .ToHashSet();

            var toAdd = permissionsForUser
                .Where(p => !existingUserClaimSet.Contains((userId, p)))
                .ToList();

            foreach (var permission in toAdd)
            {
                _ctx.Set<IdentityUserClaim<System.Guid>>().Add(new IdentityUserClaim<System.Guid>
                {
                    UserId = userId,
                    ClaimType = "permission",
                    ClaimValue = permission,
                });
            }

            if (toAdd.Count > 0)
            {
                totalAdded += toAdd.Count;
                _logger.LogInformation(
                    "Seeded {Count} user claims for user {UserId}.",
                    toAdd.Count, userId);
            }
        }

        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("UserClaimsSeeder complete — added {Total} claims across {Users} users.",
            totalAdded, usersGrouped.Count);
    }
}

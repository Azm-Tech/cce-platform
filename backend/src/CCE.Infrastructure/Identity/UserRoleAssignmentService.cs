using CCE.Application.Identity;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Identity;

public sealed class UserRoleAssignmentService : IUserRoleAssignmentService
{
    private readonly CceDbContext _db;
    private readonly ILogger<UserRoleAssignmentService> _logger;

    public UserRoleAssignmentService(CceDbContext db, ILogger<UserRoleAssignmentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> ReplaceRolesAsync(
        Guid userId,
        IReadOnlyCollection<string> targetRoleNames,
        CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct).ConfigureAwait(false);
        if (user is null)
        {
            return false;
        }

        var normalizedTargets = targetRoleNames
            .Select(static n => n.ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);

        var roles = await _db.Roles
            .Where(r => r.NormalizedName != null && normalizedTargets.Contains(r.NormalizedName))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var roleIdByNormalizedName = roles.ToDictionary(r => r.NormalizedName!, r => r.Id, StringComparer.Ordinal);

        var existing = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var existingRoleIds = existing.Select(ur => ur.RoleId).ToHashSet();

        var targetRoleIds = roleIdByNormalizedName.Values.ToHashSet();

        var toAdd = targetRoleIds.Except(existingRoleIds).ToList();
        var toRemove = existing.Where(ur => !targetRoleIds.Contains(ur.RoleId)).ToList();

        foreach (var roleId in toAdd)
        {
            _db.Set<IdentityUserRole<Guid>>().Add(new IdentityUserRole<Guid>
            {
                UserId = userId,
                RoleId = roleId,
            });
        }
        foreach (var ur in toRemove)
        {
            _db.Set<IdentityUserRole<Guid>>().Remove(ur);
        }

        if (toAdd.Count > 0 || toRemove.Count > 0)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation(
                "Replaced roles for user {UserId}: +{Added} −{Removed}",
                userId, toAdd.Count, toRemove.Count);
        }

        return true;
    }
}

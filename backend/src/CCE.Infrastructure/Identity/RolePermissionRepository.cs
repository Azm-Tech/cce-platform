using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Identity.Permissions;
using CCE.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Identity;

public sealed class RolePermissionRepository : IRolePermissionRepository
{
    private readonly ICceDbContext _db;
    private readonly IPermissionService _permissions;

    public RolePermissionRepository(ICceDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task<RolePermissionsResult?> UpsertAsync(
        string roleName,
        IReadOnlySet<string> desiredPermissions,
        Guid actorId,
        string actorEmail,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var normalizedName = roleName.ToUpperInvariant();
        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, ct)
            .ConfigureAwait(false);

        if (role is null) return null;

        var existing = await _db.RoleClaims
            .Where(rc => rc.RoleId == role.Id && rc.ClaimType == "permission")
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingNames = existing.Select(rc => rc.ClaimValue!).ToHashSet(StringComparer.Ordinal);
        var toAdd    = desiredPermissions.Except(existingNames).ToList();
        var toRemove = existing.Where(rc => !desiredPermissions.Contains(rc.ClaimValue!)).ToList();

        foreach (var p in toAdd)
        {
            _db.Add(new IdentityRoleClaim<Guid>
            {
                RoleId = role.Id,
                ClaimType = "permission",
                ClaimValue = p,
            });
            _db.Add(PermissionAuditLog.Record(now, actorId, actorEmail, roleName, p, PermissionAuditAction.Granted));
        }

        foreach (var rc in toRemove)
        {
            _db.Delete(rc);
            _db.Add(PermissionAuditLog.Record(now, actorId, actorEmail, roleName, rc.ClaimValue!, PermissionAuditAction.Revoked));
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _permissions.InvalidateCacheForRole(roleName);

        return new RolePermissionsResult(
            roleName,
            desiredPermissions.OrderBy(p => p).ToArray(),
            toAdd.Count,
            toRemove.Count,
            desiredPermissions.Count);
    }
}

using CCE.Application.Identity.Auth.Common;
using CCE.Domain;
using CCE.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace CCE.Infrastructure.Identity;

public sealed class PermissionService : IPermissionService
{
    // Anonymous is not stored in AspNetRoles — its permissions come from
    // the source-generated RolePermissionMap (seeded from permissions.yaml).
    // After the lowercase rename these are already lowercase values.
    private static readonly IReadOnlyList<string> AnonymousPermissions = RolePermissionMap.Anonymous;

    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public PermissionService(
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        IMemoryCache cache)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _cache = cache;
    }

    public async Task<IReadOnlyList<string>> GetRolePermissionsAsync(
        string roleName, CancellationToken ct = default)
    {
        if (string.Equals(roleName, "Anonymous", StringComparison.OrdinalIgnoreCase))
            return AnonymousPermissions;

        var key = $"role-perm:{roleName}";
        if (_cache.TryGetValue(key, out IReadOnlyList<string>? hit) && hit is not null)
            return hit;

        var role = await _roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
        if (role is null) return Array.Empty<string>();

        var claims = await _roleManager.GetClaimsAsync(role).ConfigureAwait(false);
        var result = claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToArray();

        _cache.Set(key, (IReadOnlyList<string>)result, CacheTtl);
        return result;
    }

    public async Task<IReadOnlyList<string>> GetUserEffectivePermissionsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var key = $"user-perm:{userId}";
        if (_cache.TryGetValue(key, out IReadOnlyList<string>? hit) && hit is not null)
            return hit;

        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return Array.Empty<string>();

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var all = new HashSet<string>(StringComparer.Ordinal);

        foreach (var r in roles)
            foreach (var p in await GetRolePermissionsAsync(r, ct).ConfigureAwait(false))
                all.Add(p);

        // Merge user-level claims (additive overrides on top of role permissions)
        var userClaims = await _userManager.GetClaimsAsync(user).ConfigureAwait(false);
        foreach (var c in userClaims)
            if (c.Type == "permission" && !string.IsNullOrEmpty(c.Value))
                all.Add(c.Value);

        var result = all.ToArray();
        _cache.Set(key, (IReadOnlyList<string>)result, CacheTtl);
        return result;
    }

    public void InvalidateCacheForRole(string roleName)
        => _cache.Remove($"role-perm:{roleName}");

    public void InvalidateCacheForUser(Guid userId)
        => _cache.Remove($"user-perm:{userId}");
}

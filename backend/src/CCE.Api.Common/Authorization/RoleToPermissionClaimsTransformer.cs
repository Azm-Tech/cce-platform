using System.Security.Claims;
using CCE.Domain;
using Microsoft.AspNetCore.Authentication;

namespace CCE.Api.Common.Authorization;

/// <summary>
/// Expands the role-name <c>groups</c> claims (e.g., <c>SuperAdmin</c>) on an authenticated
/// principal into permission-name <c>groups</c> claims (e.g., <c>User.Read</c>) so the
/// per-permission authorization policies registered by <c>AddCcePermissionPolicies</c> pass.
/// Idempotent — recognises an already-transformed principal via a sentinel claim and
/// short-circuits to avoid re-flattening on every authorization callback.
/// </summary>
public sealed class RoleToPermissionClaimsTransformer : IClaimsTransformation
{
    private const string SentinelType = "cce:permissions-flattened";
    private const string GroupsClaimType = "groups";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        if (identity.HasClaim(SentinelType, "1"))
        {
            return Task.FromResult(principal);
        }

        // Snapshot the roles already present so we don't mutate while iterating.
        var roleGroups = principal.FindAll(GroupsClaimType).Select(c => c.Value).ToList();
        var existingPermissions = new HashSet<string>(roleGroups, System.StringComparer.Ordinal);

        var permissionsToAdd = new List<string>();
        foreach (var group in roleGroups)
        {
            var roleName = NormalizeRoleName(group);
            var permissions = ResolveRolePermissions(roleName);
            foreach (var permission in permissions)
            {
                if (existingPermissions.Add(permission))
                {
                    permissionsToAdd.Add(permission);
                }
            }
        }

        // Clone the identity so we don't mutate a shared principal across requests.
        var clone = identity.Clone();
        foreach (var permission in permissionsToAdd)
        {
            clone.AddClaim(new Claim(GroupsClaimType, permission));
        }
        clone.AddClaim(new Claim(SentinelType, "1"));

        var result = new ClaimsPrincipal(principal.Identities.Select(i => i == identity ? clone : i.Clone()));
        return Task.FromResult(result);
    }

    /// <summary>
    /// Maps a Keycloak group claim value (often prefixed with <c>"/"</c>, e.g. <c>"/cce-admins"</c>)
    /// to a CCE role name. Handles the bare role names too (<c>"SuperAdmin"</c>).
    /// </summary>
    private static string NormalizeRoleName(string group)
    {
        if (string.IsNullOrEmpty(group))
        {
            return string.Empty;
        }
        var trimmed = group.TrimStart('/');
        // Keycloak realm-roles claim emits bare names already; group-paths come with a '/' prefix.
        // Future configurability lives in IConfiguration["UserSync:GroupToRoleMap"] but for the
        // claims-flatten step we accept the bare role names directly.
        return trimmed;
    }

    private static IReadOnlyList<string> ResolveRolePermissions(string roleName) => roleName switch
    {
        "SuperAdmin" => RolePermissionMap.SuperAdmin,
        "ContentManager" => RolePermissionMap.ContentManager,
        "StateRepresentative" => RolePermissionMap.StateRepresentative,
        "CommunityExpert" => RolePermissionMap.CommunityExpert,
        "RegisteredUser" => RolePermissionMap.RegisteredUser,
        "Anonymous" => RolePermissionMap.Anonymous,
        _ => System.Array.Empty<string>(),
    };
}

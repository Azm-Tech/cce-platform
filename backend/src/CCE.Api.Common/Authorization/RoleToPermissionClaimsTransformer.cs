using System.Security.Claims;
using CCE.Domain;
using Microsoft.AspNetCore.Authentication;

namespace CCE.Api.Common.Authorization;

/// <summary>
/// Sub-11 — expands role-name claims into permission-name <c>groups</c>
/// claims (e.g., <c>User.Read</c>) so the per-permission authorization
/// policies registered by <c>AddCcePermissionPolicies</c> pass.
///
/// Reads from BOTH claim types during the Sub-11 coexistence window:
/// <list type="bullet">
///   <item><c>roles</c> — Entra ID app-role values (<c>cce-admin</c>, etc.); the
///   primary path going forward (Sub-11 Phase 03+).</item>
///   <item><c>groups</c> — Keycloak realm-role names (<c>SuperAdmin</c>, etc.);
///   still consumed through the custom-BFF path until Phase 04 cutover deletes
///   the legacy IdP. Phase 04 removes this branch.</item>
/// </list>
///
/// Both legacy and new role names map to the same permission set —
/// <c>SuperAdmin</c> and <c>cce-admin</c> both resolve to
/// <see cref="RolePermissionMap.CceAdmin"/>, etc.
///
/// Idempotent — recognises an already-transformed principal via a sentinel
/// claim and short-circuits to avoid re-flattening on every authorization
/// callback.
/// </summary>
public sealed class RoleToPermissionClaimsTransformer : IClaimsTransformation
{
    private const string SentinelType = "cce:permissions-flattened";
    private const string RolesClaimType = "roles";
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

        // Collect role-bearing claims from both the new (Entra ID) `roles` claim
        // and the legacy (Keycloak) `groups` claim. The latter doubles as the
        // output claim type for permissions, so we snapshot it before mutating.
        var existingGroups = principal.FindAll(GroupsClaimType).Select(c => c.Value).ToList();
        var rolesFromNewClaim = principal.FindAll(RolesClaimType).Select(c => c.Value);
        var roleValues = rolesFromNewClaim.Concat(existingGroups).ToList();

        var existingPermissions = new HashSet<string>(existingGroups, System.StringComparer.Ordinal);

        var permissionsToAdd = new List<string>();
        foreach (var raw in roleValues)
        {
            var roleName = NormalizeRoleName(raw);
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
    /// Strips a leading <c>"/"</c> if present (Keycloak group-path notation
    /// like <c>"/cce-admins"</c>). Entra ID `roles` claim values are bare
    /// strings — pass through unchanged.
    /// </summary>
    private static string NormalizeRoleName(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }
        return raw.TrimStart('/');
    }

    private static IReadOnlyList<string> ResolveRolePermissions(string roleName) => roleName switch
    {
        // Sub-11 Entra ID app-role values (current).
        "cce-admin"           => RolePermissionMap.CceAdmin,
        "cce-editor"          => RolePermissionMap.CceEditor,
        "cce-reviewer"        => RolePermissionMap.CceReviewer,
        "cce-expert"          => RolePermissionMap.CceExpert,
        "cce-user"            => RolePermissionMap.CceUser,
        "Anonymous"           => RolePermissionMap.Anonymous,

        // Legacy Keycloak realm-role names — still emitted through the
        // custom-BFF path until Phase 04 cutover deletes Keycloak. Map to
        // the same permission sets as their Entra ID equivalents.
        "SuperAdmin"          => RolePermissionMap.CceAdmin,
        "ContentManager"      => RolePermissionMap.CceEditor,
        "StateRepresentative" => RolePermissionMap.CceEditor,
        "CommunityExpert"     => RolePermissionMap.CceExpert,
        "RegisteredUser"      => RolePermissionMap.CceUser,

        _ => System.Array.Empty<string>(),
    };
}

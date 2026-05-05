using System.Security.Claims;
using CCE.Domain;
using Microsoft.AspNetCore.Authentication;

namespace CCE.Api.Common.Authorization;

/// <summary>
/// Sub-11 — expands the role-name <c>roles</c> claim values (Entra ID
/// app-role values, e.g. <c>cce-admin</c>) on an authenticated principal
/// into permission-name <c>groups</c> claims (e.g., <c>User.Read</c>) so
/// the per-permission authorization policies registered by
/// <c>AddCcePermissionPolicies</c> pass.
///
/// Idempotent — recognises an already-transformed principal via a
/// sentinel claim and short-circuits to avoid re-flattening on every
/// authorization callback.
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

        var roleValues = principal.FindAll(RolesClaimType).Select(c => c.Value).ToList();
        var existingPermissions = new HashSet<string>(
            principal.FindAll(GroupsClaimType).Select(c => c.Value),
            System.StringComparer.Ordinal);

        var permissionsToAdd = new List<string>();
        foreach (var role in roleValues)
        {
            var permissions = ResolveRolePermissions(role);
            foreach (var permission in permissions)
            {
                if (existingPermissions.Add(permission))
                {
                    permissionsToAdd.Add(permission);
                }
            }
        }

        var clone = identity.Clone();
        foreach (var permission in permissionsToAdd)
        {
            clone.AddClaim(new Claim(GroupsClaimType, permission));
        }
        clone.AddClaim(new Claim(SentinelType, "1"));

        var result = new ClaimsPrincipal(principal.Identities.Select(i => i == identity ? clone : i.Clone()));
        return Task.FromResult(result);
    }

    private static IReadOnlyList<string> ResolveRolePermissions(string role) => role switch
    {
        "cce-admin"    => RolePermissionMap.CceAdmin,
        "cce-editor"   => RolePermissionMap.CceEditor,
        "cce-reviewer" => RolePermissionMap.CceReviewer,
        "cce-expert"   => RolePermissionMap.CceExpert,
        "cce-user"     => RolePermissionMap.CceUser,
        "Anonymous"    => RolePermissionMap.Anonymous,
        _              => System.Array.Empty<string>(),
    };
}

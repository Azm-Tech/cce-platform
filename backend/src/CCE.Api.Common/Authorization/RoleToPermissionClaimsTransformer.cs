using System.Security.Claims;
using CCE.Application.Identity.Auth.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Api.Common.Authorization;

/// <summary>
/// Expands the <c>roles</c> claims on an authenticated principal into
/// permission-name <c>groups</c> claims by reading <c>AspNetRoleClaims</c>
/// via <see cref="IPermissionService"/>. Results are cached per role for 5 minutes.
/// Idempotent — short-circuits on the sentinel claim.
/// </summary>
public sealed class RoleToPermissionClaimsTransformer : IClaimsTransformation
{
    private const string SentinelType   = "cce:permissions-flattened";
    private const string RolesClaimType = "roles";
    private const string GroupsClaimType = "groups";

    private readonly IServiceScopeFactory _scopeFactory;

    public RoleToPermissionClaimsTransformer(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;

        if (identity.HasClaim(SentinelType, "1"))
            return principal;

        var roleValues = principal.FindAll(RolesClaimType).Select(c => c.Value).ToList();
        var existing   = new HashSet<string>(
            principal.FindAll(GroupsClaimType).Select(c => c.Value),
            System.StringComparer.Ordinal);

        var toAdd = new List<string>();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var svc = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        foreach (var role in roleValues)
        {
            foreach (var p in await svc.GetRolePermissionsAsync(role).ConfigureAwait(false))
                if (existing.Add(p)) toAdd.Add(p);
        }

        var clone = identity.Clone();
        foreach (var p in toAdd) clone.AddClaim(new Claim(GroupsClaimType, p));
        clone.AddClaim(new Claim(SentinelType, "1"));

        return new ClaimsPrincipal(principal.Identities
            .Select(i => i == identity ? clone : i.Clone()));
    }
}

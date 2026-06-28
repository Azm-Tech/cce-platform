# Claims-Based Permissions: DB Migration Implementation Plan

## Decisions

| Question | Decision |
|---|---|
| User-level permission overrides | **No** — role-based only |
| Audit trail for matrix changes | **Yes** — `PermissionAuditLog` table |
| Permission naming convention | **lowercase.dot.case** — `news.publish`, `community.post.create` |
| Anonymous role | **Pseudo-role** — not in `AspNetRoles`; static virtual role in code |
| Storage for permission catalog | **`AspNetRoleClaims` directly** — no separate `Permission` table needed |

---

## Why No Separate `Permission` Table

ASP.NET Identity already provides everything:

| Need | Existing table | How |
|---|---|---|
| List all known permissions | `AspNetRoleClaims` | `SELECT DISTINCT claim_value WHERE claim_type = 'permission'` |
| Role → permission assignments | `AspNetRoleClaims` | one row per assignment |
| User → effective permissions | `AspNetRoleClaims` JOIN `AspNetUserRoles` | resolve via role memberships |
| "Create" a permission | `AspNetRoleClaims` | a permission exists the moment it is assigned to at least one role |
| "Delete" a permission | `AspNetRoleClaims` | remove all rows with that `claim_value` |

A separate catalog table would only add: a description field and an independent existence before any role assignment. Neither is needed for the matrix CRUD. The first segment of the lowercase name (`news.publish` → group `news`) is derivable without storage.

The only new table is `PermissionAuditLog` (required by the audit decision).

---

## Current Architecture (Quick Reference)

| Layer | What happens now |
|---|---|
| `permissions.yaml` | Source of truth — nested groups, each leaf has `description` + `roles` |
| `PermissionsGenerator.cs` (Roslyn) | Reads YAML → emits `Permissions.g.cs` (constants) + `RolePermissionMap.g.cs` |
| `RolesAndPermissionsSeeder` | Seeds `AspNetRoles` + `AspNetRoleClaims` (`ClaimType="permission"`) from `RolePermissionMap` |
| `LocalTokenService` | JWT holds only `roles` — no permissions in token |
| `RoleToPermissionClaimsTransformer` | Expands `roles` → `groups` claims via **static** `RolePermissionMap` |
| `PermissionPolicyRegistration` | One ASP.NET policy per `Permissions.All` entry |
| `AuthService.BuildDtoAsync` | Login response: `{ roles: [...] }` — no claims list |

**What changes:**
1. Rename all permission values to `lowercase.dot.case` (generator + data migration).
2. Transformer reads from `AspNetRoleClaims` (DB) instead of static `RolePermissionMap`.
3. Login response includes `claims: [...]`.
4. Super-admin endpoints to CRUD permissions and toggle role assignments via `AspNetRoleClaims`.

---

## Phase 0 — Rename Convention: `lowercase.dot.case`

Do this first — every subsequent phase emits or stores lowercase names.

### 0.1 Update Roslyn source generator
**File:** `src/CCE.Domain.SourceGenerators/PermissionsGenerator.cs`

YAML stays PascalCase (human-readable, structural). Generator lowercases **emitted string values** only. C# constant identifiers stay PascalCase.

**Line 326** — value emission in `Permissions` class:
```csharp
// Before:
sb.AppendLine($"    public const string {memberName} = \"{e.Name}\";");
// After:
sb.AppendLine($"    public const string {memberName} = \"{e.Name.ToLowerInvariant()}\";");
```

**Line 369** — value emission in `RolePermissionMap`:
```csharp
// Before:
sb.AppendLine($"        \"{name}\",");
// After:
sb.AppendLine($"        \"{name.ToLowerInvariant()}\",");
```

`Permissions.All` references the constants (not string literals) so it picks up lowercase automatically — no change needed there.

`IsValidPermissionName` (the PascalCase validator) stays — it validates YAML source, not emitted values.

After rebuild: `Permissions.News_Publish == "news.publish"`, `Permissions.Community_Post_Create == "community.post.create"`. All `.RequireAuthorization(Permissions.News_Publish)` call sites are unchanged.

### 0.2 Data migration — lowercase existing `AspNetRoleClaims` rows
In the new EF migration's `Up()`:
```csharp
migrationBuilder.Sql(
    "UPDATE asp_net_role_claims SET claim_value = LOWER(claim_value) WHERE claim_type = 'permission'");
```

Converts existing rows (`"News.Publish"` → `"news.publish"`) before any code reads from DB.

---

## Phase 1 — Audit Table

The only new DB entity.

### 1.1 Domain entity
**New file:** `src/CCE.Domain/Identity/PermissionAuditLog.cs`

```csharp
namespace CCE.Domain.Identity;

public sealed class PermissionAuditLog
{
    public long Id { get; private set; }                  // identity; cheaper than Guid for append-only
    public DateTimeOffset ChangedAtUtc { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public string ChangedByEmail { get; private set; }
    public string RoleName { get; private set; }           // e.g., "cce-admin"
    public string PermissionName { get; private set; }     // e.g., "news.publish"
    public PermissionAuditAction Action { get; private set; }

    private PermissionAuditLog() { ChangedByEmail = ""; RoleName = ""; PermissionName = ""; }

    public static PermissionAuditLog Record(
        DateTimeOffset now, Guid actorId, string actorEmail,
        string role, string permission, PermissionAuditAction action) => new()
    {
        ChangedAtUtc    = now,
        ChangedByUserId = actorId,
        ChangedByEmail  = actorEmail,
        RoleName        = role,
        PermissionName  = permission,
        Action          = action,
    };
}

public enum PermissionAuditAction { Granted = 1, Revoked = 2 }
```

No FK to `AspNetRoles` or `AspNetUsers` — audit rows must survive deletions.

### 1.2 EF configuration
**New file:** `src/CCE.Infrastructure/Persistence/Configurations/PermissionAuditLogConfiguration.cs`

```csharp
internal sealed class PermissionAuditLogConfiguration : IEntityTypeConfiguration<PermissionAuditLog>
{
    public void Configure(EntityTypeBuilder<PermissionAuditLog> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseIdentityColumn();
        builder.Property(p => p.ChangedByEmail).HasMaxLength(256);
        builder.Property(p => p.RoleName).HasMaxLength(100);
        builder.Property(p => p.PermissionName).HasMaxLength(200);
    }
}
```

### 1.3 Add DbSet to CceDbContext
```csharp
public DbSet<PermissionAuditLog> PermissionAuditLogs => Set<PermissionAuditLog>();
```

### 1.4 EF migration
```powershell
dotnet ef migrations add AddPermissionAuditLog `
    --project src/CCE.Infrastructure `
    --startup-project src/CCE.Infrastructure
```

Include the lowercase SQL from Phase 0.2 in this same migration's `Up()`.

---

## Phase 2 — Infrastructure: DB-Backed Permission Resolver

### 2.1 Application interface
**New file:** `src/CCE.Application/Identity/Auth/Common/IPermissionService.cs`

```csharp
namespace CCE.Application.Identity.Auth.Common;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetRolePermissionsAsync(string roleName, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUserEffectivePermissionsAsync(Guid userId, CancellationToken ct = default);
    void InvalidateCacheForRole(string roleName);
}
```

### 2.2 Infrastructure implementation
**New file:** `src/CCE.Infrastructure/Identity/PermissionService.cs`

```csharp
public sealed class PermissionService : IPermissionService
{
    // Anonymous is not in AspNetRoles. Its permissions come from the generated map
    // (seeded from YAML). After Phase 0 these are lowercase values.
    private static readonly IReadOnlyList<string> AnonymousPermissions = RolePermissionMap.Anonymous;

    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

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

        // Reads from AspNetRoleClaims via Identity
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

        var result = all.ToArray();
        _cache.Set(key, (IReadOnlyList<string>)result, CacheTtl);
        return result;
    }

    public void InvalidateCacheForRole(string roleName)
        => _cache.Remove($"role-perm:{roleName}");
}
```

### 2.3 Update `RoleToPermissionClaimsTransformer`
**File:** `src/CCE.Api.Common/Authorization/RoleToPermissionClaimsTransformer.cs`

Replace the static switch with `IPermissionService` via `IServiceScopeFactory` (singleton transformer, scoped DB):

```csharp
public sealed class RoleToPermissionClaimsTransformer : IClaimsTransformation
{
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
        var existing = new HashSet<string>(
            principal.FindAll(GroupsClaimType).Select(c => c.Value), StringComparer.Ordinal);

        var toAdd = new List<string>();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var svc = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        foreach (var role in roleValues)
            foreach (var p in await svc.GetRolePermissionsAsync(role).ConfigureAwait(false))
                if (existing.Add(p)) toAdd.Add(p);

        var clone = identity.Clone();
        foreach (var p in toAdd) clone.AddClaim(new Claim(GroupsClaimType, p));
        clone.AddClaim(new Claim(SentinelType, "1"));

        return new ClaimsPrincipal(principal.Identities
            .Select(i => i == identity ? clone : i.Clone()));
    }
    // constants SentinelType / RolesClaimType / GroupsClaimType unchanged
}
```

### 2.4 Register services
```csharp
services.AddScoped<IPermissionService, PermissionService>();
services.AddMemoryCache();
```

---

## Phase 3 — Login Response: Include `claims` Array

### 3.1 `AuthUserDto`
**File:** `src/CCE.Application/Identity/Auth/Common/AuthUserDto.cs`

```csharp
public sealed record AuthUserDto(
    Guid Id,
    string EmailAddress,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Claims);  // ← new
```

### 3.2 `AuthService.BuildDtoAsync`
**File:** `src/CCE.Infrastructure/Identity/AuthService.cs`

Inject `IPermissionService` into the constructor, then:

```csharp
private async Task<AuthTokenDto> BuildDtoAsync(
    User user, TokenIssueResult issued, CancellationToken ct = default)
{
    var roles  = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
    var claims = await _permissionService
        .GetUserEffectivePermissionsAsync(user.Id, ct).ConfigureAwait(false);

    return new AuthTokenDto(
        issued.AccessToken, issued.AccessTokenExpiresAtUtc,
        issued.RefreshToken, issued.RefreshTokenExpiresAtUtc, "Bearer",
        new AuthUserDto(user.Id, user.Email ?? string.Empty,
            user.FirstName, user.LastName, roles.ToArray(), claims));
}
```

Login response after this change:
```json
{
  "accessToken": "...",
  "user": {
    "id": "...",
    "roles": ["cce-admin"],
    "claims": ["news.publish", "news.update", "user.read", "user.create", ...]
  }
}
```

---

## Phase 4 — Application Layer: Admin CRUD

All files under `src/CCE.Application/Identity/Permissions/`.

### DTOs

```csharp
// PermissionSummaryDto.cs — one row in the list or matrix header
public sealed record PermissionSummaryDto(string Name, string Group);
// Group is derived: "news.publish".Split('.')[0] → "news"

// PermissionMatrixDto.cs
public sealed record PermissionMatrixDto(
    IReadOnlyList<PermissionSummaryDto> Permissions,
    IReadOnlyList<string> Roles,
    // key = role name; value = set of permission names assigned to that role
    IReadOnlyDictionary<string, IReadOnlyList<string>> Assignments);
```

### Queries

**`GetPermissionsQuery`** — distinct permission names from `AspNetRoleClaims`
```
SELECT DISTINCT claim_value
FROM asp_net_role_claims
WHERE claim_type = 'permission'
ORDER BY claim_value
```
Returns `IReadOnlyList<PermissionSummaryDto>` (name + derived group).

**`GetPermissionMatrixQuery`** — full grid for admin UI
```
1. Load roles: AspNetRoles (all real roles)
2. Load assignments: AspNetRoleClaims WHERE claim_type = 'permission'
3. Build matrix: for each role, list of its permission claim_values
```
Returns `PermissionMatrixDto`.

### Commands

**`UpdateRolePermissionsCommand`** `{ RoleName, PermissionNames: IReadOnlySet<string> }`
```
Load role from AspNetRoles (error if not found)
Load existing claims: RoleManager.GetClaimsAsync(role) WHERE type = "permission"

Diff:
  added   = PermissionNames − existing
  removed = existing − PermissionNames

In one transaction (use RoleManager API to stay within Identity):
  foreach added:   RoleManager.AddClaimAsync(role, new Claim("permission", name))
  foreach removed: RoleManager.RemoveClaimAsync(role, new Claim("permission", name))

Audit: one PermissionAuditLog row per added (Granted) and per removed (Revoked)
  — use ICurrentUserAccessor for actor info, ISystemClock for timestamp

Cache: IPermissionService.InvalidateCacheForRole(RoleName)
```

That's the only command needed for the matrix. "Create a permission" = assign it to a role (it appears in the DISTINCT list immediately). "Delete a permission" = unassign from all roles via the matrix.

**Permissions to add to `permissions.yaml`** (rebuild after adding):
```yaml
  Permission:
    Read:
      description: View permission catalog and role-permission matrix
      roles: [cce-super-admin]
    Manage:
      description: Toggle role-permission assignments
      roles: [cce-super-admin]
```
Generates `Permissions.Permission_Read = "permission.read"` and `Permissions.Permission_Manage = "permission.manage"`.

---

## Phase 5 — API Endpoints (Internal, Super Admin Only)

**New file:** `src/CCE.Api.Internal/Endpoints/PermissionEndpoints.cs`

```
GET  /admin/permissions          → GetPermissionsQuery          [permission.read]
GET  /admin/permissions/matrix   → GetPermissionMatrixQuery     [permission.read]
PUT  /admin/roles/{role}/permissions  → UpdateRolePermissionsCommand  [permission.manage]
```

Three endpoints total. The matrix PUT replaces the entire permission set for a role atomically — the frontend sends the full checked state of one column.

**`PUT /admin/roles/{role}/permissions` body:**
```json
{ "permissions": ["news.publish", "news.update", "user.read"] }
```

**`GET /admin/permissions/matrix` response:**
```json
{
  "permissions": [
    { "name": "news.publish",           "group": "news" },
    { "name": "news.update",            "group": "news" },
    { "name": "community.post.create",  "group": "community" }
  ],
  "roles": ["cce-super-admin", "cce-admin", "cce-content-manager", ...],
  "assignments": {
    "cce-admin":           ["news.publish", "news.update", "user.read"],
    "cce-content-manager": ["news.publish", "resource.center.upload"]
  }
}
```

Frontend renders: rows = permissions (grouped by `group`), columns = roles, cell = checkbox. On save: `PUT /admin/roles/{role}/permissions` with the full checked set for that column.

---

## Phase 6 — Dynamic Policy Provider

New admin-created permissions won't have pre-registered policies. Replace the static loop with an on-demand provider.

**New file:** `src/CCE.Api.Common/Authorization/DynamicPermissionPolicyProvider.cs`

```csharp
public sealed class DynamicPermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Any dotted name → "require groups claim" policy, no pre-registration needed.
        if (policyName.Contains('.', StringComparison.Ordinal))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim("groups", policyName)
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();
}
```

**Update `PermissionPolicyRegistration`:**
```csharp
public static IServiceCollection AddCcePermissionPolicies(this IServiceCollection services)
{
    services.AddSingleton<IClaimsTransformation, RoleToPermissionClaimsTransformer>();
    services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
    services.AddAuthorization();  // no static loop needed
    return services;
}
```

---

## Migration Safety

| Concern | Status |
|---|---|
| Existing role-permission assignments | Safe — `AspNetRoleClaims` is unchanged except lowercase conversion |
| Existing users' effective permissions | Safe — same rows, same join logic, just lowercase values |
| `RoleToPermissionClaimsTransformer` output | Identical to before — DB was seeded from the same YAML |
| Anonymous permissions | Unchanged — static `RolePermissionMap.Anonymous` (now lowercase after Phase 0) |

---

## Implementation Order

| # | Phase | Key files | Effort |
|---|---|---|---|
| 0 | Lowercase: generator + data migration SQL | `PermissionsGenerator.cs` (2 lines), EF migration | S |
| 1 | Audit table | `PermissionAuditLog.cs`, EF config, `CceDbContext.cs`, EF migration | S |
| 2 | DB-backed resolver | `IPermissionService.cs`, `PermissionService.cs`, updated transformer, DI reg | M |
| 3 | Login response claims | `AuthUserDto.cs`, `AuthService.cs` | S |
| 4 | Admin commands/queries | ~4 files in Application layer | S |
| 5 | Admin endpoints | `PermissionEndpoints.cs` | S |
| 6 | Dynamic policy provider | `DynamicPermissionPolicyProvider.cs`, `PermissionPolicyRegistration.cs` | S |

**Total: ~1.5 days.** One new table (audit), three new endpoints, one generator change.

# Sub-11 Phase 04 — Cutover + Keycloak deletion + close-out

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Delete the Keycloak surface across the codebase, remove the Sub-11 coexistence scaffolding (custom BFF cluster, dual-claim transformer's legacy branch, `KEYCLOAK_*` + `LDAP_*` env-keys, `ad-federation.md` runbook, `Testcontainers.Keycloak` reference), ship the cutover + troubleshooting runbooks, write the Sub-11 completion doc + CHANGELOG entry, and tag `entra-id-v1.0.0`.

**Architecture:** Phase 04 is the **only** phase that touches operationally-running infrastructure. All prior phases have been additive (Phase 00-02) or coexistent-with-Keycloak (Phase 03). Phase 04 deletes the deprecated surface in a single coordinated pass: backend code, tests, infra scripts, env-keys, and docs. The deletions are mechanical — there are no architectural decisions left, just removal + reference fixes + a final verification sweep.

**Operational note:** The `entra-id-cutover.md` runbook this phase ships is a **maintenance-window procedure** that the operator runs against deployed environments. Phase 04 ships the *artifact* (the runbook); the *cutover itself* happens at deploy time per env.

**Tech Stack:** PowerShell 7+ (cutover scripts) · existing CCE backend (deletion-only changes) · existing CCE frontend (no changes — Phase 03 already migrated)

**Test count target:** Backend Infrastructure 87 → **84** (-3 KeycloakLdap). IntegrationTests `RoleToPermissionClaimsTransformerTests` 7 → **4** (-3 legacy-branch tests + 1 retargeted to `roles` claim). IntegrationTests `BffSessionMiddlewareTests` 5 → **0** (deleted with the BFF cluster). Net Infrastructure delta: -3. Net IntegrationTests delta: -8 (5 BFF + 3 transformer-legacy). Frontend unchanged at 720 (web-portal 502 + admin-cms 218).

---

## Phase 04 deliverables (5 tasks)

| # | Layer | Outcome |
|---|---|---|
| 4.1 | Docs (creates) | `entra-id-cutover.md` (12-step + rollback runbook) + `entra-id-troubleshooting.md` |
| 4.2 | Backend code | Delete custom BFF cluster (7 files) + `BffSessionMiddlewareTests.cs`; update `BffRegistration.cs` to M.I.W-only; remove `AddCceBff`/`UseCceBff`/`MapBffAuthEndpoints` from `Program.cs` |
| 4.3 | Backend tests/infra | Delete `KeycloakLdapFixture.cs` + `KeycloakLdapFederationTests.cs`; remove `Testcontainers.Keycloak` from csproj + `Directory.Packages.props`; delete `infra/keycloak/`; remove legacy-branch from `RoleToPermissionClaimsTransformer` + retarget existing tests |
| 4.4 | Env-files + docs | Delete `KEYCLOAK_*` + `LDAP_*` blocks from all 6 env-file `.example` templates + `.env.local.example` Keycloak secrets; delete `docs/runbooks/ad-federation.md` |
| 4.5 | Close-out | `docs/sub-11-entra-id-migration-completion.md` + `CHANGELOG.md` `[entra-id-v1.0.0]` entry + tag `entra-id-v1.0.0` |

---

## Global conventions (Phase 04)

- All commits use Conventional Commits (`feat`, `refactor`, `docs`, `chore`).
- All commits include the `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` trailer.
- `dotnet test` invocations run from `/Users/m/CCE/backend`.
- Each task ends with green build + targeted test run + commit.
- The final tag (`entra-id-v1.0.0`) is created **after** the close-out commit lands and **all** test suites are green.

---

## Task 4.1: Cutover + troubleshooting runbooks

**Files:**
- Create: `docs/runbooks/entra-id-cutover.md`
- Create: `docs/runbooks/entra-id-troubleshooting.md`

The cutover runbook is a 12-step procedure the operator runs at maintenance window. The troubleshooting runbook captures the common failure modes the operator can hit during or after cutover.

- [ ] **Step 1: Create `entra-id-cutover.md`**

```markdown
# Entra ID cutover runbook (Sub-11)

Maintenance-window procedure for swapping a CCE environment from
Keycloak to multi-tenant Entra ID. Run **per env** in this order:
test → preprod → prod → dr.

**Estimated downtime per env:** 15–30 minutes.

**Rollback:** revert to the prior `app-v*.*.*` image tag via
`deploy.ps1 -Environment <env> -Rollback` (Sub-10b). The migration
`AddEntraIdObjectIdToUser` is forward-only-friendly (additive nullable
column + filtered unique index); old images ignore the column.

## Prerequisites

- Phase 02 PowerShell scripts (`apply-app-registration.ps1` +
  `Configure-Branding.ps1`) have been run successfully against the
  CCE Entra ID tenant.
- The runtime CCE app's `ENTRA_CLIENT_ID` / `ENTRA_CLIENT_SECRET` /
  `ENTRA_TENANT_ID` are populated in `C:\ProgramData\CCE\.env.<env>`.
- A Conditional Access policy targeting the CCE app is configured
  (per ADR-0060).
- Outbound HTTPS to `login.microsoftonline.com` and
  `graph.microsoft.com` is whitelisted on the deploy host.

## Steps

1. **Pre-cutover snapshot.** Capture the current SQL Server state via
   `infra/backup/Test-BackupChain.ps1 -Environment <env>` and confirm
   the latest full backup is on the off-host UNC share.

2. **Operator check-in.** Post a `#cce-ops` Slack message announcing
   the maintenance window (start time + estimated end). Set the
   environment's status page (if any) to "Maintenance".

3. **Halt traffic.** On the deploy host: `iisreset /stop`. The IIS
   reverse proxy stops accepting traffic; ARR rules return 503.

4. **Deploy the cutover image.** From the repo root on the deploy host:
   ```powershell
   .\deploy\deploy.ps1 -Environment <env> -ImageTag entra-id-v1.0.0
   ```
   This pulls the Sub-11 Phase 04 backend image, runs the EF migration
   `AddEntraIdObjectIdToUser` (no-op if previously applied), and
   restarts the app containers.

5. **Verify migration applied.** SSMS:
   ```sql
   SELECT TOP 1 name FROM sys.columns
     WHERE object_id = OBJECT_ID('[identity].[Users]')
       AND name = 'entra_id_object_id';
   ```
   Expected: one row.

6. **Resume traffic.** `iisreset /start`. ARR resumes proxying;
   smoke probes (`https://<hostname>/health/ready`) should return
   200 within 30 s.

7. **Backfill objectId for existing users.** `EntraIdUserResolver`
   does this lazily on first sign-in per user. No batch step needed
   on cutover day, but operators may run a manual sync via the
   `/api/admin/users/sync` endpoint (Sub-11d work — defer if not yet
   shipped).

8. **Smoke-test sign-in.** From a fresh browser:
   - Navigate to `https://<portal-hostname>/`
   - Click Sign In
   - Verify redirect to `login.microsoftonline.com/<tenant>/oauth2/v2.0/authorize`
   - Complete Entra ID login (with MFA if CA policy demands it)
   - Verify return to `/me/profile` with permissions resolved

9. **Smoke-test admin CMS.** Same flow against `https://<cms-hostname>/`.

10. **Verify Conditional Access.** Sign-in attempt **without** MFA
    (use a test account with conditional access disabled per-account
    via Entra ID portal): MFA prompt should fire. With MFA satisfied:
    sign-in completes.

11. **Decommission Keycloak (deferred — operator's call):**
    Stop the Keycloak container/service. Operators may keep it running
    for a 7-day rollback window before fully decommissioning.

12. **Operator check-out.** Post completion message to `#cce-ops`.
    Mark status page back to "Operational". Add an entry to the
    deploy-history TSV for the env.

## Rollback

If steps 5–10 surface an issue that can't be fixed within the
maintenance window:

```powershell
.\deploy\rollback.ps1 -Environment <env>
```

This reverts to the prior image tag. Keycloak is still running
(per step 11 deferral). Sign-in flows through Keycloak again. The
`entra_id_object_id` column stays in the schema (forward-only-friendly
migration); old images simply ignore it.

Post the rollback to `#cce-ops`. Open a follow-up issue documenting
what failed; do not retry until the issue is resolved in a code change.
```

- [ ] **Step 2: Create `entra-id-troubleshooting.md`**

```markdown
# Entra ID troubleshooting runbook (Sub-11)

Common failure modes when running CCE on Entra ID. This runbook
replaces `ad-federation.md` (deleted in Phase 04 — Keycloak no longer
in use).

## Sign-in fails with "AADSTS50011: redirect URI mismatch"

The Entra ID app registration's redirect URIs don't include the
hostname the user is trying to sign in to. Fix:

1. Run `infra/entra/apply-app-registration.ps1 -Environment <env>`
   to PATCH the manifest. The script substitutes `{{HOSTNAME_*}}`
   from the env-file at apply time — confirm the env-file has the
   correct hostnames.
2. If a single redirect URI is missing for a one-off URL, add it
   manually in the Entra ID portal → App registrations → CCE Knowledge
   Center → Authentication → Web → Redirect URIs.

## Sign-in fails with "AADSTS70011: invalid scope"

Token request asked for a scope the app registration didn't grant.
Most common cause: the app's `requiredResourceAccess[].resourceAccess`
list in the manifest doesn't include the requested scope. Fix:
edit `infra/entra/app-registration-manifest.json`, re-run
`apply-app-registration.ps1`, ask the user to clear cookies and retry.

## Graph user-create returns `Authorization_RequestDenied`

The runtime CCE app is missing `User.ReadWrite.All` admin consent.
Fix:

1. Entra ID portal → App registrations → CCE Knowledge Center → API
   permissions
2. Confirm `User.ReadWrite.All` (Application) is listed
3. Click **Grant admin consent for <tenant>**
4. Wait ~5 minutes for the consent to propagate

If the runtime app instead has the **wrong tenant**'s admin consent:
delete the app reg, re-run `apply-app-registration.ps1` against the
correct tenant.

## `EntraIdUserResolver` log: "Concurrent EntraIdObjectId link race"

Two requests with the same `oid` claim raced to insert the link row.
The filtered unique index rejected the duplicate. Action: none — the
resolver swallows the `DbUpdateException` at Information level. The
next request for the same user sees the linked state and short-circuits.

If the log fires repeatedly (more than ~10/min) it indicates a
stampede. Investigate upstream cookie / session expiry behavior.

## Branding doesn't render after `Configure-Branding.ps1` success

Entra ID caches branding for ~1 hour at the CDN edge. Either:
- Wait 1 hour, or
- Test in a private/incognito window with cleared cookies
- Verify the tenant has Entra ID P1 or P2 SKU
  (`Configure-Branding.ps1` will exit 0 with a warning if absent;
  check the script's log file at `C:\ProgramData\CCE\logs\entra-branding-*.log`)

## Conditional Access policy doesn't enforce MFA

Validation: Entra ID portal → Security → Conditional Access → policies
→ confirm a policy targets the CCE app and the assignment is set to
"Require multi-factor authentication".

If the policy exists but isn't firing, check:
- Policy state is **On** (not Report-only)
- The user's tenant is a member tenant of the CA policy (not a guest
  tenant — Entra ID doesn't extend CA across tenants; ADR-0060)
- The user's account isn't excluded by the assignment filter

## Multi-tenant: partner user gets "AADSTS50105: User not assigned to a role"

The CCE app registration has `appRoleAssignmentRequired: true` (or
the partner tenant's admin enabled it). Either:
- Disable role-assignment-required on the app registration, OR
- Have the partner tenant's admin assign at least one app role to
  the user

## Token-cache misses spike after a deploy

Microsoft.Identity.Web's in-memory token cache empties on app restart.
Expected behavior; signs of a healthy cutover. Monitor: cache hit
rate should recover within 10 minutes as users re-sign-in.

## See also

- `entra-id-cutover.md` — maintenance-window procedure
- `infra/entra/README.md` — provisioning script reference
- ADR-0058, ADR-0059, ADR-0060
```

- [ ] **Step 3: Verify the markdown renders cleanly**

```bash
ls /Users/m/CCE/docs/runbooks/entra-id-*.md
```

Expected: 2 files exist.

- [ ] **Step 4: Commit**

```bash
git add docs/runbooks/entra-id-cutover.md docs/runbooks/entra-id-troubleshooting.md
git commit -m "$(cat <<'EOF'
docs(runbooks): Sub-11 Phase 04 — Entra ID cutover + troubleshooting

entra-id-cutover.md: 12-step maintenance-window procedure for swapping
a CCE env from Keycloak to multi-tenant Entra ID. Run per env (test →
preprod → prod → dr). Includes prerequisites, step-by-step procedure,
rollback procedure, and the deferred-decommission window for Keycloak.

entra-id-troubleshooting.md: replaces ad-federation.md (deleted in
later Phase 04 task). Covers AADSTS50011/70011, Graph
Authorization_RequestDenied, concurrent objectId link races, branding
cache, Conditional Access, multi-tenant role-assignment, and post-deploy
token-cache misses.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4.2: Delete custom BFF cluster + update Program.cs + BffRegistration

**Files:**
- Delete: `backend/src/CCE.Api.Common/Auth/BffSessionMiddleware.cs`
- Delete: `backend/src/CCE.Api.Common/Auth/BffAuthEndpoints.cs`
- Delete: `backend/src/CCE.Api.Common/Auth/BffTokenRefresher.cs`
- Delete: `backend/src/CCE.Api.Common/Auth/BffSessionCookie.cs`
- Delete: `backend/src/CCE.Api.Common/Auth/BffSession.cs`
- Delete: `backend/src/CCE.Api.Common/Auth/BffOptions.cs`
- Delete: `backend/src/CCE.Api.Common/Auth/BffTokenResponse.cs`
- Delete: `backend/tests/CCE.Api.IntegrationTests/Auth/BffSessionMiddlewareTests.cs`
- Modify: `backend/src/CCE.Api.Common/Auth/BffRegistration.cs` (drop coexistence; M.I.W-only)
- Modify: `backend/src/CCE.Api.External/Program.cs` (remove `UseCceBff` + `MapBffAuthEndpoints` calls)

The custom BFF cluster was the Phase 00 coexistence scaffolding. Phase 04 deletes it; M.I.W's `AddMicrosoftIdentityWebApp` (already wired in Phase 00 `BffRegistration`) is now the only auth path.

- [ ] **Step 1: Delete the BFF cluster files**

```bash
cd /Users/m/CCE/backend && rm \
  src/CCE.Api.Common/Auth/BffSessionMiddleware.cs \
  src/CCE.Api.Common/Auth/BffAuthEndpoints.cs \
  src/CCE.Api.Common/Auth/BffTokenRefresher.cs \
  src/CCE.Api.Common/Auth/BffSessionCookie.cs \
  src/CCE.Api.Common/Auth/BffSession.cs \
  src/CCE.Api.Common/Auth/BffOptions.cs \
  src/CCE.Api.Common/Auth/BffTokenResponse.cs \
  tests/CCE.Api.IntegrationTests/Auth/BffSessionMiddlewareTests.cs
```

- [ ] **Step 2: Update `BffRegistration.cs`** — drop the coexistence comments, drop `BffOptions` binding, drop the `IHttpClientFactory("keycloak-bff")` registration, drop the resolver/refresher/cookie services, **keep** the `AddMicrosoftIdentityWebApp` block + `EntraIdUserResolver` registration.

The new shape (read the current file first to preserve any non-deletion content):

```csharp
using CCE.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace CCE.Api.Common.Auth;

/// <summary>
/// Sub-11 — registers Microsoft.Identity.Web's OpenIdConnect + Cookie
/// auth schemes against multi-tenant Entra ID, enables the in-memory
/// token cache for downstream Graph calls, and hooks the lazy
/// UPN→objectId resolver onto OnTokenValidated.
///
/// Pre-Sub-11 this file co-hosted a custom BFF (BffSessionMiddleware,
/// BffSessionCookie, BffTokenRefresher) for the Keycloak path — Phase
/// 04 deleted that surface; Microsoft.Identity.Web is now the only
/// auth path.
/// </summary>
public static class BffRegistration
{
    private static readonly string[] DownstreamScopes = { "User.Read" };

    public static IServiceCollection AddCceBff(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<EntraIdUserResolver>();

        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(configuration, configSectionName: EntraIdOptions.SectionName)
            .EnableTokenAcquisitionToCallDownstreamApi(DownstreamScopes)
            .AddInMemoryTokenCaches();

        services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, opts =>
        {
            var existingOnTokenValidated = opts.Events.OnTokenValidated;
            opts.Events.OnTokenValidated = async ctx =>
            {
                if (existingOnTokenValidated is not null)
                {
                    await existingOnTokenValidated(ctx).ConfigureAwait(false);
                }
                var resolver = ctx.HttpContext.RequestServices.GetRequiredService<EntraIdUserResolver>();
                await resolver.EnsureLinkedAsync(ctx.Principal!).ConfigureAwait(false);
            };
        });

        return services;
    }
}
```

The previous `UseCceBff` extension method (which mounted `BffSessionMiddleware`) is **deleted**. Microsoft.Identity.Web doesn't need a custom middleware step — the OpenIdConnect + Cookie schemes do their work via `UseAuthentication()` which is already in `Program.cs`.

- [ ] **Step 3: Update `backend/src/CCE.Api.External/Program.cs`**

Read the file. Remove these lines:
- `using ... CCE.Api.Common.Auth` (if it's there — keep `AddCceBff` if still used)
- `app.UseCceBff();` (mid-pipeline middleware mount)
- `app.MapBffAuthEndpoints();` (endpoint mount)

Keep:
- `services.AddCceBff(builder.Configuration)` (the auth registration; now points at M.I.W)
- `app.UseAuthentication()` + `app.UseAuthorization()`

Check `backend/src/CCE.Api.Internal/Program.cs` similarly — Internal API may not have wired the BFF cluster (CMS uses JWT bearer only); confirm no `UseCceBff` / `MapBffAuthEndpoints` references in Internal.

- [ ] **Step 4: Build the backend**

```bash
cd /Users/m/CCE/backend && dotnet build --nologo --verbosity minimal 2>&1 | tail -10
```

Expected: clean build. Likely failures:
- `BffSession` / `BffOptions` references still in some other file → grep wider, fix
- Any `IClaimsTransformation` test that constructs a `BffSession` directly → already deleted with `BffSessionMiddlewareTests.cs`

```bash
grep -rn "BffSession\|BffAuth\|BffToken\|BffOptions\|BffSessionMiddleware\|MapBffAuthEndpoints\|UseCceBff" /Users/m/CCE/backend/src /Users/m/CCE/backend/tests --include="*.cs"
```

Expected: only references inside `BffRegistration.cs` (the doc comment) — if any other file still references the deleted types, fix it.

- [ ] **Step 5: Run the full backend test sweep**

```bash
cd /Users/m/CCE/backend && \
  dotnet test tests/CCE.Domain.Tests/        --no-build --nologo --verbosity minimal | tail -3 && \
  dotnet test tests/CCE.Application.Tests/   --no-build --nologo --verbosity minimal | tail -3 && \
  dotnet test tests/CCE.ArchitectureTests/   --no-build --nologo --verbosity minimal | tail -3 && \
  dotnet test tests/CCE.Api.IntegrationTests/ --no-build --nologo --verbosity minimal | tail -3
```

Expected: Domain 290 / Application 439 / Architecture 12 (unchanged); IntegrationTests test count drops by 5 (BffSessionMiddlewareTests). Other IntegrationTests still pass.

- [ ] **Step 6: Commit**

```bash
git add -A backend/src/CCE.Api.Common/Auth/ backend/tests/CCE.Api.IntegrationTests/Auth/ backend/src/CCE.Api.External/Program.cs
git commit -m "$(cat <<'EOF'
refactor(api-common): Sub-11 Phase 04 — delete custom BFF cluster

Removes the 7-file Bff* cluster that pre-Sub-11 implemented a custom
BFF cookie-session pipeline against Keycloak. Microsoft.Identity.Web
(wired in Phase 00) is now the only auth path.

Deleted:
- BffSessionMiddleware, BffAuthEndpoints, BffTokenRefresher
- BffSessionCookie, BffSession, BffOptions, BffTokenResponse
- BffSessionMiddlewareTests (5 IntegrationTests)

BffRegistration.cs simplified: keeps the M.I.W AddMicrosoftIdentityWebApp
+ EnableTokenAcquisitionToCallDownstreamApi + InMemoryTokenCaches block
+ the EntraIdUserResolver hook on OnTokenValidated. Drops the
coexistence-era Bff service registrations.

Program.cs (External): drops UseCceBff() middleware mount and
MapBffAuthEndpoints() endpoint mount. UseAuthentication +
UseAuthorization remain (they handle M.I.W's schemes).

IntegrationTests count: -5 (BffSessionMiddlewareTests). No other
suites affected.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4.3: Delete Keycloak surface + update transformer + remove Testcontainers.Keycloak

**Files:**
- Delete: `backend/tests/CCE.Infrastructure.Tests/Identity/KeycloakLdapFixture.cs`
- Delete: `backend/tests/CCE.Infrastructure.Tests/Identity/KeycloakLdapFederationTests.cs`
- Delete (recursive): `infra/keycloak/` (apply-realm.ps1 + realm-cce-ldap-federation.json)
- Modify: `backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj` (remove `Testcontainers.Keycloak` reference)
- Modify: `backend/Directory.Packages.props` (remove `Testcontainers.Keycloak` entry)
- Modify: `backend/src/CCE.Api.Common/Authorization/RoleToPermissionClaimsTransformer.cs` (remove legacy `groups`-claim branch + Keycloak role-name mappings + slash-prefix normalization)
- Modify: `backend/tests/CCE.Api.IntegrationTests/Authorization/RoleToPermissionClaimsTransformerTests.cs` (delete 3 legacy tests; retarget 1 test from `groups` to `roles`)

- [ ] **Step 1: Delete the test files + infra/keycloak/**

```bash
cd /Users/m/CCE && rm \
  backend/tests/CCE.Infrastructure.Tests/Identity/KeycloakLdapFixture.cs \
  backend/tests/CCE.Infrastructure.Tests/Identity/KeycloakLdapFederationTests.cs && \
  rm -rf infra/keycloak/
```

- [ ] **Step 2: Remove `Testcontainers.Keycloak` package reference**

Edit `backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj` — drop the `<PackageReference Include="Testcontainers.Keycloak" />` line.

Edit `backend/Directory.Packages.props` — drop the `<PackageVersion Include="Testcontainers.Keycloak" Version="4.0.0" />` line.

- [ ] **Step 3: Simplify `RoleToPermissionClaimsTransformer.cs`** — remove the Sub-11 dual-claim coexistence

The transformer in Phase 03 reads BOTH `roles` and `groups` claims. Phase 04 simplifies to `roles`-only:

```csharp
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
```

Removed: dual-claim coexistence branch, Keycloak legacy mappings (`SuperAdmin`/`ContentManager`/`StateRepresentative`/`CommunityExpert`/`RegisteredUser`), slash-prefix normalization.

- [ ] **Step 4: Update `RoleToPermissionClaimsTransformerTests.cs`**

The Phase 03 test list:
1. `Anonymous_principal_is_returned_unchanged` — keep (no claim-type dependency)
2. `Legacy_SuperAdmin_groups_claim_expands_to_admin_permission_set` — **delete** (legacy branch removed)
3. `Slash_prefixed_keycloak_group_paths_are_normalized` — **delete** (slash-prefix normalization removed)
4. `Unknown_role_group_does_not_add_any_permissions` — **retarget** to use `roles` claim; rename `Unknown_role_does_not_add_any_permissions`
5. `Idempotent_when_already_transformed` — **retarget** to use `roles` claim with `cce-admin`
6. `EntraId_roles_claim_cce_admin_expands_to_full_permission_set` — keep
7. `EntraId_roles_claim_cce_user_grants_community_writes_but_not_admin_actions` — keep

Result: 4 tests in this file (was 7).

```csharp
using System.Security.Claims;
using CCE.Api.Common.Authorization;
using CCE.Domain;

namespace CCE.Api.IntegrationTests.Authorization;

public class RoleToPermissionClaimsTransformerTests
{
    [Fact]
    public async Task Anonymous_principal_is_returned_unchanged()
    {
        var anon = new ClaimsPrincipal(new ClaimsIdentity());
        var sut = new RoleToPermissionClaimsTransformer();

        var result = await sut.TransformAsync(anon);

        result.Should().BeSameAs(anon);
    }

    [Fact]
    public async Task Unknown_role_does_not_add_any_permissions()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("roles", "NotARealRole") },
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleToPermissionClaimsTransformer();

        var result = await sut.TransformAsync(principal);

        var groups = result.FindAll("groups").Select(c => c.Value).ToList();
        groups.Should().BeEmpty();
    }

    [Fact]
    public async Task Idempotent_when_already_transformed()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("roles", "cce-admin") },
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleToPermissionClaimsTransformer();

        var first = await sut.TransformAsync(principal);
        var firstCount = first.FindAll("groups").Count();

        var second = await sut.TransformAsync(first);
        var secondCount = second.FindAll("groups").Count();

        secondCount.Should().Be(firstCount, "second transform must short-circuit");
    }

    [Fact]
    public async Task EntraId_roles_claim_cce_admin_expands_to_full_permission_set()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("roles", "cce-admin") },
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleToPermissionClaimsTransformer();

        var result = await sut.TransformAsync(principal);

        var permissions = result.FindAll("groups").Select(c => c.Value).ToHashSet();
        permissions.Should().Contain(Permissions.System_Health_Read);
        permissions.Should().Contain(Permissions.User_Create);
        permissions.Should().Contain(Permissions.Role_Assign);
    }

    [Fact]
    public async Task EntraId_roles_claim_cce_user_grants_community_writes_but_not_admin_actions()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("roles", "cce-user") },
            authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        var sut = new RoleToPermissionClaimsTransformer();

        var result = await sut.TransformAsync(principal);

        var permissions = result.FindAll("groups").Select(c => c.Value).ToHashSet();
        permissions.Should().Contain(Permissions.Community_Post_Create);
        permissions.Should().Contain(Permissions.Community_Post_Reply);
        permissions.Should().NotContain(Permissions.Role_Assign);
        permissions.Should().NotContain(Permissions.User_Create);
    }
}
```

5 tests final (Anonymous + Unknown + Idempotent + 2 EntraId mapping tests). Wait — that's 5. Let me recount: target was 4. Actually the more I look at it, 5 is the right count: Anonymous + Unknown + Idempotent + cce-admin + cce-user. The plan target was wrong; **landed test count: 5** (was 7 in Phase 03, -2).

- [ ] **Step 5: Build + run tests**

```bash
cd /Users/m/CCE/backend && dotnet build --nologo --verbosity minimal 2>&1 | tail -5
```

Expected: clean build.

```bash
cd /Users/m/CCE/backend && \
  dotnet test tests/CCE.Infrastructure.Tests/  --nologo --verbosity minimal 2>&1 | tail -3 && \
  dotnet test tests/CCE.Api.IntegrationTests/ --filter "FullyQualifiedName~RoleToPermissionClaimsTransformerTests" --nologo --verbosity minimal 2>&1 | tail -3
```

Expected: Infrastructure 84 (was 87, -3 KeycloakLdap); transformer tests 5 (was 7, -2: legacy `groups` + slash-prefix).

- [ ] **Step 6: Commit**

```bash
git add -A backend/tests/CCE.Infrastructure.Tests/Identity/ \
        backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj \
        backend/Directory.Packages.props \
        backend/src/CCE.Api.Common/Authorization/RoleToPermissionClaimsTransformer.cs \
        backend/tests/CCE.Api.IntegrationTests/Authorization/RoleToPermissionClaimsTransformerTests.cs \
        infra/keycloak/
git commit -m "$(cat <<'EOF'
refactor(infra): Sub-11 Phase 04 — delete Keycloak surface + dual-claim branch

Deleted:
- infra/keycloak/ (apply-realm.ps1 + realm-cce-ldap-federation.json)
- KeycloakLdapFixture.cs + KeycloakLdapFederationTests.cs (3 tests)
- Testcontainers.Keycloak from CCE.Infrastructure.Tests.csproj +
  Directory.Packages.props
- RoleToPermissionClaimsTransformer's dual-claim coexistence branch:
  legacy `groups`-claim path, Keycloak role-name mappings (SuperAdmin /
  ContentManager / StateRepresentative / CommunityExpert /
  RegisteredUser), and slash-prefix normalization. Now reads `roles`
  claim only with cce-* values.

Test deltas:
- Infrastructure: 87 → 84 (-3 KeycloakLdap)
- IntegrationTests/RoleToPermissionClaimsTransformerTests: 7 → 5
  (-2: Legacy_SuperAdmin_groups_claim + Slash_prefixed; existing
  Unknown_role + Idempotent retargeted to use the `roles` claim)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4.4: Delete `KEYCLOAK_*` + `LDAP_*` env-keys + delete `ad-federation.md`

**Files:**
- Modify: `.env.example`, `.env.local.example`, `.env.test.example`, `.env.preprod.example`, `.env.prod.example`, `.env.dr.example`
- Delete: `docs/runbooks/ad-federation.md`

- [ ] **Step 1: Delete `ad-federation.md`**

```bash
rm /Users/m/CCE/docs/runbooks/ad-federation.md
```

- [ ] **Step 2: For each env-file `.example`, delete the `KEYCLOAK_*` block + the `LDAP_*` block**

Read each file first, then surgically remove:
- The Keycloak block (typically `KEYCLOAK_AUTHORITY` / `KEYCLOAK_AUDIENCE` / `KEYCLOAK_REQUIRE_HTTPS` + admin keys + the surrounding "DEPRECATED" comment)
- The LDAP block (typically `LDAP_HOST` / `LDAP_PORT` / `LDAP_BIND_*` / `LDAP_USERS_DN` / `LDAP_GROUPS_DN`)

For `.env.example`:
- Drop the `Keycloak (docker-compose service: keycloak)` block (~10 lines including the DEPRECATED header)
- Note: the `Entra ID (Sub-11 — multi-tenant + Microsoft Graph)` block stays.
- The Phase 00 `.env.example` doesn't have `LDAP_*` keys (only the per-env files do).

For `.env.local.example`:
- Drop `KEYCLOAK_CLIENT_SECRET_INTERNAL` + `KEYCLOAK_CLIENT_SECRET_EXTERNAL` lines.

For `.env.test.example`, `.env.preprod.example`, `.env.prod.example`, `.env.dr.example`:
- Drop the Keycloak block + Keycloak admin block + LDAP block.

- [ ] **Step 3: Update the `.env.prod.example` "Required-key catalogue" comment**

The trailing comment in `.env.prod.example` lists `KEYCLOAK_*` + `LDAP_*` as required. Replace those entries with the corresponding `ENTRA_*` + `HOSTNAME_*` keys.

- [ ] **Step 4: Sanity-check syntax**

```bash
for f in /Users/m/CCE/.env*.example; do
  echo "=== $f ==="
  grep -c "KEYCLOAK\|LDAP_" "$f" || echo "(clean)"
done
```

Expected: `(clean)` for all 6 files (the `grep -c` returns 0 → exit code 1 → fall-through to echo "(clean)").

- [ ] **Step 5: Commit**

```bash
git add -A .env.example .env.local.example .env.test.example .env.preprod.example .env.prod.example .env.dr.example docs/runbooks/ad-federation.md
git commit -m "$(cat <<'EOF'
chore(env): Sub-11 Phase 04 — delete KEYCLOAK_* + LDAP_* keys

All 6 env-file .example templates now contain only ENTRA_* +
HOSTNAME_* identity keys. The custom-BFF cluster + Keycloak realm-
provisioning that consumed KEYCLOAK_* / LDAP_* are deleted in earlier
Phase 04 tasks.

.env.prod.example "Required-key catalogue" comment updated to list the
new ENTRA_* / HOSTNAME_* keys instead of KEYCLOAK_* / LDAP_*.

Deletes docs/runbooks/ad-federation.md (Sub-10c-era runbook for
Keycloak LDAP federation; superseded by Sub-11 Phase 04
entra-id-cutover.md + entra-id-troubleshooting.md).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4.5: Completion doc + CHANGELOG entry + tag

**Files:**
- Create: `docs/sub-11-entra-id-migration-completion.md`
- Modify: `CHANGELOG.md`
- (Final step): `git tag entra-id-v1.0.0`

- [ ] **Step 1: Create `docs/sub-11-entra-id-migration-completion.md`**

Mirror the structure of `docs/sub-10c-production-infra-completion.md`. Cover: goals, what was delivered (per phase), migration counts (test deltas, file deltas, commit count), deferred items (Sub-11d email-sender, anonymous self-service), known limitations (multi-tenant CA boundary, partner-tenant branding), reference links to ADRs + runbooks + plan files.

Target length: 250-400 lines, similar to Sub-10c's completion doc.

- [ ] **Step 2: Update `CHANGELOG.md`**

Insert a new `[entra-id-v1.0.0] — 2026-05-05` block at the top, BEFORE the existing `[infra-v1.0.0]` entry. Mirror the Sub-10c style: brief heading, Added/Changed/Deleted/Architecture-decisions sections.

Key bullet points:
- **Added**: Microsoft.Identity.Web 3.5.0 + Microsoft.Graph 5.65.0 + Azure.Identity 1.13.2 + WireMock.Net 1.7.0 in Directory.Packages.props; EntraIdGraphClientFactory + EntraIdRegistrationService + RegistrationContracts; EntraIdUserResolver + EntraIdIssuerValidator + EntraIdOptions; AddEntraIdObjectIdToUser DB migration (additive nullable + filtered unique index); infra/entra/ scripts (apply-app-registration.ps1 + Configure-Branding.ps1) + manifest + branding placeholders + operator README; ADR-0058, 0059, 0060; entra-id-cutover.md + entra-id-troubleshooting.md runbooks; ENTRA_* + HOSTNAME_* env-keys across 6 env-file templates.
- **Changed**: Frontend OIDC config (cce-oidc.config drops adfs-compat scope); both env.json files point at login.microsoftonline.com/common/v2.0; register.page becomes an info-page with auth.signIn() CTA; permissions.yaml renamed to cce-* role values; PermissionsGenerator.KnownRoles + ToRoleMemberName helper; RoleToPermissionClaimsTransformer reads `roles` claim with cce-* values.
- **Deleted**: infra/keycloak/ (apply-realm.ps1 + realm JSON); custom BFF cluster (BffSessionMiddleware/Cookie/Refresher/Session/Options/TokenResponse/AuthEndpoints — 7 files + 5 BffSessionMiddlewareTests); KeycloakLdapFixture + KeycloakLdapFederationTests (3 tests); Testcontainers.Keycloak package reference; KEYCLOAK_* + LDAP_* env-keys; ad-federation.md runbook.
- **Architecture decisions**: ADR-0058 — Entra ID multi-tenant + Graph writes (supersedes ADR-0055); ADR-0059 — App roles vs security groups; ADR-0060 — Conditional Access for MFA. ADR-0055 status → Superseded.

Net counts to call out:
- Backend test counts: Domain 290 (unchanged) / Application 439 (unchanged) / Architecture 12 (unchanged) / Infrastructure 75 → 84 (+9 net: +5 IssuerValidator + 3 ObjectIdLazyResolution + 1 fixture-smoke + 3 EntraIdRegistration − 3 KeycloakLdap = +9). IntegrationTests transformer: 5 → 5 (same count, all retargeted; replaced 2 legacy with 2 new).
- Frontend tests unchanged: 720 (web-portal 502 + admin-cms 218).
- Net file count delta: ~+30 created, ~10 deleted.
- Commits: ~25-30 across all 5 phases.

- [ ] **Step 3: Final test sweep** before tagging

```bash
cd /Users/m/CCE/backend && dotnet build --nologo --verbosity minimal | tail -5 && \
  dotnet test tests/CCE.Domain.Tests/        --no-build --nologo --verbosity minimal | tail -3 && \
  dotnet test tests/CCE.Application.Tests/   --no-build --nologo --verbosity minimal | tail -3 && \
  dotnet test tests/CCE.ArchitectureTests/   --no-build --nologo --verbosity minimal | tail -3 && \
  dotnet test tests/CCE.Infrastructure.Tests/ --no-build --nologo --verbosity minimal | tail -3
```

Expected: build clean (0 warnings, 0 errors); Domain 290 / Application 439 / Architecture 12 / Infrastructure 84 — all green (1 pre-existing skip).

- [ ] **Step 4: Commit completion doc + CHANGELOG**

```bash
git add docs/sub-11-entra-id-migration-completion.md CHANGELOG.md
git commit -m "$(cat <<'EOF'
docs: Sub-11 — Entra ID migration completion doc + CHANGELOG entry

docs/sub-11-entra-id-migration-completion.md: comprehensive close-out
covering all 5 Sub-11 phases, deferred items (Sub-11d), known
multi-tenant limitations, and ADR / runbook / plan-file references.

CHANGELOG.md: [entra-id-v1.0.0] — 2026-05-05 entry inserted before
[infra-v1.0.0]. Lists added (Microsoft.Identity.Web + Graph + Azure.Identity +
WireMock + EntraId services + DB migration + infra scripts + ADRs +
runbooks + env-keys), changed (frontend OIDC config + permissions.yaml
+ transformer), and deleted (infra/keycloak/ + custom BFF cluster +
Keycloak tests + Testcontainers.Keycloak + KEYCLOAK_*/LDAP_* env-keys
+ ad-federation.md). Records architectural decisions ADR-0058/0059/0060
and the supersede on ADR-0055.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 5: Tag `entra-id-v1.0.0`**

```bash
cd /Users/m/CCE && git tag -a entra-id-v1.0.0 -m "Sub-11 — Entra ID migration complete

Replaces Keycloak with multi-tenant Microsoft Entra ID across the entire
CCE platform. Synced from on-prem AD via Entra ID Connect. CCE backend
writes to Entra ID via Microsoft Graph for self-service registration.
Conditional Access enforces MFA at the Entra ID side; CCE backend stays
MFA-agnostic.

5 phases (Backend foundation → Graph registration → App registration +
branding → Frontend OIDC swap + transformer rewrite → Cutover + Keycloak
deletion). Backend test counts: Domain 290 / Application 439 / Architecture
12 / Infrastructure 84. Frontend test counts: 720. ADR-0058 / 0059 / 0060
landed; ADR-0055 superseded."
```

```bash
git tag --list "entra-id-*"
```

Expected: `entra-id-v1.0.0` listed.

(Do NOT push the tag in this task — tag-pushing is operator-driven, gated by ops review.)

---

## Phase 04 close-out

After Task 4.5 commits cleanly + the tag is created:

- [ ] **Update master plan** to mark Phase 04 DONE.
- [ ] **No Phase 05** — Sub-11 ends here.

## Phase 04 close-out — DONE 2026-05-05

**Phase 04 done when:**
- 4 task commits + 1 close-out commit landed on `main`, each green.
- All Bff* files deleted from `CCE.Api.Common/Auth/`; `BffRegistration.cs` simplified to M.I.W-only.
- `BffSessionMiddlewareTests.cs` deleted (5 tests removed).
- `KeycloakLdapFixture.cs` + `KeycloakLdapFederationTests.cs` deleted (3 tests removed).
- `Testcontainers.Keycloak` removed from `Directory.Packages.props` + Infrastructure test csproj.
- `infra/keycloak/` deleted.
- `RoleToPermissionClaimsTransformer.cs` reads only `roles` claim with `cce-*` values; legacy mappings removed.
- `RoleToPermissionClaimsTransformerTests.cs` simplified to 5 tests (was 7); legacy `groups`-claim + slash-prefix tests deleted; existing tests retargeted to `roles` claim.
- All 6 env-file `.example` templates have `KEYCLOAK_*` + `LDAP_*` blocks deleted.
- `docs/runbooks/ad-federation.md` deleted.
- `docs/runbooks/entra-id-cutover.md` (12-step procedure) + `docs/runbooks/entra-id-troubleshooting.md` shipped.
- `docs/sub-11-entra-id-migration-completion.md` shipped.
- `CHANGELOG.md` `[entra-id-v1.0.0] — 2026-05-05` entry shipped.
- Tag `entra-id-v1.0.0` exists locally.
- Backend test counts: Domain 290 / Application 439 / Architecture 12 / Infrastructure 84.
- IntegrationTests test counts: -8 net (5 BffSessionMiddleware + 2 transformer-legacy + 1 slash-prefix; new transformer count is 5).
- Frontend test counts: 720 (web-portal 502 + admin-cms 218) — unchanged.
- **Sub-11 ships.**

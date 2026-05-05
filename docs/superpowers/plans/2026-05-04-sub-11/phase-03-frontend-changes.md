# Sub-11 Phase 03 — Frontend OIDC swap + permission-transformer rewrite

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire the frontends (web-portal + admin-cms + their e2e suites) to consume Entra ID instead of Keycloak, and rewrite the backend permission-mapping transformer to read the `roles` claim with `cce-admin`-style names. Lands the 2 `RoleClaimMappingTests` that Phase 00 deferred.

**Architecture:** Frontend code is already largely IdP-agnostic — the BFF cookie pattern hides the IdP behind a same-origin `/auth/login` round-trip. Phase 03 changes are mostly **config + comment cleanup**: OIDC scope list (drop Keycloak's `adfs-compat`), `env.json` URLs/clientIds, register-page UX (the underlying endpoint is now admin-only, no longer anonymous self-service). Backend transformer reads `roles` instead of `groups` and switches on the new role names; `permissions.yaml` + `PermissionsGenerator.KnownRoles` rename to match.

**Tech Stack:** Angular 19 · `angular-auth-oidc-client` 18 · Playwright e2e · existing CCE.Domain.SourceGenerators · existing `RoleToPermissionClaimsTransformer`

**Test count delta:** Backend Infrastructure 87 → 87 (unchanged — transformer tests live in `CCE.Api.IntegrationTests`, not Infrastructure). `CCE.Api.IntegrationTests` gains 2 RoleClaimMapping tests (the Phase 00 deferral). Frontend 502 → 502 (no net new tests; existing tests updated).

---

## Phase 03 deliverables (5 tasks)

| # | Layer | Files | Outcome |
|---|---|---|---|
| 3.1 | Frontend config | `cce-oidc.config.ts`, `env.types.ts`, 2× `env.json` | Entra ID scopes, comments, runtime config values |
| 3.2 | Frontend code | `auth.guard.ts`, `auth.interceptor.ts` × 2, `correlation-id.interceptor.ts` × 2, `register.page.ts`, `sign-in-cta.component.ts` | Keycloak comment cleanup; register page becomes an info page (admin-only registration) |
| 3.3 | Frontend e2e | `account.spec.ts` (web-portal), `smoke.spec.ts` × 2, `layout.spec.ts` × 2 | Stub BFF `/auth/login` redirect; assert against Entra ID URL pattern instead of Keycloak |
| 3.4 | Backend | `RoleToPermissionClaimsTransformer.cs`, `permissions.yaml`, `PermissionsGenerator.cs`, `RoleToPermissionClaimsTransformerTests.cs` | Read `roles` claim; rename roles to `cce-*`; lands 2 deferred RoleClaimMapping tests |
| 3.5 | Verification | Full backend + frontend test sweep | Confirm no regressions; test counts as expected |

---

## Role-rename mapping (locked-in here for the Phase 03 executor)

ADR-0059 specified the 5 Entra ID app roles. Phase 03 maps the 6 legacy Keycloak role names to the new 5 + Anonymous. **`StateRepresentative`-specific permissions merge into `cce-editor`** (content authoring is broad enough to include country-resource submission and country-profile updates).

| Legacy role (Keycloak) | New role (Entra ID app role) | Mapping note |
|---|---|---|
| `SuperAdmin` | `cce-admin` | direct rename |
| `ContentManager` | `cce-editor` | direct rename |
| `StateRepresentative` | `cce-editor` | merged — StateRep's `Resource.Country.Submit` + slice of `Country.Profile.Update` rolls into editor |
| `CommunityExpert` | `cce-expert` | direct rename |
| `RegisteredUser` | `cce-user` | direct rename |
| (no Keycloak equivalent) | `cce-reviewer` | NEW — Phase 03 grants it `Community.Expert.ApproveRequest` + read-only on most resources |
| `Anonymous` | `Anonymous` | unchanged — represents absence-of-token, not an Entra ID role |

`cce-reviewer` is a net-new role from the ADR-0059 set. Phase 03 assigns it the review queue (`Community.Expert.ApproveRequest`) plus read-only on content (`User.Read`, `KnowledgeMap.View`, etc.) — **not** content-authoring perms (those stay on `cce-editor` and `cce-admin`).

---

## Global conventions (Phase 03)

- All commits use Conventional Commits (`feat`, `refactor`, `docs`, `test`).
- All commits include the `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` trailer.
- `dotnet test` invocations run from `/Users/m/CCE/backend`.
- `pnpm nx` invocations run from `/Users/m/CCE/frontend`.
- Each task ends with green build + targeted test run + commit.

---

## Task 3.1: Frontend OIDC config + runtime env files

**Files:**
- Modify: `frontend/libs/auth/src/lib/cce-oidc.config.ts`
- Modify: `frontend/libs/contracts/src/lib/env.types.ts`
- Modify: `frontend/apps/web-portal/src/assets/env.json`
- Modify: `frontend/apps/admin-cms/src/assets/env.json`

- [ ] **Step 1: Update `cce-oidc.config.ts`** — drop the Keycloak-only `adfs-compat` scope; update comments to reference Entra ID

```typescript
import type { OpenIdConfiguration } from 'angular-auth-oidc-client';

export interface CceAuthEnv {
  authority: string;
  clientId: string;
  redirectUri: string;
  postLogoutRedirectUri: string;
}

/**
 * Build an angular-auth-oidc-client configuration for one of the CCE
 * Entra ID app registrations. Apps call this from their bootstrap with
 * values pulled from /assets/env.json so the same image deploys to
 * dev/staging/prod by swapping the runtime config file.
 *
 * Multi-tenant Entra ID — `authority` points at
 * `https://login.microsoftonline.com/<tenant>/v2.0` (or `/common` for
 * any-tenant); the BFF's IssuerValidator accepts any tenant matching the
 * canonical shape (see EntraIdIssuerValidator).
 */
export function buildCceOidcConfig(env: CceAuthEnv): OpenIdConfiguration {
  return {
    authority: env.authority,
    redirectUrl: env.redirectUri,
    postLogoutRedirectUri: env.postLogoutRedirectUri,
    clientId: env.clientId,
    // Entra ID standard scopes; adfs-compat (Keycloak-only) removed in Sub-11.
    scope: 'openid profile email offline_access',
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    renewTimeBeforeTokenExpiresInSeconds: 30,
    usePushedAuthorisationRequests: false,
    triggerAuthorizationResultEvent: true,
    logLevel: 0,
  } as OpenIdConfiguration;
}
```

- [ ] **Step 2: Update `env.types.ts`** — refresh the Keycloak references in the doc comments

```typescript
/**
 * Runtime environment loaded from /assets/env.json at app bootstrap.
 * Same shape across web-portal and admin-cms; the values differ.
 */
export interface CceEnv {
  /** Logical environment name — "development" | "staging" | "production". */
  readonly environment: string;

  /** Backend API base URL — External API for web-portal, Internal API for admin-cms. */
  readonly apiBaseUrl: string;

  /** OIDC authority — full Entra ID v2.0 endpoint, e.g.
   *  `https://login.microsoftonline.com/common/v2.0` (multi-tenant) or
   *  `https://login.microsoftonline.com/<tenant-guid>/v2.0` (single-tenant override). */
  readonly oidcAuthority: string;

  /** OIDC client ID — the Entra ID app registration's Application (client) ID. Same value
   *  for both web-portal and admin-cms (they share one app registration with multiple
   *  redirect URIs; see infra/entra/app-registration-manifest.json). */
  readonly oidcClientId: string;

  /** Sentry DSN; empty string disables Sentry. */
  readonly sentryDsn: string;
}
```

- [ ] **Step 3: Update both `env.json` files** with Entra ID dev defaults

`frontend/apps/web-portal/src/assets/env.json`:
```json
{
  "environment": "development",
  "apiBaseUrl": "http://localhost:5001",
  "oidcAuthority": "https://login.microsoftonline.com/common/v2.0",
  "oidcClientId": "00000000-0000-0000-0000-000000000000",
  "sentryDsn": ""
}
```

`frontend/apps/admin-cms/src/assets/env.json`:
```json
{
  "environment": "development",
  "apiBaseUrl": "http://localhost:5002",
  "oidcAuthority": "https://login.microsoftonline.com/common/v2.0",
  "oidcClientId": "00000000-0000-0000-0000-000000000000",
  "sentryDsn": ""
}
```

(Both apps share the same Entra ID `oidcClientId` — single app registration, multiple redirect URIs per Phase 02. The `00000000-...` placeholder gets overridden at deploy time.)

- [ ] **Step 4: Verify the auth lib builds**

```bash
cd /Users/m/CCE/frontend && pnpm nx build auth --skip-nx-cache 2>&1 | tail -10
```

Expected: build succeeds.

- [ ] **Step 5: Run the auth lib tests**

```bash
cd /Users/m/CCE/frontend && pnpm nx test auth --skip-nx-cache 2>&1 | tail -10
```

Expected: all tests pass. If any reference the dropped `adfs-compat` scope, update them.

- [ ] **Step 6: Commit**

```bash
git add frontend/libs/auth/src/lib/cce-oidc.config.ts \
        frontend/libs/contracts/src/lib/env.types.ts \
        frontend/apps/web-portal/src/assets/env.json \
        frontend/apps/admin-cms/src/assets/env.json
git commit -m "$(cat <<'EOF'
feat(auth): swap OIDC config from Keycloak to multi-tenant Entra ID

cce-oidc.config.ts: drops the Keycloak-only `adfs-compat` scope from the
default scope list. Standard Entra ID scopes (openid profile email
offline_access) are sufficient for both apps. Updates comments to
reference Entra ID + EntraIdIssuerValidator.

env.types.ts: refreshes the doc comments on oidcAuthority and oidcClientId
to describe the Entra ID endpoint shape (login.microsoftonline.com/<tenant>/v2.0)
and the single-app-registration model (web-portal and admin-cms share one
clientId; redirect URIs distinguish them — see Phase 02 manifest).

Both apps' /assets/env.json now point at the multi-tenant /common
endpoint with a placeholder clientId. Real values get baked in at
deploy time per env.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3.2: Frontend code — Keycloak comment cleanup + register-page rework

**Files:**
- Modify: `frontend/apps/web-portal/src/app/core/auth/auth.guard.ts`
- Modify: `frontend/apps/web-portal/src/app/core/http/correlation-id.interceptor.ts` (if it has Keycloak comments — check first)
- Modify: `frontend/apps/admin-cms/src/app/core/http/correlation-id.interceptor.ts` (same)
- Modify: `frontend/apps/admin-cms/src/app/core/http/auth.interceptor.ts`
- Modify: `frontend/apps/web-portal/src/app/core/http/auth.interceptor.ts` (if it exists; check)
- Modify: `frontend/apps/web-portal/src/app/features/account/register.page.ts` + `register.page.spec.ts`
- Modify: `frontend/apps/web-portal/src/app/features/community/sign-in-cta.component.ts`

- [ ] **Step 1: Inventory which files actually mention Keycloak**

```bash
grep -l -r "Keycloak\|keycloak" /Users/m/CCE/frontend/apps/web-portal/src /Users/m/CCE/frontend/apps/admin-cms/src --include="*.ts"
```

Run this first — only update files that actually mention Keycloak. The plan-listed files are the suspects; the grep is the source of truth.

- [ ] **Step 2: Update `auth.guard.ts`** — replace `Keycloak` → `Entra ID` in the JSDoc

```typescript
/**
 * Production guard for routes that require an authenticated user.
 *
 * Behavior:
 * - Authenticated -> true.
 * - Not authenticated AND we have not yet attempted a refresh -> awaits
 *   `auth.refresh()` once, then re-checks `isAuthenticated()`. This
 *   covers the cold-start case where the BFF cookie is valid but
 *   `/api/me` has not yet been called this session.
 * - Not authenticated AND refresh has already been attempted -> calls
 *   `auth.signIn(state.url)` so the BFF round-trips through Entra ID
 *   and brings the user back to the originally requested URL, then
 *   returns false so the route doesn't render.
 */
```

(Only the JSDoc changes — code is unchanged.)

- [ ] **Step 3: Update `auth.interceptor.ts`** files

The web-portal version may not exist (admin-cms has it for sure). For each file, swap `Keycloak's discovery endpoint` → `Entra ID's discovery endpoint` in the JSDoc. Code itself is unchanged — `isInternalUrl` already excludes any third-party origin.

- [ ] **Step 4: Rewrite `register.page.ts`** — Phase 01 made `/api/users/register` admin-only POST; the public landing page becomes an info page directing users to contact an admin

```typescript
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';

/**
 * Public landing page for the /register route.
 *
 * Sub-11 changed CCE's IdP from Keycloak (with hosted self-service registration)
 * to multi-tenant Entra ID. Anonymous self-service registration is deferred to
 * Sub-11d (needs an IEmailSender abstraction to deliver temp passwords). For
 * now, this page tells users how to get an account:
 *
 *   - Internal users (cce.local): synced via Entra ID Connect — already have
 *     accounts, click sign-in.
 *   - Partner-tenant users: sign in with their existing Entra ID tenant.
 *   - External users without an Entra ID account: contact a CCE admin.
 *
 * `POST /api/users/register` is now an admin-only Graph user-create call
 * (Sub-11 Phase 01); the public flow no longer hits it.
 */
@Component({
  selector: 'cce-register',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, TranslateModule],
  template: `
    <section class="cce-register">
      <h1 class="cce-register__title">{{ 'account.register.title' | translate }}</h1>

      @if (isAuthenticated()) {
        <p class="cce-register__body">{{ 'account.register.alreadySignedIn' | translate }}</p>
        <a mat-flat-button color="primary" routerLink="/me/profile">
          {{ 'account.register.openProfile' | translate }}
        </a>
      } @else {
        <p class="cce-register__body">{{ 'account.register.body' | translate }}</p>
        <p class="cce-register__hint">{{ 'account.register.contactHint' | translate }}</p>
        <button
          type="button"
          mat-flat-button
          color="primary"
          (click)="signIn()"
        >
          {{ 'account.register.signInButton' | translate }}
        </button>
      }
    </section>
  `,
  styleUrl: './register.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  readonly isAuthenticated = this.auth.isAuthenticated;

  signIn(): void {
    this.auth.signIn('/me/profile');
  }
}
```

Update the i18n keys if they need adjustment (`account.register.contactHint` is new; `account.register.continueButton` becomes `account.register.signInButton`). Find the i18n source files:

```bash
find /Users/m/CCE/frontend -name "*.i18n.json" -o -name "en.json" -o -name "ar.json" 2>/dev/null | grep -v node_modules | head -10
```

Update the `account.register.*` keys in both `en` and `ar` translations. New strings (write the values):
- `account.register.continueButton` → leave alone if any old code still references it; otherwise rename to `signInButton`
- `account.register.signInButton`: `"Sign in"` (en), `"تسجيل الدخول"` (ar)
- `account.register.contactHint`: `"Don't have an account? Contact your CCE administrator to get one."` (en), Arabic equivalent

- [ ] **Step 5: Update `register.page.spec.ts`** — assert the new behavior

The old test asserted `window.location.assign` was called with `/api/users/register`. The new test should assert `auth.signIn(...)` is called instead. Read the existing spec, locate the assertion, swap.

- [ ] **Step 6: Update `sign-in-cta.component.ts`** — JSDoc only

Replace `BFF round-trip through Keycloak` → `BFF round-trip through Entra ID`. No code change.

- [ ] **Step 7: Run the affected app + lib test suites**

```bash
cd /Users/m/CCE/frontend && pnpm nx test web-portal --skip-nx-cache 2>&1 | tail -10
cd /Users/m/CCE/frontend && pnpm nx test admin-cms --skip-nx-cache 2>&1 | tail -10
```

Expected: all tests pass. If `register.page.spec.ts` asserts on the old behavior, update it.

- [ ] **Step 8: Commit**

```bash
git add frontend/apps/web-portal/src/app/core/auth/auth.guard.ts \
        frontend/apps/web-portal/src/app/features/account/register.page.ts \
        frontend/apps/web-portal/src/app/features/account/register.page.spec.ts \
        frontend/apps/web-portal/src/app/features/community/sign-in-cta.component.ts \
        frontend/apps/admin-cms/src/app/core/http/auth.interceptor.ts
# Add any other files the inventory grep surfaced.
git commit -m "$(cat <<'EOF'
refactor(frontend): Sub-11 Keycloak → Entra ID copy; register page UX

Sweeps "Keycloak" → "Entra ID" in JSDoc + comments across auth.guard,
auth.interceptor, sign-in-cta. Code unchanged — these files were already
IdP-agnostic via the BFF cookie pattern.

register.page rewires from "click → 302-redirect to Keycloak's hosted
registration page" to "click → BFF /signin → Entra ID sign-in". Anonymous
self-service registration is deferred to Sub-11d (needs IEmailSender);
the page now explains the three account-acquisition paths (cce.local
sync, partner-tenant sign-in, contact admin) and offers a Sign In CTA.

i18n keys renamed: account.register.continueButton →
account.register.signInButton; account.register.contactHint added.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3.3: e2e specs — Entra ID URL pattern + register-page expectation update

**Files:**
- Modify: `frontend/apps/web-portal-e2e/src/account.spec.ts`
- Modify: `frontend/apps/web-portal-e2e/src/smoke.spec.ts` (if it asserts on Keycloak URL)
- Modify: `frontend/apps/web-portal-e2e/src/layout.spec.ts` (if it asserts on Keycloak URL)
- Modify: `frontend/apps/admin-cms-e2e/src/smoke.spec.ts` (if it asserts on Keycloak URL)
- Modify: `frontend/apps/admin-cms-e2e/src/layout.spec.ts` (if it asserts on Keycloak URL)

- [ ] **Step 1: Inventory e2e specs that touch Keycloak / register**

```bash
grep -l "keycloak\|/register\|continue.*sign-up" /Users/m/CCE/frontend/apps/*-e2e/src/*.spec.ts
```

- [ ] **Step 2: Update `account.spec.ts` `/register attaches the register page` test**

The button text changed from `continue to sign-up` to `Sign in`. Update the regex:

```typescript
test('/register attaches the register page', async ({ page }) => {
  await page.goto('/register');
  await expect(page.locator('cce-register')).toBeAttached({ timeout: 10_000 });
  await expect(
    page.getByRole('button', { name: /sign in|تسجيل الدخول/i }),
  ).toBeVisible();
});
```

- [ ] **Step 3: For each `smoke.spec.ts` / `layout.spec.ts`** — if they assert against `keycloak` or `/realms/`, update them to assert against `login.microsoftonline.com` (or simply the BFF `/auth/login` redirect which is same-origin).

The cleanest approach: assert only that the BFF `/auth/login` endpoint is hit. The downstream Entra ID redirect lives outside the test boundary.

- [ ] **Step 4: Run the e2e suites that don't require a live backend**

```bash
cd /Users/m/CCE/frontend && pnpm nx e2e web-portal-e2e --skip-nx-cache 2>&1 | tail -15
cd /Users/m/CCE/frontend && pnpm nx e2e admin-cms-e2e --skip-nx-cache 2>&1 | tail -15
```

Expected: all green. Tests that require a real BFF + Entra ID are out-of-scope for CI; document any such skips.

- [ ] **Step 5: Commit**

```bash
git add frontend/apps/web-portal-e2e/ frontend/apps/admin-cms-e2e/
git commit -m "$(cat <<'EOF'
test(e2e): Sub-11 register-page button rename + Entra ID URL pattern

account.spec: /register button changed from "continue to sign-up" to
"Sign in" (Phase 03 register-page rewrite). Asserts against the new
i18n strings.

smoke + layout specs: assertions against Keycloak realm URLs swap to
asserting that /auth/login is hit (same-origin BFF endpoint). The
downstream Entra ID redirect lives outside the test boundary.

No new e2e test files. 502-test frontend total unchanged.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3.4: Backend — `RoleToPermissionClaimsTransformer` + `permissions.yaml` + generator + 2 RoleClaimMapping tests

**Files:**
- Modify: `backend/src/CCE.Api.Common/Authorization/RoleToPermissionClaimsTransformer.cs`
- Modify: `backend/permissions.yaml`
- Modify: `backend/src/CCE.Domain.SourceGenerators/PermissionsGenerator.cs`
- Modify: `backend/tests/CCE.Api.IntegrationTests/Authorization/RoleToPermissionClaimsTransformerTests.cs`
- Create: nothing (the 2 new RoleClaimMapping tests land in the existing file)

This is the load-bearing task. Get the transformer right; everything downstream depends on it.

- [ ] **Step 1: Write the 2 deferred RoleClaimMapping tests first (TDD)**

Open `backend/tests/CCE.Api.IntegrationTests/Authorization/RoleToPermissionClaimsTransformerTests.cs`, read it, then add 2 new test cases AT THE BOTTOM (don't change existing tests yet — they assert against the legacy role names + `groups` claim and will need updating in Step 4):

```csharp
[Fact]
public async Task TransformAsync_ReadsRolesClaim_AndExpandsCceAdminToFullPermissionSet()
{
    var identity = new ClaimsIdentity(
        new[] { new Claim("roles", "cce-admin") },
        authenticationType: "test");
    var principal = new ClaimsPrincipal(identity);
    var transformer = new RoleToPermissionClaimsTransformer();

    var result = await transformer.TransformAsync(principal);

    var permissions = result.FindAll("groups").Select(c => c.Value).ToList();
    permissions.Should().Contain("System.Health.Read");
    permissions.Should().Contain("User.Create");
    permissions.Should().Contain("Role.Assign");
}

[Fact]
public async Task TransformAsync_RolesClaim_CceUserGetsCommunityWritePermissions()
{
    var identity = new ClaimsIdentity(
        new[] { new Claim("roles", "cce-user") },
        authenticationType: "test");
    var principal = new ClaimsPrincipal(identity);
    var transformer = new RoleToPermissionClaimsTransformer();

    var result = await transformer.TransformAsync(principal);

    var permissions = result.FindAll("groups").Select(c => c.Value).ToList();
    permissions.Should().Contain("Community.Post.Create");
    permissions.Should().Contain("Community.Post.Reply");
    permissions.Should().NotContain("Role.Assign"); // admin-only
}
```

- [ ] **Step 2: Run the new tests — expect them to fail** (transformer still reads `groups` claim with legacy role names)

```bash
cd /Users/m/CCE/backend && dotnet test tests/CCE.Api.IntegrationTests/ --filter "FullyQualifiedName~RoleToPermissionClaimsTransformerTests.TransformAsync_ReadsRolesClaim" --nologo --verbosity minimal 2>&1 | tail -10
```

Expected: 2 failures.

- [ ] **Step 3: Update `permissions.yaml`** with new role names

Run a search-and-replace across the file. Mapping:
- `SuperAdmin` → `cce-admin`
- `ContentManager` → `cce-editor`
- `StateRepresentative` → `cce-editor` (merged)
- `CommunityExpert` → `cce-expert`
- `RegisteredUser` → `cce-user`
- `Anonymous` → unchanged

After the substitution, deduplicate any role lists where the merge created duplicates (e.g., `[cce-editor, cce-editor]` → `[cce-editor]`).

Add `cce-reviewer` to selected roles. Specifically:
- `Community.Expert.ApproveRequest`: append `cce-reviewer`
- `User.Read`: append `cce-reviewer`
- `KnowledgeMap.View`: append `cce-reviewer`
- `Survey.Submit`: append `cce-reviewer`

Update the header comment block:

```yaml
# Known roles (defined in PermissionsGenerator.KnownRoles):
#   cce-admin, cce-editor, cce-reviewer, cce-expert, cce-user, Anonymous
#   These match the appRoles[].value entries in
#   infra/entra/app-registration-manifest.json (Sub-11 Phase 02).
```

- [ ] **Step 4: Update `PermissionsGenerator.KnownRoles`**

Open `backend/src/CCE.Domain.SourceGenerators/PermissionsGenerator.cs`, find the `KnownRoles` array (line ~30), replace:

```csharp
private static readonly string[] KnownRoles =
{
    "cce-admin",
    "cce-editor",
    "cce-reviewer",
    "cce-expert",
    "cce-user",
    "Anonymous",
};
```

The generator emits `RolePermissionMap.<role-name-as-PascalCase>`. With dashes in the role names (`cce-admin`), the generated property name will be invalid C#. **Check the generator's symbol-rendering**: it likely uses `KnownRoles[r]` directly as a property name. If so, the generator needs an extra step to convert `cce-admin` → `CceAdmin` for the property name while keeping `cce-admin` as the value. Read `GenerateSource` in `PermissionsGenerator.cs` and add the conversion.

If the generator does:
```csharp
sb.AppendLine($"public static readonly string[] {role} = ...");
```
…it fails on `cce-admin`. Wrap with a `ToPascalCase` helper:

```csharp
private static string ToPascalCase(string roleValue)
    => string.Concat(roleValue.Split('-').Select(s => s.Length == 0 ? "" : char.ToUpperInvariant(s[0]) + s.Substring(1)));
```

…and use it everywhere the role becomes a C# identifier (the property names + the switch cases that the transformer reads from). The role *value* (the string compared against the `roles` claim) stays as `cce-admin`.

- [ ] **Step 5: Update the transformer** — read `roles` instead of `groups`, switch on new names

```csharp
using System.Security.Claims;
using CCE.Domain;
using Microsoft.AspNetCore.Authentication;

namespace CCE.Api.Common.Authorization;

/// <summary>
/// Sub-11 — expands the role-name <c>roles</c> claims (Entra ID app-role values,
/// e.g. <c>cce-admin</c>) on an authenticated principal into permission-name
/// <c>groups</c> claims (e.g., <c>User.Read</c>) so the per-permission
/// authorization policies registered by <c>AddCcePermissionPolicies</c> pass.
/// Idempotent — recognises an already-transformed principal via a sentinel
/// claim and short-circuits to avoid re-flattening on every authorization callback.
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

        // Snapshot the roles already present so we don't mutate while iterating.
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

    private static IReadOnlyList<string> ResolveRolePermissions(string roleValue) => roleValue switch
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

- [ ] **Step 6: Update existing transformer tests** — rename role-name assertions

The existing tests in `RoleToPermissionClaimsTransformerTests.cs` use `groups` claim with `SuperAdmin` etc. Two paths:
- (a) Update each test to use `roles` claim with new role names. Cleanest.
- (b) Delete the old tests; the 2 new ones cover the new behavior.

Go with (a) — the test names + assertion targets carry institutional knowledge. Substitute `groups` → `roles` and `SuperAdmin` → `cce-admin` etc. in each existing test.

- [ ] **Step 7: Build + targeted test run**

```bash
cd /Users/m/CCE/backend && dotnet build --nologo --verbosity minimal 2>&1 | tail -10
```

Expected: clean build. The source generator should emit `RolePermissionMap.CceAdmin` etc. — if it fails with "invalid identifier" the `ToPascalCase` helper wasn't applied everywhere.

```bash
cd /Users/m/CCE/backend && dotnet test tests/CCE.Api.IntegrationTests/ --filter "FullyQualifiedName~RoleToPermissionClaimsTransformerTests" --nologo --verbosity minimal 2>&1 | tail -10
```

Expected: all transformer tests pass (existing-renamed + 2 new).

- [ ] **Step 8: Run Domain + Application + Architecture suites to catch regressions from the generated `RolePermissionMap` shape**

```bash
cd /Users/m/CCE/backend && \
  dotnet test tests/CCE.Domain.Tests/ --no-build --nologo --verbosity minimal && \
  dotnet test tests/CCE.Application.Tests/ --no-build --nologo --verbosity minimal && \
  dotnet test tests/CCE.ArchitectureTests/ --no-build --nologo --verbosity minimal
```

Expected: Domain 290 / Application 439 / Architecture 12 all green.

- [ ] **Step 9: Commit**

```bash
git add backend/src/CCE.Api.Common/Authorization/RoleToPermissionClaimsTransformer.cs \
        backend/permissions.yaml \
        backend/src/CCE.Domain.SourceGenerators/PermissionsGenerator.cs \
        backend/tests/CCE.Api.IntegrationTests/Authorization/RoleToPermissionClaimsTransformerTests.cs
git commit -m "$(cat <<'EOF'
refactor(api-common): RoleToPermissionClaimsTransformer reads `roles` claim

Sub-11 Phase 03 swaps the transformer from Keycloak `groups` claim with
SuperAdmin-style role names to Entra ID `roles` claim with cce-admin-style
role values. The output `groups` claim shape is unchanged (still emits
permission names like User.Read), so existing per-permission policies
keep working.

permissions.yaml renames the 6 legacy role names to the 5 Entra ID
app-role values + Anonymous, merging StateRepresentative into cce-editor
(content authoring is broad enough). Adds cce-reviewer as a net-new role
with read-only on content + ApproveRequest queue access.

PermissionsGenerator.KnownRoles + ToPascalCase helper handle the
dash-to-PascalCase conversion so the generated RolePermissionMap.CceAdmin
etc. are valid C# identifiers.

Lands the 2 RoleClaimMapping tests deferred from Phase 00. Existing
transformer tests updated to use the new role names + claim type.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3.5: Full backend + frontend test sweep

**Files:** none modified — verification only.

- [ ] **Step 1: Backend full sweep**

```bash
cd /Users/m/CCE/backend && \
  dotnet build --nologo --verbosity minimal | tail -3 && \
  dotnet test tests/CCE.Domain.Tests/        --no-build --nologo --verbosity minimal | tail -3 && \
  dotnet test tests/CCE.Application.Tests/   --no-build --nologo --verbosity minimal | tail -3 && \
  dotnet test tests/CCE.ArchitectureTests/   --no-build --nologo --verbosity minimal | tail -3 && \
  dotnet test tests/CCE.Infrastructure.Tests/ --no-build --nologo --verbosity minimal | tail -3
```

Expected: build clean (0 warnings, 0 errors); Domain 290 / Application 439 / Architecture 12 / Infrastructure 87 — all green.

```bash
cd /Users/m/CCE/backend && dotnet test tests/CCE.Api.IntegrationTests/ --filter "FullyQualifiedName~RoleToPermissionClaimsTransformer" --nologo --verbosity minimal | tail -5
```

Expected: existing transformer tests + 2 new RoleClaimMapping tests all pass.

- [ ] **Step 2: Frontend full sweep**

```bash
cd /Users/m/CCE/frontend && pnpm nx run-many -t test --skip-nx-cache 2>&1 | tail -20
```

Expected: 502 frontend tests pass, no regressions. (Test count unchanged in Phase 03 — register.page.spec is updated, not added.)

- [ ] **Step 3: If anything fails, fix forward**

Common failure modes:
- e2e specs that hardcoded `cce-web-portal` clientId in assertions → swap to placeholder GUID match.
- Tests that asserted `Keycloak` somewhere in error messages → update.

- [ ] **Step 4: No commit needed for Step 3 if no fixes** — Step 3 is a verification gate, not a deliverable.

---

## Phase 03 close-out

After Task 3.4 commits cleanly + Task 3.5 verifies green:

- [ ] **Run the final verification:**
  ```bash
  cd /Users/m/CCE/backend && dotnet build && \
    dotnet test tests/CCE.Domain.Tests/ tests/CCE.Application.Tests/ \
                tests/CCE.ArchitectureTests/ tests/CCE.Infrastructure.Tests/ --nologo
  ```
  Expected: backend builds clean (0 warnings, 0 errors); Domain 290, Application 439, Architecture 12, Infrastructure 87 — all green.

- [ ] **Update master plan + Phase 03 doc** to mark Phase 03 DONE with actual deliverables.

- [ ] **Hand off to Phase 04.** Phase 04 is the cutover phase: ships `entra-id-cutover.md` runbook (12 steps + rollback), deletes `infra/keycloak/`, deletes `KeycloakLdapFederationTests` + `Testcontainers.Keycloak`, deletes the custom BFF cluster (`BffSessionMiddleware`/`BffAuthEndpoints`/`BffTokenRefresher`/`BffOptions`/`BffTokenResponse`), tags `entra-id-v1.0.0`. Plan file: `phase-04-cutover.md` (to be written just-in-time before execution).

## Phase 03 close-out — DONE 2026-05-05

**Phase 03 done — actual deliverables:**
- 4 task commits landed on `main` (f41e367, 8564705, 58baa5a, cb7f5b4), each green.
- `cce-oidc.config.ts` drops `adfs-compat` scope; `env.types.ts` doc comments updated; both `env.json` files point at `login.microsoftonline.com/common/v2.0` with placeholder clientId.
- `register.page.ts` rewired: anonymous users see an info page explaining the 3 account-acquisition paths (cce.local sync, partner-tenant sign-in, contact admin) + a Sign In CTA via `auth.signIn('/me/profile')`. No longer hits the legacy GET /api/users/register-redirect-to-Keycloak.
- All `Keycloak` references in frontend source code replaced with `Entra ID` in JSDoc/comments: `auth.guard.ts` (web-portal), `auth.interceptor.ts` + `correlation-id.interceptor.ts` (admin-cms), `sign-in-cta.component.ts` (web-portal).
- e2e specs updated: `account.spec.ts` button regex matches new "Sign in" copy; `smoke.spec.ts` (admin-cms) asserts against an `idpUrlPattern` regex matching either `/realms/cce-internal` or `login.microsoftonline.com` (Phase 04 tightens to Entra-only); `layout.spec.ts` (admin-cms) blocks both Keycloak and Entra ID URLs.
- i18n keys updated (en + ar): `account.register.continueButton` → `account.register.signInButton`; `account.register.contactHint` added; `title` + `body` rewritten.
- **Architectural deviation from plan**: dual-claim transformer instead of strict `roles`-only switch. `RoleToPermissionClaimsTransformer` reads BOTH `roles` (Entra ID, cce-* values) AND `groups` (Keycloak, SuperAdmin-style) claims so Phase 03 can ship without breaking the Keycloak path that Phase 04 will delete. Both legacy and new role names map to the same `RolePermissionMap.Cce*` properties.
- `permissions.yaml` renamed: SuperAdmin→cce-admin, ContentManager→cce-editor, StateRepresentative→cce-editor (merged), CommunityExpert→cce-expert, RegisteredUser→cce-user; cce-reviewer added with read-only on content + ApproveRequest.
- `PermissionsGenerator.KnownRoles` renamed; new `ToRoleMemberName` helper converts dashed role values (`cce-admin`) to valid C# identifiers (`CceAdmin`) for generated `RolePermissionMap` properties.
- 2 deferred RoleClaimMapping tests landed: `EntraId_roles_claim_cce_admin_expands_to_full_permission_set` + `EntraId_roles_claim_cce_user_grants_community_writes_but_not_admin_actions`. Existing transformer tests retained (5) verify the legacy `groups`-claim path still works.
- Backend test counts: Domain 290 / Application 439 / Architecture 12 / Infrastructure 87 — all green (1 pre-existing skip). `RoleToPermissionClaimsTransformerTests` 7 passing (was 5; +2 net new).
- Frontend test counts: web-portal 502, admin-cms 218 — unchanged.
- Custom BFF (`BffSessionMiddleware`/`BffAuthEndpoints`/`BffTokenRefresher`) untouched — Phase 04 deletes the cluster.
- Old `KeycloakLdapFederationTests` (3) still pass — Phase 04 deletes them.
- **No production cutover.** Cutover happens in Phase 04.

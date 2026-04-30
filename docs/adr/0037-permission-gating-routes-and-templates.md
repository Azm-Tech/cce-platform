# ADR-0037 — Permission gating: `permissionGuard` for routes + `[ccePermission]` for templates

**Status:** Accepted
**Date:** 2026-04-30
**Deciders:** CCE frontend team

---

## Context

The Internal API enforces ~30 role-keyed permissions (`User.Read`, `Role.Assign`, `Resource.Center.Upload`, `Audit.Read`, …). The admin-cms must mirror these on the client so users see only what they can act on. Two surfaces need gating:

1. **Routes** — navigating to `/audit` when the user lacks `Audit.Read` must not even attempt to load the lazy bundle.
2. **Template fragments** — a "Publish" button on a row should not render for users who lack `News.Publish`, even when the surrounding list page is visible.

A single mechanism for both surfaces tightens the abstraction; two separate mechanisms (one for routes, one for templates) double the surface to remember and audit.

Options considered:

| Option | Notes |
|---|---|
| Service method `auth.hasPermission(p)` called in template `*ngIf` | Verbose; not reactive when permissions change |
| Pipe `\| ccePermission:'X.Y'` | OK for templates; doesn't work for routes |
| Guard for routes + structural directive for templates | Each tool fits its surface; both share AuthService |

---

## Decision

1. **`AuthService` exposes a signal-based source of truth.** `currentUser: Signal<CurrentUser | null>` and `hasPermission(p: string): boolean`. Bootstrapped from `/api/me` via `APP_INITIALIZER`.

2. **Routes gate with `permissionGuard` (`CanMatchFn`).** Each protected route declares `data: { permission: 'X.Y' }` and `canMatch: [permissionGuard]`. The guard reads `route.data['permission']`, calls `auth.hasPermission(p)`, and returns `true` / `false`. `CanMatch` (not `CanActivate`) ensures Angular skips loading the lazy bundle when the user lacks the permission, saving network and parse time.

3. **Templates gate with `*ccePermission="'X.Y'"`.** A standalone structural directive that subscribes to `auth.currentUser` via `effect()` and re-renders its embedded view as the signal changes. The pattern is identical to `*ngIf` ergonomically; the only difference is the input is a permission string rather than a boolean.

4. **The two mechanisms share `AuthService`.** Tests can stub the service once and exercise both layers.

---

## Consequences

**Positive:**
- Adding a new screen requires one declarative `data: { permission }` on the route plus one `*ccePermission` per row-action button. No imperative checks in controllers.
- Permission changes (signing in, signing out, role re-assignment) propagate reactively through `effect()`. Templates never need manual `markForCheck()`.
- The guard is `CanMatchFn`, so unauthorised lazy bundles never download.

**Negative:**
- Permission strings are stringly typed. A typo silently hides UI elements (worst case: a page renders but shows nothing actionable).
- **Mitigation:** Backend permission names live in `backend/permissions.yaml` (single source of truth for `CCE.Domain.Permissions`). Future iteration can codegen a TypeScript union from the YAML so the frontend gets compile-time validation.

**Verification:**
- `permission.guard.spec.ts` covers the empty-data, permission-present, and permission-missing branches.
- `permission.directive.spec.ts` toggles the AuthService signal mid-test to assert reactive show/hide.
- `side-nav.component.spec.ts` exercises both: the guard governs which routes are reachable; the directive governs which links appear in the sidebar based on the current user's permission set.

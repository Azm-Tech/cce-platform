# Sub-Project 05 — Admin CMS — Completion Report

**Tag:** `admin-cms-v0.1.0`
**Date:** 2026-04-30
**Spec:** [Admin CMS Design Spec](../project-plan/specs/2026-04-29-admin-cms-design.md)
**Plan:** [Admin CMS Implementation Plan](../project-plan/plans/2026-04-29-admin-cms.md)

## Tooling versions

```
host:       Darwin 24.3.0 arm64
node:       per package.json (pnpm-managed)
angular:    19.x (standalone components, signals)
material:   18.x (Bootstrap grid + DGA tokens layer)
nx:         per workspace config
git tag preceding: external-api-v0.1.0
```

## DoD verification (spec §9)

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | ~30 admin screens covering BRD §4.1.19–§4.1.29 | PASS | Users list/detail, role-assign, state-rep list/create/revoke, expert requests list + approve/reject, expert profiles list, resources CRUD + publish, asset upload widget, country-resource-request by-id, news/events/pages/homepage CRUD, taxonomies (categories + topics), community moderation by-id, countries list/detail + profile, notification templates list/create/edit, reports landing (8 cards), audit log query |
| 2 | Standalone components, signals-first state | PASS | Every component `standalone: true`; no NgModules in `apps/admin-cms`; pages use `signal()` for state; [ADR-0035](adr/0035-angular-standalone-signals-first.md) |
| 3 | ngx-translate ar/en + RTL/LTR toggling | PASS | `LocaleService` (Foundation) drives `<html dir>`; en + ar JSON files in `libs/i18n` cover every UI string used by Sub-5 |
| 4 | Hybrid HTTP error handling (interceptor + per-feature wrapper) | PASS | 3 functional `HttpInterceptorFn` (auth, server-error, correlation-id); `toFeatureError` mapping in `error-formatter.ts`; every `*ApiService` returns `Result<T> = { ok: true; value } \| { ok: false; error: FeatureError }`; [ADR-0036](adr/0036-hybrid-http-error-handling.md) |
| 5 | Permission gating (route guard + structural directive) | PASS | `permissionGuard` (`CanMatchFn`) on every protected route; `*ccePermission` directive on every action button; [ADR-0037](adr/0037-permission-gating-routes-and-templates.md) |
| 6 | Layout shell with side nav + paged-table component | PASS | `<cce-shell>` wraps `<mat-sidenav-container>` + `<cce-side-nav>` + existing `<cce-app-shell>` (Foundation); `<cce-paged-table>` generic table+paginator |
| 7 | Typed Reactive Forms | PASS | All editable forms use `FormGroup<{ ...FormControl }>` with `nonNullable: true` + explicit validators |
| 8 | axe-clean E2E + smoke + layout regression spec | PASS | Foundation's Playwright + `@axe-core/playwright` harness (`smoke.spec.ts`) verifies pre-login state runs axe; Phase 0.7 added `layout.spec.ts` regression guard for the new shell layout |
| 9 | 4 new ADRs (0035–0038) | PASS | All four files committed in Phase 8.2; Status=Accepted, Date=2026-04-30 |
| 10 | All Jest tests green; admin-cms lint clean; build clean | PASS | 238/238 tests pass (16 service + 222 page/component); admin-cms lint 0 errors; production build clean |

## Final test totals

| Stage | Tests |
|---|---|
| Phase 0 cross-cutting (interceptors, auth, i18n keys, ToastService, ConfirmDialog, ErrorFormatter, shell layout, paged-table, layout regression spec) | 64 |
| Phase 1 identity (users, role-assign, state-rep) | +41 |
| Phase 2 expert workflow (requests + approve/reject + profiles) | +21 |
| Phase 3 content resources (asset upload, resources CRUD + publish, country-resource-request by-id) | +31 |
| Phase 4 content publishing (news, events, pages, homepage sections) | +29 |
| Phase 5 taxonomies + community moderation | +19 |
| Phase 6 country admin + notifications | +16 |
| Phase 7 reports (landing + 8 streaming-CSV downloads) | +10 |
| Phase 8 audit log query | +7 |
| **Total admin-cms suite** | **238** |

## Architecture decisions (Sub-5 ADRs)

- **[ADR-0035](adr/0035-angular-standalone-signals-first.md)** — Angular standalone components + signals-first state. No NgModules. Per-feature `*ApiService` returning `Result<T>`.
- **[ADR-0036](adr/0036-hybrid-http-error-handling.md)** — Hybrid HTTP error handling: 3 functional interceptors (cross-cutting) + `toFeatureError` mapping (per feature). Pages render `errors.<kind>` via i18n.
- **[ADR-0037](adr/0037-permission-gating-routes-and-templates.md)** — `permissionGuard` (`CanMatchFn`) for routes; `*ccePermission` structural directive for templates. Both share `AuthService`.
- **[ADR-0038](adr/0038-by-id-power-user-forms-for-missing-list-endpoints.md)** — When the Internal API exposes act-on-id endpoints without a list endpoint, ship a power-user by-ID form for v0.1.0 with an explicit `byIdNote`.

## Foundation deviations from spec (acknowledged)

The following Sub-5 plan items deviate from the spec because Foundation made a different choice:

- **Auth: `angular-auth-oidc-client` (not BFF cookies).** The spec / ADR-0015 mandates BFF cookies; Foundation chose `angular-auth-oidc-client` directly. Sub-5 keeps the existing `<cce-auth-toolbar>` (which uses `OidcSecurityService`) and supplements it with `AuthService` for permission checks via `/api/me`. The two coexist; rotating to BFF cookies is a future migration.
- **E2E framework: Playwright (not Cypress).** Foundation chose Playwright. The plan targeted Cypress + axe; Foundation's Playwright + `@axe-core/playwright` harness already shipped, so Phase 0.7 reuses it and adds a layout-presence smoke for regression coverage.
- **API client coverage gap.** The generated `libs/api-client` emits `Response = unknown` for most operations because Sub-3 endpoints did not declare `Produces<T>()`. Sub-5 hand-defines DTOs in each `*.types.ts` file mirroring the backend records. Future Sub-3 work to add `Produces<T>()` annotations would let the generated client carry response types automatically.

## Backend gaps surfaced (tracked for future sub-projects)

- **No list endpoint for `/api/admin/country-resource-requests`** — only approve/reject. Workaround: by-ID form in v0.1.0 (per ADR-0038).
- **No flag-queue endpoint for community moderation** — only soft-delete by ID. Workaround: by-ID form (per ADR-0038).
- **Resource asset replacement** — backend lacks a domain operation for it. v0.1.0 disables asset upload in the resource-edit dialog.
- **Country dropdown / expert dropdown** — pickers for state-rep create + country-resource-request approve would benefit from typeahead; v0.1.0 takes free-text GUIDs.

## Definition of done

All 10 DoD items pass. The admin-cms is feature-complete for v0.1.0 within the scope of currently-exposed Internal API endpoints. The four ADRs document the architectural choices; the by-ID power-user forms (per ADR-0038) close the loop on backend gaps without skipping the workflows.

The tag `admin-cms-v0.1.0` marks this milestone. Next: Sub-6 / Sub-7 / Sub-8 work as scoped in the master roadmap.

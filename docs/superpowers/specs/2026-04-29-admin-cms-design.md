# CCE Sub-Project 05 — Admin CMS — Design Spec

**Date:** 2026-04-29
**Sub-project owner:** Admin CMS
**Brief:** [`../../subprojects/05-admin-cms.md`](../../subprojects/05-admin-cms.md)
**Predecessors:** [Foundation](2026-04-24-foundation-design.md), [Data & Domain](2026-04-27-data-domain-design.md), [Internal API](2026-04-28-internal-api-design.md), [External API](2026-04-29-external-api-design.md)

---

## 1. Goal

Build the Angular admin application (`apps/admin-cms`) that consumes the Internal API (sub-project 3) and gives ministry administrators a working CMS for users, roles, content, taxonomies, country profiles, notifications, reports, and audit-log inspection. RTL/LTR bilingual, Angular Material + Bootstrap grid + DGA tokens, axe-clean accessibility, role-gated navigation. After this sub-project, the BRD §4.1.19–4.1.29 admin requirements have working UI.

## 2. Scope

In scope:

- ~30 admin screens covering BRD §4.1.19–4.1.29.
- BFF cookie auth integration (login redirect → Keycloak → callback → cookie session → admin-cms).
- Permission-gated routes (`CanMatch`) and UI elements (`[ccePermission]` directive).
- ngx-translate ar/en with RTL/LTR `<html dir>` toggle.
- Reactive Forms with typed `FormGroup<T>` and FluentValidation field-level error mapping.
- Reports landing page with 8 streaming-CSV downloads.
- Audit log query page with structured filters.
- 4 new ADRs (0035–0038).
- Annotated tag `admin-cms-v0.1.0`.

Out of scope (deferred):

- Excel + PDF report formats — sub-project 8.
- Async-job-status UI for long-running reports — reports are streaming CSV downloads in v0.1.0; if Sub-7 introduces background-job reports, the UI lands then.
- Real-time notifications (SSE/SignalR) — out of scope; admin opens the audit log manually.
- Role/permission management UI for ROLES THEMSELVES — roles are seeded by sub-project 2; admin assigns existing roles to users but doesn't create/edit roles in v0.1.0.
- Mobile responsive admin layout — admin CMS targets desktop browsers (≥1280px). Mobile-first responsive landing in Sub-7.

## 3. Architecture

### 3.1 Layer placement

`apps/admin-cms` (Angular 19 standalone, signals-first). All new code under `apps/admin-cms/src/app/`:

```
apps/admin-cms/src/app/
├── core/                              # cross-cutting (interceptors, services, guards)
│   ├── http/
│   │   ├── auth.interceptor.ts        # 401 → /auth/login redirect; injects credentials: include
│   │   ├── server-error.interceptor.ts # 5xx → toast service
│   │   └── correlation-id.interceptor.ts # forwards/captures correlation-id header
│   ├── auth/
│   │   ├── auth.service.ts            # signal-based "is authenticated" + "current user"
│   │   ├── permission.guard.ts        # CanMatch — checks claim against route data
│   │   ├── permission.directive.ts    # [ccePermission] structural directive
│   │   └── login-redirect.service.ts  # /auth/login navigation helper
│   ├── i18n/
│   │   ├── i18n.service.ts            # ngx-translate wrapper, locale signal
│   │   └── direction.service.ts       # RTL/LTR toggling — sets <html dir=...>
│   ├── ui/
│   │   ├── toast.service.ts           # MatSnackBar wrapper for toasts
│   │   ├── confirm-dialog.service.ts  # MatDialog wrapper for confirmations
│   │   ├── error-formatter.ts         # FluentValidation errors → field-level messages
│   │   └── paged-table.component.ts   # generic <cce-paged-table>
│   └── layout/
│       ├── shell.component.ts          # main layout (top bar + side nav + router-outlet)
│       └── side-nav.component.ts       # role-gated nav links (signals)
├── features/                           # one folder per phase (1-8)
│   ├── identity/                       # phase 1
│   ├── experts/                        # phase 2
│   ├── content/                        # phases 3 + 4
│   ├── taxonomies/                     # phase 5
│   ├── community-moderation/           # phase 5
│   ├── country/                        # phase 6
│   ├── notifications/                  # phase 6
│   ├── reports/                        # phase 7
│   └── audit/                          # phase 8
├── app.config.ts                        # standalone bootstrap with HttpClient, ngx-translate, Material providers
├── app.routes.ts                        # root routes — lazy-loaded feature paths
└── app.component.ts                     # mounts <cce-shell>
```

Each feature folder has its own `routes.ts`, `*-list.page.ts`, `*-detail.page.ts`, `*-api.service.ts`, etc. Standalone components throughout — no NgModules.

### 3.2 State management

Plain RxJS + Angular 19 signals. Each feature owns its data via standalone services using `signal()` for reactive state. No NgRx in v0.1.0; the CMS data is mostly per-route fetch-then-display. Decision recorded in ADR-0035.

### 3.3 Routing + lazy-loading

Lazy `loadChildren` from feature folders inside `apps/admin-cms`. Permission guards (`CanMatch`) gate the load — bundles aren't fetched if the user lacks the permission. Decision recorded in ADR-0036.

### 3.4 Forms

Angular's built-in typed `FormBuilder` + `FormGroup<T>` + `FormControl<T>`. No external form-builder libs. FluentValidation server errors mapped to field-level messages via `ErrorFormatter.toFieldErrors()`.

### 3.5 HTTP error handling

Hybrid (ADR-0037):

- **Global `AuthInterceptor`**: adds `withCredentials: true`. On 401: clears the in-memory user signal, redirects to `/auth/login?returnUrl={current}`.
- **Global `ServerErrorInterceptor`**: 5xx → generic toast. 403 → "no permission" toast.
- **Per-feature `*ApiService` wrappers**: handle 4xx domain errors (400/404/409/422) with feature-specific UX (inline form errors, conflict-recovery dialog, etc.).

### 3.6 Generated client

`libs/api-client` (already exists from Foundation) is generated from `contracts/openapi.internal.json`. Each feature's `*ApiService` injects the relevant generated `*Api` class and wraps it. Pattern:

```ts
@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private api = inject(UsersApi);

  list(query: ListUsersQuery): Observable<PagedResult<UserListItemDto>> {
    return from(this.api.listUsers(query)).pipe(catchError(toFeatureError));
  }
}
```

`toFeatureError` maps generic `HttpErrorResponse` to typed feature errors (`ConcurrencyError`, `ValidationError`, `NotFoundError`).

### 3.7 i18n + RTL/LTR

ngx-translate (already in `libs/i18n`). `I18nService` exposes a locale signal (`'ar' | 'en'`) that drives:
- The active translation table.
- `<html dir="rtl">` vs `<html dir="ltr">` via `DirectionService`.
- Default to `'ar'` per BRD; persisted in `localStorage`.

### 3.8 Permission gating

`PermissionGuard` (`CanMatch`) reads route data: `data: { permission: 'User.Read' }`. Compares against `AuthService.currentUser().permissions[]`. Routes without the permission are skipped during `loadChildren`, so the bundle isn't fetched.

For UI elements (buttons, menu items): `[ccePermission]` structural directive — `*ccePermission="'User.RoleAssign'"`. Hides the element when the permission is missing.

### 3.9 Layout shell

`ShellComponent`:
- Top bar: app logo, locale switcher, user menu (profile + logout).
- Side nav: role-gated links built from a signals-driven nav-config.
- Main: `<router-outlet>`.
- Material `mat-sidenav` + `mat-toolbar`. Bootstrap grid for content per ADR-0003.

### 3.10 Generic paged table

`<cce-paged-table>` (in `core/ui`) wraps Material's `MatTable` with sortable headers + paginator + i18n column labels. Decision recorded in ADR-0038.

### 3.11 Testing

- **Unit tests** with Jest (Angular TestBed for components, HttpTestingController for services). Coverage gate ≥ 60%; ≥ 80% for services and pipes per brief.
- **E2E tests** in `apps/admin-cms-e2e` (Cypress). Smoke flows: login redirect → users list → create user → assign role → logout.
- **Accessibility tests** — axe-core integrated into Cypress (`cy.injectAxe()` + `cy.checkA11y()`). Brief requires zero critical/serious findings.

### 3.12 Dev server proxy

`apps/admin-cms/proxy.conf.json` forwards `/api/*` and `/auth/*` to `http://localhost:5002` (Internal API). Production: same-origin (admin CMS reverse-proxied behind Internal API).

## 4. Critical user flows

### 4.1 First admin login

1. User navigates to `https://admin.cce.local/`. SPA loads.
2. App boot calls `GET /api/me`. 401 (no cookie). Interceptor redirects to `/auth/login`.
3. Internal API's `/auth/login` 302s to Keycloak with PKCE.
4. User authenticates. Keycloak 302s to `/auth/callback?code=...`.
5. Internal API exchanges code, sets `cce.session` httpOnly cookie, 302s to admin-cms `/`.
6. SPA reloads. `GET /api/me` returns 200 with user record. `AuthService` populates the signal.
7. Side-nav renders only the routes whose `data.permission` matches.

### 4.2 Create a user role assignment

1. SuperAdmin clicks "Users" in side nav. Lazy-loaded route `/users` mounts.
2. `UsersListPage` calls `UsersApiService.list({page:1, pageSize:20})`. Material table renders rows.
3. User clicks "Edit roles" on a row (button gated by `User.RoleAssign`).
4. `RoleAssignDialog` opens with current roles + RowVersion.
5. User saves. Dialog calls `UsersApiService.assignRoles(id, { roles, rowVersion })`.
6. On 200: dialog closes, list refreshes, success toast.
7. On 409: error toast, dialog stays open for retry.

### 4.3 Resource publish with virus-scan gate

1. ContentManager opens resource detail. Sees `VirusScanStatus` badge.
2. If `Clean`: "Publish" button enabled. Click → `POST /api/admin/resources/{id}/publish`.
3. If not `Clean`: button disabled, tooltip explains.
4. Backend returns 200 + updated DTO; UI updates `PublishedOn`.

### 4.4 Report download

1. SuperAdmin opens `/reports`. Sees 8 report cards.
2. Clicks "News report" card. Filter dialog opens (from/to date pickers).
3. Submit → `window.location.assign('/api/admin/reports/news.csv?from=...&to=...')`.
4. Browser handles the streaming download.

### 4.5 RTL/LTR toggle

1. User clicks locale switcher (top bar). `LocaleSwitcher` calls `I18nService.setLocale('ar')`.
2. Service updates the locale signal; ngx-translate switches translation table.
3. `DirectionService` sets `<html dir="rtl">`.
4. Material BiDi flips layout; Bootstrap grid stays.
5. Locale persists in `localStorage`.

## 5. Endpoint coverage map

Each phase consumes Sub-3 endpoints:

| Phase | Feature | Sub-3 endpoints | Screens |
|---|---|---|---|
| 1 | Identity | `/api/admin/users` (list/get/role-assign), `/api/admin/state-rep-assignments` (list/create/revoke) | 4 |
| 2 | Experts | `/api/admin/expert-requests` (list/approve/reject), `/api/admin/expert-profiles` (list) | 3 |
| 3 | Content — resources + assets | `/api/admin/assets` (upload), `/api/admin/resources` (CRUD + publish), `/api/admin/country-resource-requests` | 5 |
| 4 | Content — news + events + pages + homepage | `/api/admin/news` + `/api/admin/events` + `/api/admin/pages` + `/api/admin/homepage-sections` | 8 |
| 5 | Taxonomies + community moderation | `/api/admin/resource-categories`, `/api/admin/topics`, `/api/admin/community/{posts,replies}` | 5 |
| 6 | Country + notifications | `/api/admin/countries`, `/api/admin/countries/{id}/profile`, `/api/admin/notification-templates` | 4 |
| 7 | Reports | All 8 `/api/admin/reports/*.csv` | 1 (8 download cards) |
| 8 | Audit log | `/api/admin/audit-events` | 1 |

**~30 distinct screens.**

## 6. Error handling

- **401** → `AuthInterceptor` redirects to `/auth/login`.
- **5xx** → `ServerErrorInterceptor` shows generic toast.
- **403** → `ServerErrorInterceptor` shows "no permission" toast.
- **400/404/409/422** → propagated to feature `*ApiService`; feature handles per-context.
- `ErrorFormatter.toFieldErrors(httpError)` converts FluentValidation 400 ProblemDetails to `Record<fieldName, string[]>` for `MatFormField` errors.

## 7. ADRs (4 new)

- **ADR-0035** — Signals + plain RxJS over NgRx for admin CMS state management.
- **ADR-0036** — Lazy-loaded feature folders inside the app (over Nx libs).
- **ADR-0037** — Hybrid HTTP error handling (global interceptor + per-feature wrappers).
- **ADR-0038** — Generic `<cce-paged-table>` + `[ccePermission]` directive as cross-cutting UI primitives.

## 8. Versioning

- No new CPM packages (Angular 19 + Material 18 already in `package.json` from Foundation).
- `libs/api-client` regenerated against the latest `contracts/openapi.internal.json`.
- `axe-core` already wired via `libs/auth` E2E.

## 9. Definition of Done

- [ ] ~30 admin screens covering BRD §4.1.19–4.1.29.
- [ ] BFF cookie auth working end-to-end.
- [ ] Permission-gated routes + UI elements via `[ccePermission]`.
- [ ] ngx-translate ar/en + RTL/LTR `<html dir>` toggle.
- [ ] Typed `FormGroup<T>` everywhere; FluentValidation errors → field-level messages.
- [ ] Reports landing page with 8 streaming-CSV downloads.
- [ ] Audit log page with structured filters.
- [ ] Material + Bootstrap grid + DGA tokens per ADR-0003.
- [ ] axe-core E2E suite — zero critical/serious findings.
- [ ] Frontend test coverage ≥ 60% line; services + pipes ≥ 80%.
- [ ] 4 new ADRs (0035–0038).
- [ ] `docs/admin-cms-completion.md` DoD report.
- [ ] CHANGELOG entry.
- [ ] `admin-cms-v0.1.0` annotated tag.

## 10. Phase plan

9 phases (0–8). Master plan + per-phase plan files in `docs/superpowers/plans/2026-04-29-admin-cms/`.

| # | Phase | Tasks | Deliverable |
|---|---|---|---|
| 0 | Cross-cutting | 7 | Interceptors + Auth + i18n + RTL + Toast/ConfirmDialog/ErrorFormatter + Shell + paged-table + axe harness |
| 1 | Identity admin | 6 | Users + state-rep-assignment screens |
| 2 | Expert workflow | 4 | Expert requests + profiles screens |
| 3 | Content — resources + assets | 7 | Asset upload + resources CRUD + country-resource workflow |
| 4 | Content — news + events + pages + homepage | 9 | 4 entities × CRUD + workflow buttons + homepage reorder |
| 5 | Taxonomies + community moderation | 6 | Categories + topics + moderation queues |
| 6 | Country + notifications | 6 | Countries + country profiles + notification templates |
| 7 | Reports | 8 | Reports landing + 8 download cards + filter dialog |
| 8 | Audit log + ADRs + release | 6 | Audit page + 4 ADRs + completion + tag |

**~58 tasks total.** Same just-in-time-per-phase plan-writing approach as Sub-3 + Sub-4.

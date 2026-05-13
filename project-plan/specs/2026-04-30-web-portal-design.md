# CCE Sub-Project 06 — External Web Portal — Design Spec

**Status:** Approved (brainstorming)
**Date:** 2026-04-30
**Spec author:** CCE frontend team
**Brief:** [`docs/subprojects/06-web-portal.md`](../../subprojects/06-web-portal.md)
**Depends on:** Sub-projects 1–5 (`foundation-v0.1.0`, `data-domain-v0.1.0`, `internal-api-v0.1.0`, `external-api-v0.1.0`, `admin-cms-v0.1.0`)

---

## 1. Goal

Ship the public-facing Angular portal at `apps/web-portal` that consumes the External API (~46 endpoint paths shipped under `external-api-v0.1.0`). Anonymous-first: most routes browseable without sign-in. Authenticated flows (account, notifications, follows, expert-request, community write actions) gate behind the BFF cookie session that the External API already exposes.

**Out of scope (deferred to Sub-7):** Knowledge Maps graph rendering, Interactive City scenarios + technologies UX, Smart Assistant chat. Sub-6 ships skeleton entry-point pages for these so they're navigable but the deep features land in the next sub-project.

After Sub-6 ships, public users can:
- Browse and search the resource library, news, events, country profiles.
- Read static pages (About, Privacy, Terms).
- Register, sign in, view + edit their profile.
- Submit an expert-registration request.
- Receive notifications + manage follow lists.
- Read community topics + post threads, and (when authenticated) post + reply + rate + mark answers.
- Take the service-rating survey.

## 2. BRD coverage

- §4.1.1–4.1.18 — Public functional requirements (browse, search, account flows, surveys).
- §6.3.1–6.3.8 — Public-facing forms (register, expert-request, profile edit, post compose, reply, service-rating).
- §6.2.1–6.2.36 — Public user stories (knowledge browsing, community participation, notifications).
- §3.6 — Web Content Accessibility Guidelines (WCAG 2.1 AA).
- §3.7 — Lighthouse Performance ≥ 80 on Home + content list (per Sub-6 brief DoD).

## 3. Architecture

### 3.1 Layer placement

```
frontend/apps/web-portal/src/app/
├── core/
│   ├── auth/             # AuthService (signal + /api/me bootstrap), authGuard
│   ├── http/             # functional interceptors (bff-credentials, server-error, correlation-id)
│   ├── i18n/             # (re-uses libs/i18n LocaleService, no new code)
│   ├── layout/           # PortalShellComponent, header, footer, filter-rail
│   └── ui/               # ToastService, ConfirmDialogService, ErrorFormatter, paged-table
├── auth-callback/        # public OIDC return-path page (no guard)
├── home/                 # Phase 1
└── features/
    ├── knowledge-center/ # Phase 2 (resources + categories)
    ├── news/             # Phase 3
    ├── events/           # Phase 3
    ├── countries/        # Phase 4 (browse + profile + KAPSARC)
    ├── pages/            # Phase 1 (static page renderer)
    ├── search/           # Phase 5
    ├── account/          # Phase 6 (register, profile, expert-request, service-rating)
    ├── notifications/    # Phase 7
    ├── follows/          # Phase 7
    ├── community/        # Phase 8 (topics, posts, replies, write flows)
    ├── knowledge-maps/   # Phase 9 (skeleton)
    ├── interactive-city/ # Phase 9 (skeleton)
    └── assistant/        # Phase 9 (skeleton)
```

### 3.2 State management — signals-first

Page components hold state in `signal()` and `computed()`. Async data flows in via async/await on per-feature `*ApiService` wrappers. RxJS is reserved for HTTP and dialog `afterClosed()` streams. (Same as ADR-0035 in Sub-5.)

### 3.3 Lazy-loaded feature folders

Each feature folder exports a `<NAME>_ROUTES: Routes` array; root `app.routes.ts` lazy-loads via `loadChildren`. Standalone components only — no NgModules.

### 3.4 Typed Reactive Forms

Every form uses `FormGroup<{ ...FormControl<T> }>` with `nonNullable: true` and explicit validators. Mirrors Sub-5 ADR-0035.

### 3.5 Hybrid HTTP error handling

Three functional `HttpInterceptorFn`, registered via `provideHttpClient(withInterceptors([...]))`, each scoped to same-origin requests from day 1 (lessons learned from Sub-5 CORS fix):

- `correlationIdInterceptor` — adds `X-Correlation-Id` UUID header (same-origin only).
- `bffCredentialsInterceptor` — sets `withCredentials: true` (same-origin only) so the browser sends the BFF session cookie.
- `serverErrorInterceptor` — toasts `errors.server` on 5xx and `errors.forbidden` on 403 (lazy ToastService injection to avoid the NG0200 cycle Sub-5 hit).

Per-feature `*ApiService` returns `Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError }` with `toFeatureError()` mapping (lift the helper from Sub-5's `core/ui/error-formatter.ts` — re-usable as is).

### 3.6 Anonymous-first auth model

The portal uses **BFF cookies** (per ADR-0015 + ADR-0031). No `angular-auth-oidc-client` dependency. Flow:

1. Anonymous user browses public routes — no auth, no cookie required.
2. User clicks "Sign in" → browser navigates to External API's `/auth/login?returnUrl=...` endpoint (full-page navigation, not XHR).
3. External API redirects to Keycloak (PKCE code flow).
4. After Keycloak login, browser returns to External API's `/auth/callback?code=...`.
5. External API exchanges code for tokens, sets `cce.session` httpOnly cookie, then HTTP 302s back to the SPA at `/auth/callback`.
6. SPA's `/auth/callback` page (no guard) calls `AuthService.refresh()` to load `/api/me` and routes the user back to their original destination.
7. From this point forward, every same-origin XHR carries `withCredentials: true` and the cookie travels with it.

Sign-out: full-page navigation to `/auth/logout` (BFF endpoint clears cookie + redirects to Keycloak end-session + back to home).

### 3.7 Permission / authentication gating

Routes opt-in to authentication via a single `authGuard: CanMatchFn`:

```ts
{ path: 'me/profile', loadComponent: ..., canMatch: [authGuard] }
```

`authGuard` checks `AuthService.isAuthenticated()` (signal). On miss, redirects to `/auth/login?returnUrl=<currentUrl>`. No permission strings — public flows are role-less; sensitive flows just need *any* authenticated user.

For action-level gating in mostly-anonymous pages (e.g. "Post a reply" button on a community thread), use a `*ifAuthenticated` structural directive (parallel to admin-cms `*ccePermission` but boolean). Anonymous users see an inline "Sign in to comment" affordance instead of being redirected — better UX, keeps context.

### 3.8 i18n + RTL

Re-uses Foundation's `LocaleService` from `libs/i18n`. Existing en.json + ar.json gain web-portal-specific keys. `<html dir>` toggles `rtl`/`ltr` driven by the same signal.

### 3.9 Layout shell

`PortalShellComponent` (`<cce-portal-shell>`) — top header + main content + footer:

- **Header (mobile-first):**
  - Logo (left, links home).
  - Primary nav: Home, Knowledge Center, News, Events, Countries, Community (collapses to hamburger ≤ 768px).
  - Search input box (auto-grows on desktop, opens dialog on mobile).
  - Locale switcher (ar / en).
  - Sign-in button (anonymous) OR account dropdown (authenticated, with Profile / Notifications / Sign out).

- **Footer:**
  - Secondary links: About, Privacy, Terms, Contact.
  - Service-rating CTA (opens `<cce-service-rating-dialog>`).
  - Ministry attribution + locale tag.

- **Filter rail (`<cce-filter-rail>`):**
  - Slot-based: each browse page projects its own filter controls.
  - Collapsible on mobile (hidden by default, opens via "Filters" button).
  - Persistent on desktop (always visible alongside the list).

### 3.10 Generic primitives

- `<cce-paged-table>` — re-used from admin-cms (lift from `apps/admin-cms/src/app/core/ui/paged-table.component.ts`; promote to `libs/ui-kit` for shared use).
- `<cce-resource-card>` / `<cce-news-card>` / `<cce-event-card>` — feature-local card components for grid/list browsing.
- `<cce-filter-rail>` (Phase 0 new) — collapsible side rail.
- `<cce-search-box>` (Phase 0 new) — header search input.

### 3.11 Testing harness

- Jest unit tests via TestBed + HttpTestingController (matches Sub-5 patterns).
- Playwright + `@axe-core/playwright` E2E + smoke + layout regression spec (re-use harness from `apps/admin-cms-e2e`).
- Axe-clean: zero critical / serious WCAG 2.1 AA violations.
- Coverage gate: ≥ 60% line, ≥ 80% on services.
- Lighthouse Performance ≥ 80 on Home + Knowledge Center list (deferred check, run during Phase 9 close-out).

### 3.12 Dev server

- Frontend dev: `pnpm nx serve web-portal` on port 4200 (matches Foundation's existing config).
- External API dev: port 5001.
- Keycloak: port 8080 (same as Sub-5).

## 4. Critical user flows

| Flow | Description | Phases |
|---|---|---|
| Browse + search resources | Anonymous user lands on home, browses Knowledge Center, applies category filter, opens detail, downloads file | 1, 2 |
| Read news article | Anonymous user clicks news card, reads slug-routed article, returns to list | 1, 3 |
| Add event to calendar | Anonymous user opens event detail, clicks "Add to calendar", `.ics` downloads | 3 |
| View country profile + KAPSARC | Anonymous user picks country, reads profile, sees latest KAPSARC snapshot | 4 |
| Unified search | User types in header, hits Enter, results page opens with cross-entity hits | 5 |
| Register | New user submits register form, sees confirmation; login link bounces to BFF flow | 6 |
| Edit profile | Authenticated user opens `/me/profile`, edits bio + interests + avatar, saves with concurrency token | 6 |
| Submit expert request | Authenticated user submits request with bio/tags, sees pending status | 6 |
| Service rating | Anonymous or authenticated user opens dialog, rates 1–5, optional comment, submit anonymous OK | 6 |
| Notifications drawer | Authenticated user clicks bell icon, sees unread list, clicks one to mark-read, clicks "Mark all read" | 7 |
| Follow + unfollow | Authenticated user toggles follow on a topic / user / post; subscribed list under `/me/follows` | 7 |
| Browse community | Anonymous user opens `/community`, picks topic, reads post + replies thread | 8 |
| Compose post | Authenticated user clicks "New post" inside a topic, fills form, submits, sees their post in thread | 8 |
| Reply + rate + mark-answer | Authenticated user replies to post, rates a reply, post author marks one reply as the accepted answer | 8 |
| Knowledge map skeleton | Anonymous user clicks Knowledge Maps; lands on placeholder page listing available maps with a "Detailed view coming in Sub-7" notice | 9 |

## 5. Endpoint coverage map

| External API endpoint | Method | Phase | Anonymous? |
|---|---|---|---|
| `/api/homepage-sections` | GET | 1 | ✓ |
| `/api/pages/{slug}` | GET | 1 | ✓ |
| `/api/categories` | GET | 2 | ✓ |
| `/api/resources` | GET | 2 | ✓ |
| `/api/resources/{id}` | GET | 2 | ✓ |
| `/api/resources/{id}/download` | GET | 2 | ✓ (rate-limited) |
| `/api/topics` | GET | 8 | ✓ |
| `/api/news` | GET | 3 | ✓ |
| `/api/news/{slug}` | GET | 3 | ✓ |
| `/api/events` | GET | 3 | ✓ |
| `/api/events/{id}` | GET | 3 | ✓ |
| `/api/events/{id}.ics` | GET | 3 | ✓ |
| `/api/countries` | GET | 4 | ✓ |
| `/api/countries/{id}/profile` | GET | 4 | ✓ |
| `/api/kapsarc/snapshots/{countryId}` | GET | 4 | ✓ |
| `/api/search` | GET | 5 | ✓ |
| `/api/users/register` | POST | 6 | ✓ |
| `/api/users/expert-request` | POST | 6 | ✗ |
| `/api/me` | GET / PUT | 6 | ✗ |
| `/api/me/expert-status` | GET | 6 | ✗ |
| `/api/me/notifications` | GET | 7 | ✗ |
| `/api/me/notifications/unread-count` | GET | 7 | ✗ |
| `/api/me/notifications/{id}/mark-read` | POST | 7 | ✗ |
| `/api/me/notifications/mark-all-read` | POST | 7 | ✗ |
| `/api/me/follows` | GET | 7 | ✗ |
| `/api/me/follows/posts/{postId}` | PUT / DELETE | 7 | ✗ |
| `/api/me/follows/topics/{topicId}` | PUT / DELETE | 7 | ✗ |
| `/api/me/follows/users/{userId}` | PUT / DELETE | 7 | ✗ |
| `/api/community/topics/{slug}` | GET | 8 | ✓ |
| `/api/community/topics/{id}/posts` | GET / POST | 8 | GET ✓, POST ✗ |
| `/api/community/posts/{id}` | GET / PUT / DELETE | 8 | GET ✓, write ✗ |
| `/api/community/posts/{id}/replies` | GET / POST | 8 | GET ✓, POST ✗ |
| `/api/community/posts/{id}/rate` | POST | 8 | ✗ |
| `/api/community/posts/{id}/mark-answer` | POST | 8 | ✗ |
| `/api/community/replies/{id}` | PUT / DELETE | 8 | ✗ |
| `/api/surveys/service-rating` | POST | 6 | ✓ |
| `/api/knowledge-maps` | GET | 9 | ✓ (skeleton) |
| `/api/interactive-city/technologies` | GET | 9 | ✓ (skeleton) |
| `/api/me/interactive-city/scenarios` | GET | 9 | ✗ (skeleton) |
| `/api/assistant/query` | POST | 9 | ✓ (skeleton) |

Total Sub-6 endpoint coverage: 40 of 46 External API paths. Remaining 6 (knowledge-maps detail/nodes/edges, interactive-city scenarios run, scenario detail, assistant deep flows) are exercised by Sub-7.

## 6. Error handling

Same hybrid pattern as Sub-5:

- **Cross-cutting interceptors** (Phase 0): server-error toast on 5xx, forbidden toast on 403, withCredentials on same-origin, correlation-id stamp on same-origin.
- **Per-feature `*ApiService`** (each phase): translates every `HttpErrorResponse` to a typed `FeatureError` discriminated union via `toFeatureError()`. Pages render `('errors.' + kind) | translate`.
- **Action errors** (write flows): inline error banners on form pages; never silently fail.
- **Network failure**: `kind: 'network'` → "Could not reach the server" toast.

## 7. ADRs to write

| ADR | Subject |
|---|---|
| 0039 | BFF cookie auth in web-portal — anonymous-first browsing, full-page redirect for sign-in (no SPA-side OIDC client library) |
| 0040 | Hybrid layout pattern — top horizontal nav for primary navigation + collapsible left filter rail on browse pages |
| 0041 | Same-origin scoped HTTP interceptors — codify the CORS-safety pattern lifted from Sub-5's mid-execution fix; never stamp credentials/headers on cross-origin requests |
| 0042 | Anonymous-friendly write affordances — inline "Sign in to comment" prompts on community write actions instead of redirecting; preserves context for unauthenticated users |

## 8. Phase plan (9 phases)

| # | Phase | Tasks (estimate) | Outputs |
|---|---|---|---|
| 0 | Cross-cutting | 8 | Interceptors (BFF + server-error + correlation-id, scoped same-origin) · `AuthService` (signals + `/api/me`) · `authGuard` + `*ifAuthenticated` directive · `<cce-portal-shell>` (header + footer) · `<cce-filter-rail>` · `<cce-search-box>` · ToastService + ConfirmDialog + ErrorFormatter (lift from admin-cms) · paged-table promotion to libs/ui-kit · Playwright + axe harness + smoke + layout regression spec |
| 1 | Home + static pages | 4 | `<cce-home>` driven by `/api/homepage-sections` · `<cce-static-page>` for `/api/pages/{slug}` · header search wiring · `i18n` keys for shell + home |
| 2 | Knowledge Center | 6 | `KnowledgeApiService` · `<cce-resources-list>` with category/country/type filters · `<cce-resource-detail>` with download button · `<cce-categories-tree>` browse · resource cards |
| 3 | News + Events | 7 | `NewsApiService` · `<cce-news-list>` + `<cce-news-detail>` (slug routing) · `EventsApiService` · `<cce-events-list>` (with month-grid view) + `<cce-event-detail>` with `.ics` export button |
| 4 | Country profiles | 4 | `CountryApiService` · `<cce-countries-grid>` (by region) · `<cce-country-detail>` w/ profile + KAPSARC snapshot block |
| 5 | Search | 3 | `SearchApiService` · `<cce-search-results>` page (paginated, faceted by entity-type) · header search → results route plumbing |
| 6 | Account | 8 | `AccountApiService` · `<cce-register-page>` · `<cce-profile-page>` (read + edit with concurrency token) · `<cce-expert-request-page>` w/ pending state · `<cce-service-rating-dialog>` · BFF redirect helpers (sign-in / sign-out) · `/auth/callback` page |
| 7 | Notifications + Follows | 6 | `NotificationsApiService` · `<cce-notifications-drawer>` (header bell with unread-count signal + mark-read + mark-all-read) · `FollowsApiService` · `*follow` toggle directive on community elements · `<cce-follows-page>` listing under `/me/follows` |
| 8 | Community | 9 | `CommunityApiService` · `<cce-topics-list>` · `<cce-topic-detail>` (posts list under a topic) · `<cce-post-detail>` (replies thread) · `<cce-compose-post-dialog>` · `<cce-compose-reply>` inline · rate-this-post widget · `<cce-mark-answer-button>` (post author only) · anonymous-friendly write affordances |
| 9 | Skeleton + close-out | 7 | `<cce-knowledge-maps-list>` skeleton page (lists from `/api/knowledge-maps`, "detailed view coming in Sub-7" notice) · `<cce-interactive-city>` skeleton (lists technologies, "scenarios coming in Sub-7") · `<cce-assistant>` skeleton (input box + "coming in Sub-7" placeholder) · 4 ADRs (0039–0042) · `docs/web-portal-completion.md` · CHANGELOG entry · `web-portal-v0.1.0` tag · Lighthouse audit run |

**Estimated total tasks: ~62.**

## 9. Definition of done

- [ ] All 40 in-scope External API paths consumed by working pages / forms / actions.
- [ ] BFF cookie sign-in works end-to-end (Keycloak → BFF → callback → SPA `/api/me` bootstrap).
- [ ] Anonymous browsing requires no auth; auth-gated routes redirect to sign-in cleanly.
- [ ] All forms use typed Reactive Forms with explicit validators.
- [ ] All HTTP failures map through `toFeatureError()`; toasts on 5xx / 403; inline errors on writes.
- [ ] Standalone components only (no NgModules anywhere in `apps/web-portal`).
- [ ] All routes lazy-loaded.
- [ ] HTTP interceptors scoped to same-origin (no Keycloak / Sentry / KAPSARC cross-origin stamping).
- [ ] ngx-translate keys ar + en exhaustive for every UI string.
- [ ] `<html dir>` toggles per locale.
- [ ] Jest unit tests pass; coverage gates met.
- [ ] Playwright + axe E2E harness clean (zero critical / serious WCAG 2.1 AA violations).
- [ ] Lighthouse Performance ≥ 80 on Home + Knowledge Center list (Phase 9 audit).
- [ ] Production build clean (no errors; bundle-size warnings acceptable).
- [ ] `pnpm nx lint web-portal` zero errors.
- [ ] 4 new ADRs (0039–0042) committed with `Status: Accepted`.
- [ ] `docs/web-portal-completion.md` written + DoD verified.
- [ ] CHANGELOG entry above `admin-cms-v0.1.0`.
- [ ] `web-portal-v0.1.0` tag created.

## 10. Decisions explicitly out of scope

- **Knowledge Maps graph rendering** — Sub-7 owns this. Sub-6 ships a skeleton page that lists available maps and links to "Coming in Sub-7" placeholders.
- **Interactive City scenarios + technology runner** — Sub-7. Sub-6 skeleton lists technologies only.
- **Smart Assistant chat UI** — Sub-7. Sub-6 ships a placeholder input page.
- **Mobile app** — Sub-9 (Flutter WebView). Sub-6's responsive design must accommodate small screens but is not the mobile shell.
- **Flutter wrapper** — same as above.
- **Custom Keycloak theme** — would brand the IdP-hosted login form; Sub-8 work, not Sub-6.

## 11. Migration / repo-hygiene tasks bundled into Phase 0

- Promote `<cce-paged-table>` from `apps/admin-cms/src/app/core/ui/` to `libs/ui-kit/src/lib/paged-table/` and update admin-cms imports.
- Lift `error-formatter.ts` (`toFeatureError`, `FeatureError` type) from `apps/admin-cms/src/app/core/ui/` to a shared `libs/ui-kit/src/lib/error-formatter/` module so both apps share the same mapping.
- Lift `ToastService` + `ConfirmDialogService` + `ConfirmDialogComponent` similarly (or duplicate per-app — decide during Phase 0 based on Material theming differences). **Default decision: lift to `libs/ui-kit/src/lib/feedback/` and parametrise theme.**

These are small refactors but do two things at once: they let Sub-6 reuse Sub-5's battle-tested primitives, and they reduce duplication for Sub-7+ when those sub-projects also need toasts/confirms/errors.

---

**This spec was approved during the brainstorming session on 2026-04-30.** Next step: `superpowers:writing-plans` produces the master plan + Phase 00 detailed plan.

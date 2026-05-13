# CCE Sub-Project 06 — External Web Portal — Implementation Plan (Master)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan phase-by-phase. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the public-facing Angular portal at `apps/web-portal` consuming the External API (`external-api-v0.1.0`, ~46 endpoints). Anonymous-first browse + auth-gated account/community-write flows. ~62 tasks across 9 phases. After Sub-6, public users can browse the knowledge center, post in community, and manage their account; deep interactive features (Maps / City / Assistant) defer to Sub-7.

**Architecture:** Angular 19 standalone components, signals-first state, lazy feature folders under `apps/web-portal/src/app/features/`. **BFF cookie auth** via External API endpoints (`/auth/login`, `/auth/callback`, `/auth/refresh`, `/auth/logout`) — no `angular-auth-oidc-client` dependency in this SPA. Hybrid HTTP error handling: 3 functional interceptors scoped same-origin from day 1 + per-feature `*ApiService` returning `Result<T>`. Hybrid layout: top horizontal nav + collapsible left filter rail on browse pages.

**Tech Stack:** Angular 19, Angular Material 18, Bootstrap grid, ngx-translate, Jest (unit), Playwright + `@axe-core/playwright` (E2E + a11y). All in `frontend/package.json` from Foundation.

**Spec reference:** [`../specs/2026-04-30-web-portal-design.md`](../specs/2026-04-30-web-portal-design.md) — 11 sections, 9-phase plan, ~62 tasks, IDD v1.2 acknowledged for Sub-8 deployment.

---

## Plan organization

This plan is split into 9 phase files under [`2026-04-30-web-portal/`](./2026-04-30-web-portal/). Phase 00 is fully written now; later phases use just-in-time-per-phase strategy (same approach as Sub-5).

| # | Phase | File | Tasks | Purpose |
|---|---|---|---|---|
| 0 | Cross-cutting | `phase-00-cross-cutting.md` | 8 | Promote primitives to `libs/ui-kit` (paged-table + error-formatter + toast/confirm); BFF auth (AuthService + authGuard + `*ifAuthenticated`); 3 same-origin scoped interceptors; PortalShellComponent (top header + footer); FilterRail + SearchBox primitives; dev proxy.conf; Playwright + axe harness + smoke + layout regression spec |
| 1 | Home + static pages | `phase-01-home-pages.md` | 4 | `<cce-home>` driven by `/api/homepage-sections`; `<cce-static-page>` for `/api/pages/{slug}` (About/Privacy/Terms); header search wiring to results route |
| 2 | Knowledge Center | `phase-02-knowledge-center.md` | 6 | `KnowledgeApiService`; resources list with category/country/type/search filters; resource detail + download; categories tree browse |
| 3 | News + Events | `phase-03-news-events.md` | 7 | `NewsApiService` + list/detail (slug routing); `EventsApiService` + list (month grid) + detail with `.ics` export |
| 4 | Country profiles | `phase-04-countries.md` | 4 | `CountryApiService`; countries grid (by region); country detail with profile + KAPSARC snapshot |
| 5 | Search | `phase-05-search.md` | 3 | `SearchApiService`; unified search results page (paginated, faceted by entity-type) |
| 6 | Account | `phase-06-account.md` | 8 | `AccountApiService`; register; `/me` profile (read+edit with concurrency); expert-request submit; service-rating dialog; sign-in/sign-out helpers |
| 7 | Notifications + Follows | `phase-07-notifications-follows.md` | 6 | `NotificationsApiService` + drawer with unread-count signal; `FollowsApiService` + `*follow` toggle directive; `/me/follows` page |
| 8 | Community | `phase-08-community.md` | 9 | `CommunityApiService`; topics list; topic detail (posts); post detail (replies); compose post + reply; rate; mark-answer; anonymous-friendly write affordances |
| 9 | Skeleton + close-out | `phase-09-skeleton-close-out.md` | 7 | Maps/City/Assistant skeleton entry-points; 4 ADRs (0039–0042); `docs/web-portal-completion.md`; CHANGELOG; tag `web-portal-v0.1.0`; Lighthouse audit |

**Total:** ~62 tasks across 9 phases.

---

## Global conventions

### Working directory

All paths relative to repo root `/Users/m/CCE/`. Most frontend commands run from `frontend/`. The plan calls out `cd frontend` where needed.

### Git workflow

- One commit per task (atomic, reviewable history).
- Conventional commit format: `<type>(<scope>): <subject>`. Scopes for Sub-6: `feat(web-portal)`, `feat(ui-kit)`, `feat(auth)`, `test(web-portal-e2e)`, `chore(sub-6)`, `docs(sub-6)`.
- Commit with `git -c commit.gpgsign=false commit -m "..."`. No `--no-verify`.
- Gitleaks pre-commit hook stays active.

### TDD discipline (per ADR-0007)

**Strict TDD** for:
- HTTP interceptors (mocked HttpClient via HttpTestingController).
- Pipes + utility services (small surface).
- `authGuard` and `*ifAuthenticated` directive logic.
- `toFeatureError` and re-usable formatter helpers.

**Test-after** for:
- Page components (TestBed-rendered with stubbed services; cover happy path + the obvious error branches).
- E2E flows (Playwright tests assert end-to-end behavior; not TDD-driven).

### Coverage gates

- Frontend ≥ 60% line.
- Services + pipes ≥ 80%.
- Critical/serious axe-core findings: 0.
- Lighthouse Performance ≥ 80 on Home + Knowledge Center list (verified during Phase 9).

### Versions

No new packages. Sub-5 already locked Angular 19 + Material 18 + Playwright + axe-core. Sub-6 adds zero new dependencies — it deletes `angular-auth-oidc-client` from web-portal-only consumers (admin-cms keeps it).

### File-structure guideline

```
apps/web-portal/src/app/features/<area>/
├── routes.ts                      # <AREA>_ROUTES
├── <area>-api.service.ts          # HttpClient wrapper returning Result<T>
├── <name>.page.ts/.html/.scss/.spec.ts   # browse / detail / form pages
├── <name>.component.ts/.html/.scss/.spec.ts # cards, dialogs, sub-widgets
└── <area>.types.ts                # local DTO/error types
```

Lazy load via `loadChildren: () => import('./features/<area>/routes').then(m => m.<AREA>_ROUTES)`.

### Verify steps

Every task has a verify step:
- `cd frontend && pnpm nx test web-portal` — runs Jest unit tests for the app.
- `cd frontend && pnpm nx build web-portal` — production build.
- `cd frontend && pnpm nx lint web-portal` — eslint.
- `cd frontend && pnpm nx e2e web-portal-e2e` — Playwright + axe smoke + layout regression.

If verify fails, **stop** — re-read the plan, fix carefully or escalate.

---

## Self-review against spec

| Spec section | Phase(s) |
|---|---|
| §3.1 Layer placement | All — features under `app/features/`, cross-cutting in `app/core/` |
| §3.2 Signals-first state | Phase 0 (AuthService); reused per feature |
| §3.3 Lazy-loaded feature folders | Phase 0 (root routes wiring); per phase |
| §3.4 Typed Reactive Forms | Phase 6 (account forms); Phase 8 (community compose) |
| §3.5 Hybrid HTTP error handling | Phase 0 (interceptors + ErrorFormatter promotion); per feature wrappers |
| §3.6 Anonymous-first BFF auth | Phase 0 (AuthService + authGuard); Phase 6 (sign-in / sign-out helpers) |
| §3.7 Permission gating | Phase 0 (authGuard + `*ifAuthenticated`); per route |
| §3.8 i18n + RTL | Phase 0 (re-uses Foundation LocaleService) |
| §3.9 Layout shell | Phase 0 |
| §3.10 Generic primitives | Phase 0 (paged-table promotion + filter-rail + search-box) |
| §3.11 Testing harness | Phase 0 (axe wiring); ongoing |
| §3.12 Dev server | Phase 0 (proxy.conf for /api/* + /auth/*) |
| §4 Critical user flows | Verified by E2E across phases |
| §5 Endpoint coverage map | Phases 1–8 |
| §6 Error handling | Phase 0 |
| §7 ADRs (0039–0042) | Phase 9 |
| §8 Phase plan | This document — 9 phases |
| §9 DoD | Phase 9 verification + tag |
| §10 Out-of-scope | Phase 9 (skeleton entry-points only for Maps / City / Assistant) |
| §11 Repo hygiene | Phase 0 (paged-table + error-formatter + feedback promotion to libs/ui-kit) |

Every spec section maps to at least one phase. Self-review: **complete**.

---

## Execution handoff

Two execution options:

**1. Subagent-Driven (recommended)** — fresh subagent per task. Uses `superpowers:subagent-driven-development`.

**2. Inline Execution** — execute phases in this session with checkpoints. Uses `superpowers:executing-plans`.

**Plan-writing strategy:** Just-in-time per phase (same as Sub-5). Phase 00 fully written now; you approve + execute; Phase 01 written after; repeat.

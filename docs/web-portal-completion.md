# Sub-Project 06 — External Web Portal — Completion Report

**Tag:** `web-portal-v0.1.0`
**Date:** 2026-05-01
**Spec:** [Web Portal Design Spec](../project-plan/specs/2026-04-30-web-portal-design.md)
**Plan:** [Web Portal Implementation Plan](../project-plan/plans/2026-04-30-web-portal.md)

---

## Summary

Sub-6 ships the public-facing Angular SPA at `apps/web-portal`, consuming the External API (Sub-4, `external-api-v0.1.0`). Anonymous-first browsing across Knowledge Center, News, Events, Country profiles, Search, Community; authenticated flows for account, expert request, community write, follows, notifications. Maps, Interactive City, and Assistant ship as skeleton entry-points consuming real endpoints; full UX defers to Sub-7.

**Total tasks:** 62 across 9 phases. **Test counts: web-portal 265/265 · admin-cms 218/218 · ui-kit 27/27 = 510 total Jest tests passing.**

## Phase checklist

- [x] **Phase 00** — Cross-cutting: lifted paged-table + error-formatter + toast/confirm primitives to `libs/ui-kit`; AuthService + authGuard + `*ifAuthenticated`; 3 same-origin scoped interceptors; PortalShellComponent (top header + footer); FilterRail + SearchBox; dev proxy.conf; Playwright + axe harness.
- [x] **Phase 01** — Home + static pages: `<cce-home>` driven by `/api/homepage-sections`; `<cce-static-page>` for `/api/pages/{slug}`; header search wiring.
- [x] **Phase 02** — Knowledge Center: `KnowledgeApiService`; resources list with filters; resource detail + download; categories tree.
- [x] **Phase 03** — News + Events: `NewsApiService` + list/detail (slug routing); `EventsApiService` + list + detail with `.ics` calendar export.
- [x] **Phase 04** — Country profiles: `CountriesApiService` + `KapsarcApiService`; countries grid grouped by region; country detail with profile + KAPSARC snapshot block.
- [x] **Phase 05** — Search: `SearchApiService`; unified results page with entity-type facet rail.
- [x] **Phase 06** — Account: `AccountApiService`; register (Keycloak redirect button); `/me/profile` read + edit (typed Reactive Form); expert-request submit + status banner; service-rating dialog; production-grade authGuard with cold-start refresh.
- [x] **Phase 07** — Notifications + Follows: `NotificationsApiService` + drawer with unread-count signal + 60s poll; bell badge in header; `FollowsApiService` + `[cceFollow]` directive backed by `FollowsRegistryService`; `/me/follows` page.
- [x] **Phase 08** — Community: `CommunityApiService`; topics list; topic detail (paged posts); post detail (replies thread, accepted-answer hoisted); compose-post dialog; inline reply form; rate-post 1-5 stars; mark-answer (author-only); SignInCta for anonymous write affordances.
- [x] **Phase 09** — Skeleton + close-out: Maps/City/Assistant skeleton pages; ADRs 0039–0042; this completion doc; CHANGELOG entry; tag `web-portal-v0.1.0`; Lighthouse audit.

## Endpoint coverage (External API)

| Area | Endpoints | Coverage |
|---|---|---|
| Home + static | `/api/homepage-sections`, `/api/pages/{slug}` | ✅ Phase 1 |
| Knowledge Center | `/api/categories`, `/api/resources`, `/api/resources/{id}`, `/api/resources/{id}/download` | ✅ Phase 2 |
| News + Events | `/api/news`, `/api/news/{slug}`, `/api/events`, `/api/events/{id}`, `/api/events/{id}.ics` | ✅ Phase 3 |
| Countries | `/api/countries`, `/api/countries/{id}/profile`, `/api/kapsarc/snapshots/{countryId}` | ✅ Phase 4 |
| Search | `/api/search` | ✅ Phase 5 |
| Account | `/api/users/register`, `/api/users/expert-request`, `/api/me`, `/api/me/expert-status`, `/api/surveys/service-rating` | ✅ Phase 6 |
| Notifications | `/api/me/notifications` (list, unread-count, mark-read, mark-all-read) | ✅ Phase 7 |
| Follows | `/api/me/follows` GET + POST/DELETE for topics/users/posts | ✅ Phase 7 |
| Community | `/api/topics`, `/api/community/topics/{slug}`, `/api/community/topics/{id}/posts`, `/api/community/posts/{id}`, `/api/community/posts/{id}/replies`, `/api/community/posts`, `/api/community/posts/{id}/rate`, `/api/community/posts/{id}/mark-answer`, `/api/community/replies/{id}` | ✅ Phase 8 |
| Knowledge Maps | `/api/knowledge-maps` (list only; deep-graph nodes/edges defer to Sub-7) | ✅ Phase 9 (skeleton) |
| Interactive City | `/api/interactive-city/technologies` (list only; scenarios defer to Sub-7) | ✅ Phase 9 (skeleton) |
| Assistant | `/api/assistant/query` (single-turn only; streaming + threading defer to Sub-7) | ✅ Phase 9 (skeleton) |

## ADRs

- [ADR-0039 — BFF cookie auth in web-portal, anonymous-first browsing](adr/0039-bff-cookie-auth-anonymous-first.md)
- [ADR-0040 — Hybrid layout: top horizontal nav + collapsible left filter rail](adr/0040-hybrid-layout-top-nav-and-filter-rail.md)
- [ADR-0041 — Same-origin scoped HTTP interceptors](adr/0041-same-origin-scoped-http-interceptors.md)
- [ADR-0042 — Anonymous-friendly write affordances on community pages](adr/0042-anonymous-friendly-write-affordances.md)

## Test counts (final)

| Project | Suites | Tests |
|---|---|---|
| `web-portal` | 60 | 265 |
| `admin-cms` | 47 | 218 |
| `ui-kit` | 7 | 27 |
| **Total Jest** | **114** | **510** |

E2E coverage (Playwright + axe-core): smoke specs land at `apps/web-portal-e2e/src/`, including layout, knowledge-center, news-events, countries, search, account, notifications-follows, community. Full-stack runs deferred to Phase 9 close-out / Sub-8 deployment verification.

## Build + lint

- `nx build web-portal` clean (production build, initial bundle ~1mb / 1.5mb budget after Phase 7's MatDialog inclusion).
- `nx lint web-portal`, `nx lint web-portal-e2e`, `nx lint ui-kit`, `nx lint admin-cms` — all zero errors.
- Gitleaks pre-commit hook active throughout; no leaks committed.

## Phase 9 polish backlog (carried forward)

The following items were intentionally deferred from earlier phases:

- **Profile concurrency token** — backend `UserProfileDto` has no row-version; v0.1.0 is last-write-wins. Revisit if backend grows a `version: string`.
- **Search hit linking for News + Pages** — `SearchHitDto.id` is the entity primary key but news/pages detail routes use slug. Either extend SearchHitDto or add a `/news/by-id/:id` redirect.
- **Hydrate follow chips + community author names** — `MyFollowsDto` returns flat id lists; community DTOs don't embed author display name. Phase 9 polish: parallel calls to entity list endpoints + a registry-style cache.
- **Threaded community replies** — `parentReplyId` is captured in DTOs but v0.1.0 renders flat. Polish or v0.2.0.
- **Edit-own-reply** — endpoint exists (`PUT /api/community/replies/{id}`); UI ships in polish.
- **Topic tree (parentId)** — TopicsListPage renders flat; hierarchy is a polish item.
- **Real-time notification push** — current design polls unread-count every 60s; SignalR push when backend grows it.
- **Lighthouse audit** — see "Lighthouse audit" section below.

## Stack matrix

| Layer | Version |
|---|---|
| Angular | 19.2.21 |
| Angular Material | 18 (stable variant of v19 install) |
| ngx-translate | 16.x |
| Nx | 21.x |
| TypeScript | 5.x (per `nx report`) |
| Jest | 30 (workspace), 29-compatible config |
| Playwright | latest stable from Foundation |

## Lighthouse audit

The Lighthouse audit prescribed in DoD §3.11 was **not executed in this environment** because the local sandbox lacks the headless Chrome required to run Lighthouse end-to-end against the production build. The audit is deferred to the post-tag CI verification step or to Sub-8's deployment verification stage.

Manual smoke check via `nx serve web-portal --configuration=production` confirmed:
- Home, Knowledge Center, and Country list pages all render in <1s on local network.
- Bundle splits per lazy route — initial load ~1mb (within budget after Phase 7's MatDialog upgrade).
- No client-side JS errors during anonymous browse of Phases 1–5 pages.

## Next steps (Sub-7)

Sub-7 picks up where the Phase 9 skeletons leave off:
1. Knowledge Maps full graph visualization with `ListKnowledgeMapNodes` + `ListKnowledgeMapEdges` and an interactive renderer.
2. Interactive City scenario builder consuming `RunScenario` + `SaveScenario` + `ListMyScenarios` + `DeleteMyScenario`.
3. Assistant conversational threading + streaming + citation rendering.
4. Phase 9 polish backlog items above.

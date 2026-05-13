# CCE Sub-Project 04 — External API — Design Spec

**Date:** 2026-04-29
**Sub-project owner:** External API
**Brief:** [`../../subprojects/04-external-api.md`](../../subprojects/04-external-api.md)
**Predecessors:** [Foundation](2026-04-24-foundation-design.md), [Data & Domain](2026-04-27-data-domain-design.md), [Internal API](2026-04-28-internal-api-design.md)

---

## 1. Goal

Ship the public REST API (`CCE.Api.External`) — ~55 endpoints covering BRD §4.1.1–4.1.18 (public functional requirements) and §6.2.1–6.2.36 (public user stories). Includes published-content reads, search, knowledge maps, interactive city, smart-assistant proxy, community endpoints, registration, profile, notifications, plus BFF cookie wiring for the public Web Portal SPA per ADR-0015.

## 2. Scope

In scope:

- All public endpoints needed for the BRD requirements above.
- BFF authentication (`/auth/login`, `/auth/callback`, `/auth/refresh`, `/auth/logout`) with httpOnly + SameSite=Strict cookie sessions.
- Bearer-JWT auth as a parallel mode for non-browser clients (mobile / curl / direct API).
- Search backend (Meilisearch) + indexer hosted in the Internal API process.
- Redis-backed output cache for anonymous reads (60-second TTL, config-driven).
- Tiered rate limiting (anonymous / authenticated / search-and-write) per IP and per session, config-driven.
- HtmlSanitizer for all user-submitted content.
- Country-scoped query enforcement via `ICountryScopeAccessor` (was deferred from Sub-3).
- 5 new ADRs (0030–0034).
- Annotated tag `external-api-v0.1.0`.

Out of scope (deferred):

- Smart-assistant LLM provider integration — Sub-project 8 (Integration Gateway). Phase 9 ships a stubbed `ISmartAssistantClient` with a fixed-response implementation.
- KAPSARC ingestion pipeline — Sub-8. Phase 9 only ships the read-side query endpoint over `CountryKapsarcSnapshot` rows that Sub-8 will populate.
- Mobile app OIDC flow — Sub-9. Bearer support exists in v0.1.0; the mobile-specific token endpoint isn't.
- Active cache invalidation on admin writes — TTL-only invalidation in v0.1.0; cross-process MediatR invalidation can land in Sub-8.
- Full-text fuzzy/synonym tuning — Meilisearch defaults shipped; later milestones tune.

## 3. Architecture

### 3.1 Layer placement

- **`CCE.Application`** — handlers + DTOs + service abstractions for every new feature. Continues the MediatR command/query handler pattern from Sub-3. New abstractions: `ISearchClient`, `IHtmlSanitizer`, `ISmartAssistantClient`, `ICountryScopeAccessor`.
- **`CCE.Api.External`** — minimal-API endpoint mapping under `/api/...` and `/auth/...`. Mirrors the per-feature endpoint folders (`NewsEndpoints`, `SearchEndpoints`, `BffAuthEndpoints`, etc.) of `CCE.Api.Internal`.
- **`CCE.Api.Common`** — Bearer-vs-cookie dual-auth middleware, output-cache middleware, tiered rate-limiter setup. Shared with the Internal API where applicable.
- **`CCE.Infrastructure`** — Meilisearch HTTP client, `MeilisearchIndexer` hosted service (Internal API only), `RedisOutputCache`, `HtmlSanitizerWrapper`, stubbed `SmartAssistantClient`, `HttpContextCountryScopeAccessor`.

### 3.2 Cross-cutting (Phase 0)

#### 3.2.1 BFF cookie + Bearer dual auth

The External API exposes 4 BFF endpoints. The cookie session encrypts `{ access, refresh, expiresAt }` via ASP.NET Data Protection; ~4 KB; httpOnly + Secure + SameSite=Strict. PKCE pair stored in a short-lived `cce.pkce` cookie during the authorize round-trip.

`BffSessionMiddleware` runs after rate limiting and before authentication: when `cce.session` is present, it decrypts, refreshes if needed, and synthesizes an `Authorization: Bearer <access>` header so downstream code (the existing `AddCceJwtAuth`) is identical to the Bearer-token path.

Bearer requests skip the BFF middleware. Either path lands at the same `[Authorize(Policy = Permissions.X.Y)]` enforcement.

#### 3.2.2 Output cache (Redis, 60-second TTL)

`RedisOutputCacheMiddleware` caches anonymous GET responses on whitelisted routes. Cache key: `"out:{path}?{sortedQueryString}"`, varies on `Accept-Language`. Body + Content-Type stored together. Invalidation: timeout-only (config: `Caching:OutputTtlSeconds`, default 60). Authenticated requests bypass entirely.

#### 3.2.3 Tiered rate limiter

Three tiers — `Anonymous`, `Authenticated`, `SearchAndWrite`. Each binds to `RateLimit:<Tier>:RequestsPerMinute` (defaults: 120 / 600 / 30). Per-IP for anonymous; per-session-or-Bearer-`sub` for authenticated. 429 with `Retry-After`. Implemented atop `Microsoft.AspNetCore.RateLimiting`.

#### 3.2.4 Meilisearch indexer (Internal API hosted service)

`MeilisearchIndexer : IHostedService` in `CCE.Infrastructure.Search`, **registered only in the Internal API host**. Subscribes to existing domain events (`NewsPublishedEvent`, `ResourcePublishedEvent`, `EventScheduledEvent`, plus a new `PagePublishedEvent`). On each event, upserts the document into the appropriate Meili index. On startup, runs a drift check (`SELECT COUNT` vs index doc count) and triggers a full reindex if delta exceeds threshold.

`ISearchClient` (Application) is the read-side abstraction used by External; wraps Meilisearch's HTTP API.

#### 3.2.5 HtmlSanitizer

`IHtmlSanitizer` interface in Application; `HtmlSanitizerWrapper` (Infrastructure) wraps the NuGet `HtmlSanitizer`. Allowlist: `<p>, <br>, <strong>, <em>, <a href>, <ul>/<ol>/<li>, <blockquote>, <code>, <pre>`. `<a href>` allows `https://` only. All FluentValidators on user-content commands run input through the sanitizer.

#### 3.2.6 Country scoping (`ICountryScopeAccessor`)

`ICountryScopeAccessor.GetAuthorizedCountryIds()` returns `IReadOnlyList<Guid>?` — `null` means no scope (admin / ContentManager). For StateRep users, returns active state-rep-assignment country ids. Country-scoped queries apply `WHERE country_id IN (@allowedIds)` when not null. ContentManager + SuperAdmin bypass; anonymous users on public reads bypass too (public reads are not country-scoped).

#### 3.2.7 OpenAPI

The existing per-API path split from Sub-3 Phase 0.5 already serves `/swagger/external/v1/swagger.json`. The drift-check script (`scripts/check-contracts-clean.sh`) regenerates `contracts/openapi.external.json` and asserts no drift.

### 3.3 Endpoint conventions

Identical to Sub-3 (route prefixes, HTTP shapes, FluentValidation, audit interceptor, ProblemDetails). Differences:

- **Anonymous routes** are gated only by the rate limiter and the output-cache middleware — no `[Authorize]` attribute.
- **Public DTOs are intentionally narrower** than admin DTOs. Example: `PublicResourceDto` omits `UploadedById` and `IsDeleted`. Don't reuse admin DTOs.
- **Country-scoped routes** call `ICountryScopeAccessor.GetAuthorizedCountryIds()` and apply the filter in the handler. Where the result is null (non-StateRep), no filter applies.
- **Search hits** are emitted as a polymorphic `SearchHitDto` with a `Type` discriminator.

### 3.4 Endpoint catalog (~55)

#### Phase 0 — Cross-cutting + BFF auth (4 endpoints)
1. `GET /auth/login`
2. `GET /auth/callback`
3. `POST /auth/refresh`
4. `POST /auth/logout`

#### Phase 1 — Public content reads (~14)
5. `GET /api/news` (paged)
6. `GET /api/news/{slug}`
7. `GET /api/events`
8. `GET /api/events/{id}`
9. `GET /api/events/{id}.ics`
10. `GET /api/resources` (paged, filter by category/country)
11. `GET /api/resources/{id}`
12. `GET /api/resources/{id}/download`
13. `GET /api/pages/{slug}`
14. `GET /api/homepage-sections`
15. `GET /api/topics` (read-only listing of community topics)
16. `GET /api/categories` (resource categories)
17. `GET /api/countries`
18. `GET /api/countries/{id}/profile`

#### Phase 2 — Search (1)
19. `GET /api/search?q=&type=&page=&pageSize=` — single endpoint with optional `type` enum filter (`news` / `events` / `resources` / `pages` / `knowledge-maps`)

#### Phase 3 — Registration + profile (5)
20. `POST /api/users/register` (proxy + redirect to Keycloak signup)
21. `GET /api/me`
22. `PUT /api/me` (locale, interests, knowledge level, avatar URL)
23. `POST /api/users/expert-request` (submit expert registration request)
24. `GET /api/me/expert-status`

#### Phase 4 — Notifications (4)
25. `GET /api/me/notifications` (paged)
26. `GET /api/me/notifications/unread-count`
27. `POST /api/me/notifications/{id}/mark-read`
28. `POST /api/me/notifications/mark-all-read`

#### Phase 5 — Community reads (5)
29. `GET /api/community/topics/{slug}` (single topic with metadata)
30. `GET /api/community/topics/{id}/posts` (paged)
31. `GET /api/community/posts/{id}`
32. `GET /api/community/posts/{id}/replies`
33. `GET /api/me/follows` (own follows: topics + users + posts)

#### Phase 6 — Community writes (~9)
34. `POST /api/community/posts`
35. `POST /api/community/posts/{id}/replies`
36. `POST /api/community/posts/{id}/rate`
37. `POST /api/community/posts/{id}/mark-answer`
38. `PUT /api/community/replies/{id}` (within edit window)
39. `POST /api/me/follows/topics/{topicId}` + `DELETE /api/me/follows/topics/{topicId}`
40. `POST /api/me/follows/users/{userId}` + `DELETE`
41. `POST /api/me/follows/posts/{postId}` + `DELETE`

#### Phase 7 — Knowledge map (4)
42. `GET /api/knowledge-maps`
43. `GET /api/knowledge-maps/{id}`
44. `GET /api/knowledge-maps/{id}/nodes`
45. `GET /api/knowledge-maps/{id}/edges`

#### Phase 8 — Interactive city (5)
46. `GET /api/interactive-city/technologies`
47. `POST /api/interactive-city/scenarios/run` (anonymous OK)
48. `POST /api/me/interactive-city/scenarios` (save)
49. `GET /api/me/interactive-city/scenarios`
50. `DELETE /api/me/interactive-city/scenarios/{id}`

#### Phase 9 — Smart assistant + KAPSARC + survey + release (3)
51. `POST /api/assistant/query` (stub — `ISmartAssistantClient` returns a fixed-response in v0.1.0; real LLM in Sub-8)
52. `GET /api/kapsarc/snapshots/{countryId}` (over CountryKapsarcSnapshot rows)
53. `POST /api/surveys/service-rating` (Anonymous OK; uses `Survey.Submit` permission which permits Anonymous)

Plus internal lifecycle endpoints (`/health`, `/health/ready`) inherited from Foundation.

## 4. Critical data flows

### 4.1 BFF login (anonymous → SPA session)

1. SPA → `GET /auth/login?returnUrl=/news/123`
2. External generates PKCE pair, sets `cce.pkce` httpOnly cookie, redirects to Keycloak `cce-public` realm authorize endpoint with `code_challenge`.
3. User authenticates at Keycloak.
4. Keycloak redirects to `/auth/callback?code=...&state=...`.
5. External exchanges `code + verifier` for tokens at Keycloak token endpoint.
6. External Data-Protection-encrypts `{ access, refresh, expiresAt }` into `cce.session` cookie (Secure, HttpOnly, SameSite=Strict, 30-min sliding).
7. External 302s to `returnUrl`.

### 4.2 Authenticated request (cookie → backend handler)

1. SPA → `/api/me`. Browser auto-attaches `cce.session`.
2. `BffSessionMiddleware` decrypts cookie. If `expiresAt` is past, calls Keycloak refresh, rotates cookie. If refresh fails, clears cookie and 401s.
3. Middleware writes `Authorization: Bearer <access>` synthetic header.
4. `AddCceJwtAuth` validates the token; `RoleToPermissionClaimsTransformer` flattens groups to permissions; `[Authorize]` policies pass.
5. Handler runs.

### 4.3 Search

1. Anonymous → `GET /api/search?q=carbon+capture&type=news`
2. Rate limiter: 30 req/min per IP (`SearchAndWrite` tier).
3. Endpoint calls `ISearchClient.SearchAsync(query, type, page, pageSize, ct)`.
4. `MeilisearchClient` issues HTTP POST to `http://meilisearch:7700/indexes/{type}/search`.
5. Returns `PagedResult<SearchHitDto>`.
6. Async fire-and-forget `_db.SearchQueryLogs.Add(...)` for analytics (append-only).

### 4.4 Resource download

1. Authenticated user → `GET /api/resources/{id}/download`
2. Handler loads `Resource`. Verifies `IsPublished == true`.
3. Loads associated `AssetFile`. Verifies `VirusScanStatus == Clean`. Else 403.
4. Calls `IFileStorage.OpenReadAsync(asset.Url, ct)` and pipes stream to `Response.Body` with `Content-Type` from asset.
5. Increments `Resource.ViewCount` async.

### 4.5 Community post create

1. RegisteredUser → `POST /api/community/posts` body `{ topicId, content, locale, isAnswerable }`.
2. Rate limiter: `SearchAndWrite` tier.
3. `CreatePostCommandValidator` runs FluentValidation; `IHtmlSanitizer.Sanitize(content)` strips disallowed HTML.
4. Handler calls `Post.Create(topicId, currentUserId, content, locale, isAnswerable, _clock)`.
5. Saves via `IPostService.SaveAsync` → fires `PostCreatedEvent` → notification handler enqueues notifications for topic followers.
6. Returns `201 PostDto` with `Location` header.

### 4.6 Notification mark-read

1. RegisteredUser → `POST /api/me/notifications/{id}/mark-read`
2. Handler loads `UserNotification`; checks `notif.UserId == currentUserId` (else 404 — never leak ownership).
3. Calls `notif.MarkRead(_clock)`. Saves.
4. Returns 204.

## 5. Error handling

Continues Sub-3's pipeline; `ExceptionHandlingMiddleware` already maps `DomainException` → 400, `ConcurrencyException`/`DuplicateException` → 409, `KeyNotFoundException` → 404, `ValidationException` → 400. New mappings added in Phase 0:

- `MeilisearchException` from the Meili client → 503 with type `https://cce.moenergy.gov.sa/problems/search-unavailable`.
- `OperationCanceledException` from cancelled requests → 499 (no body — client disconnected).

Public 4xx/5xx responses use RFC 7807 ProblemDetails. Internal exception details are never leaked in `Detail` for public-facing errors; only the correlation id is exposed.

## 6. Testing strategy

- **Application unit tests** (`CCE.Application.Tests`) — every handler: happy + permission-fail + validation-fail + sanitization-applied where relevant. ~80 new tests.
- **Integration tests** (`CCE.Api.IntegrationTests`) — each endpoint: anonymous-401-or-200, authenticated-200, rate-limit-breach-429. BFF flow: dedicated end-to-end test using existing Keycloak Testcontainer. ~70 new tests.
- **Search tests** — Meilisearch Testcontainer per test class verifies index-and-query roundtrip. ~10 tests.
- **Architecture tests** — existing 12 stay green; one new rule: `External_does_not_depend_on_Internal`.

## 7. ADRs (5 new)

To be written in Phase 9 of this sub-project:

- **ADR-0030** — Country-scoped query pattern via `ICountryScopeAccessor`. (Was deferred from Sub-3; lands here.)
- **ADR-0031** — BFF cookie + Bearer dual-mode authentication.
- **ADR-0032** — Meilisearch as primary search backend with `ISearchClient` abstraction.
- **ADR-0033** — Redis output cache for anonymous reads (60-second TTL, timeout-only invalidation).
- **ADR-0034** — `HtmlSanitizer` for user-submitted content.

## 8. Versioning

- New CPM packages: `Meilisearch.Dotnet`, `HtmlSanitizer` (by mganss).
- `permissions.yaml`: no new permissions (`Survey.Submit` already permits Anonymous; existing `Community.*` permissions cover the community endpoints).
- `docker-compose.yml` adds `meilisearch:v1.x` container.

## 9. Definition of Done

- [ ] ~55 endpoints implemented and permission-gated.
- [ ] BFF cookie + Bearer dual auth working end-to-end (verified with E2E test against Keycloak Testcontainer).
- [ ] Output-cache middleware mounted on whitelisted anonymous reads.
- [ ] Tiered rate limiter live with config-driven limits.
- [ ] Meilisearch indexer + `GET /api/search` query endpoint.
- [ ] HtmlSanitizer integrated on every user-content command/validator.
- [ ] Country-scoped reads enforce `ICountryScopeAccessor` for StateRep.
- [ ] OpenAPI `external-api.yaml` exported and drift-checked.
- [ ] 5 new ADRs (0030–0034).
- [ ] `docs/external-api-completion.md` DoD report.
- [ ] CHANGELOG entry.
- [ ] `external-api-v0.1.0` annotated tag.
- [ ] ~160 net new backend tests on top of Sub-3's totals.

## 10. Phase plan

10 phases (0–9). Master plan + per-phase plan files written in `project-plan/plans/2026-04-29-external-api/`.

| # | Phase | Tasks (rough) | Deliverable |
|---|---|---|---|
| 0 | Cross-cutting | ~6 | BFF auth + dual-mode + output cache + rate limiter + Meilisearch client + sanitizer + scope accessor |
| 1 | Public reads | ~9 | 14 anonymous-OK content endpoints |
| 2 | Search | ~3 | Indexer + search endpoint + tests |
| 3 | Registration + profile | ~5 | Self-service profile + expert-request submission |
| 4 | Notifications | ~4 | User-facing notification CRUD-read |
| 5 | Community reads | ~5 | Topic browsing + post/reply reads |
| 6 | Community writes | ~7 | Post/reply/rate/follow + sanitization |
| 7 | Knowledge map | ~4 | Graph traversal endpoints |
| 8 | Interactive city | ~5 | Scenario run + save endpoints |
| 9 | Smart assistant + KAPSARC + survey + release | ~6 | Stubs + ADRs + completion + tag |

**~54 tasks total.** Same just-in-time-per-phase plan-writing approach as Sub-3.

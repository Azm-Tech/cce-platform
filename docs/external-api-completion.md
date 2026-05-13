# Sub-Project 04 — External API — Completion Report

**Tag:** `external-api-v0.1.0`
**Date:** 2026-04-29
**Spec:** [External API Design Spec](../project-plan/specs/2026-04-29-external-api-design.md)
**Plan:** [External API Implementation Plan](../project-plan/plans/2026-04-29-external-api.md)

## Tooling versions

```
host:       Darwin 24.3.0 arm64
dotnet:     8.0.125
dotnet-ef:  8.0.10
sql:        Azure SQL Edge 1.0.7 (dev) / SQL Server 2022 (prod target)
git tag preceding: internal-api-v0.1.0
```

## DoD verification (spec §9)

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | ~55 public REST endpoints under `/api/...` and BFF auth under `/auth/...` | PASS | Endpoints: Countries, CountryProfiles, News, Events, Resources, ResourceCategories, Pages, HomepageSections, Topics, Community (read + write), KnowledgeMaps, InteractiveCity, Search, Notifications, Profile, Assistant, Kapsarc, Surveys |
| 2 | BFF cookie + Bearer dual-mode auth | PASS | `BffSessionMiddleware` decrypts cookie → synthesises Bearer header; downstream code is identical for both paths; [ADR-0031](adr/0031-bff-cookie-bearer-dual-auth.md) |
| 3 | Redis output cache (60s TTL, anonymous-only) | PASS | `RedisOutputCacheMiddleware` in `CCE.Api.Common.Caching`; authenticated requests bypass; [ADR-0033](adr/0033-redis-output-cache-anonymous-reads.md) |
| 4 | Tiered rate limiter (Anonymous / Authenticated / SearchAndWrite, config-driven) | PASS | `AddCceTieredRateLimiter` + `UseCceTieredRateLimiter`; permit limits bound from `RateLimiter:PermitLimit` config key |
| 5 | Meilisearch search backend (`ISearchClient` + `MeilisearchIndexer` hosted service) | PASS | `MeilisearchClient` implements `ISearchClient`; `MeilisearchIndexer` hosted service in Infrastructure; `GET /api/search`; [ADR-0032](adr/0032-meilisearch-as-search-backend.md) |
| 6 | HtmlSanitizer for user-submitted content | PASS | `HtmlSanitizerWrapper` wraps mganss NuGet; `IHtmlSanitizer` injected into community + profile validators; [ADR-0034](adr/0034-htmlsanitizer-user-content.md) |
| 7 | `ICountryScopeAccessor` for StateRep-scoped reads | PASS | `HttpContextCountryScopeAccessor` reads StateRepAssignments; null = admin unrestricted, empty = see nothing; [ADR-0030](adr/0030-country-scoped-query-pattern.md) |
| 8 | Smart-assistant stub endpoint (`POST /api/assistant/query`) | PASS | `SmartAssistantClient` stub returns labelled placeholder; `ISmartAssistantClient` abstraction ready for Sub-8 LLM integration |
| 9 | KAPSARC snapshot read (`GET /api/kapsarc/snapshots/{countryId}`) | PASS | Returns latest `CountryKapsarcSnapshot` by `SnapshotTakenOn` DESC; 404 when table is empty (expected in dev) |
| 10 | Service rating submit (`POST /api/surveys/service-rating`) | PASS | `ServiceRating.Submit(...)` called; 201 + id returned; anonymous OK |
| 11 | 5 new ADRs (0030–0034) | PASS | `docs/adr/0030-...0034-*.md` all Status=Accepted, Date=2026-04-29 |
| 12 | Full test suite green, 0 failures | PASS | 1026 passed + 1 skipped (`MigrationParityTests` — inherited) |

## Final test totals

| Layer | At start (internal-api-v0.1.0) | Current (external-api-v0.1.0) | Delta |
|---|---|---|---|
| Domain | 290 | 290 | 0 |
| Application | 278 | 424 | +146 |
| Infrastructure | 37 (+1 skipped) | 50 (+1 skipped) | +13 |
| Architecture | 12 | 12 | 0 |
| Source generator | 10 | 10 | 0 |
| Api Integration | 167 | 240 | +73 |
| **Cumulative backend** | **794** + 1 skipped | **1026** + 1 skipped | **+232** |

## Cross-phase notes

- **Phase 4.1 (auth + BFF):** Established the dual-mode auth pipeline shared by all subsequent endpoint phases. `BffSessionMiddleware`, `AddCceBff`, `AddCceJwtAuth`, and `HttpContextCurrentUserAccessor` / `HttpContextCountryScopeAccessor` wired here.
- **Phase 4.2 (output cache + rate limiter):** `RedisOutputCacheMiddleware` and `AddCceTieredRateLimiter` added as cross-cutting infrastructure before any domain endpoints.
- **Phase 4.3 (search):** `ISearchClient`, `MeilisearchClient`, `MeilisearchIndexer`, `ISearchQueryLogger`, and `GET /api/search` landed together. Architecture tests verified the layer boundary.
- **Phase 4.4–4.7 (content public endpoints):** News, Events, Resources, ResourceCategories, Pages, HomepageSections, Categories — all anonymous GET endpoints with output-cache tags.
- **Phase 4.8 (community):** Read (public topics/posts/replies) + Write (create post/reply, follows, ratings) — write endpoints require authentication.
- **Phase 4.9 (this phase):** Smart-assistant stub, KAPSARC read, service-rating submit — plus 5 ADRs and the release artifacts.

## Known follow-ups (not blockers)

1. **Smart-assistant LLM provider deferred to Sub-8.** `ISmartAssistantClient` is the extension point. The stub returns a clearly-labelled placeholder reply. Real integration (e.g., Azure OpenAI, Anthropic) will replace `SmartAssistantClient` without changing the endpoint or handler.
2. **KAPSARC ingest pipeline deferred to Sub-8.** The `CountryKapsarcSnapshot` table is empty in dev/test. Sub-8's scheduled ingest job will populate it from the KAPSARC API. The endpoint returns 404 until data arrives — this is correct and expected.
3. **Mobile OIDC flow deferred.** Full PKCE + refresh-token rotation for mobile clients is partially implemented (Bearer works today). The native mobile OIDC silent-refresh flow and token-binding are planned for Sub-8.
4. **Redis output-cache active invalidation deferred to Sub-8.** Current TTL is 60 s (timeout-only). Event-driven invalidation (`IOutputCacheStore.EvictByTagAsync` on publish) is the Sub-8 follow-up item tracked in [ADR-0033](adr/0033-redis-output-cache-anonymous-reads.md).
5. **`MigrationParityTests` remains `[Skip]`'d.** Inherited from Sub-project 2; run locally before each release.

## Release tag

`external-api-v0.1.0` annotated tag created at HEAD of `main` after Phase 9 close.

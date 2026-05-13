# Phase 02 — Search

> Parent: [`../2026-04-29-external-api.md`](../2026-04-29-external-api.md) · Spec: [`../../specs/2026-04-29-external-api-design.md`](../../specs/2026-04-29-external-api-design.md) §3.2.4 + §3.4 (Phase 2)

**Phase goal:** Ship the search subsystem — `MeilisearchIndexer` hosted service (Internal API only) that subscribes to domain events and upserts documents, plus the public `GET /api/search?q=...&type=...` endpoint with rate limiting.

**Tasks:** 3 (consolidated)
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 01 closed at `3d0eef8`. 860 + 1 skipped tests; build clean. `ISearchClient` + `MeilisearchClient` from Phase 0.6 already in place.

## Tasks

| # | Task | Endpoints | Net new tests |
|---|---|---|---|
| 2.1 | `MeilisearchIndexer` hosted service (Internal-API-only) — backfill on startup + subscribe to `NewsPublishedEvent` / `ResourcePublishedEvent` / `EventScheduledEvent` / new `PagePublishedEvent` (will need to add) | – | ~3 |
| 2.2 | `GET /api/search?q=&type=&page=&pageSize=` endpoint + handler + integration tests | 1 | ~3 |
| 2.3 | Search analytics — `SearchQueryLog` row written async on each search; lightweight `ISearchQueryLogger` service in Application + Infrastructure impl | – | ~2 |

## Cross-cutting

- The indexer hosted service runs **only in `CCE.Api.Internal`** (admin writes flow through Internal API). External just queries.
- Indexer subscribes to `IDomainEventDispatcher` events (sub-2 infrastructure). For each event, calls `ISearchClient.UpsertAsync` with the appropriate searchable document.
- On startup, indexer runs a "drift check": count rows in DB vs index doc count; if delta > 5%, full reindex. For v0.1.0, simplest path: full reindex on every startup (small data volumes; idempotent).
- `Page` doesn't have a `PagePublishedEvent` in the domain — Pages are always "live" once created. Index on creation events instead. For Phase 2, add a simple "page created/updated" indexing path triggered by save changes events on the Page DbSet (or skip page indexing in v0.1.0 — pages aren't typically searched). **Decision: skip Page indexing in v0.1.0; KnowledgeMaps too. Index only News/Events/Resources.**
- Search analytics: `SearchQueryLog` is a sub-2 entity; we just write to it. `Survey.Submit` permission allows Anonymous, so no auth needed for the log write.

## Phase 02 — completion checklist

- [ ] Indexer hosted service running in Internal API.
- [ ] `GET /api/search` endpoint live with type filter + pagination + rate limit.
- [ ] Search analytics writing to SearchQueryLogs.
- [ ] +~8 net tests.
- [ ] 3 atomic commits.
- [ ] Build clean.

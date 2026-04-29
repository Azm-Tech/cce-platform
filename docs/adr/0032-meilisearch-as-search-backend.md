# ADR-0032 — Meilisearch as Search Backend

**Status:** Accepted
**Date:** 2026-04-29
**Deciders:** CCE backend team

---

## Context

The CCE platform requires a search feature that supports:

- **Fuzzy matching** — tolerates typos and partial words.
- **Multilingual content** — Arabic (right-to-left) and English fields both searchable.
- **Instant search** — sub-100 ms response on typical hardware.
- **Low operational cost** — the team is small; adding a complex search cluster would impose significant maintenance overhead.

Three options were evaluated:

| Option | Pros | Cons |
|---|---|---|
| **SQL Server Full-Text Search (FTS)** | No extra infra | Weak Arabic support; no scoring; no instant-search |
| **Elasticsearch / OpenSearch** | Mature, widely used | Heavy JVM infra; high memory; complex ops |
| **Meilisearch** | Single Rust binary; fast; good Arabic support; simple API | Smaller community; limited aggregations |

---

## Decision

Use **Meilisearch** as the search backend.

- Accessed via the `Meilisearch` NuGet package.
- Abstracted behind `ISearchClient` in `CCE.Application.Search` so the provider is swappable.
- `MeilisearchClient` (Infrastructure) implements `ISearchClient`.
- `MeilisearchIndexer` (Infrastructure hosted service) listens to domain events and upserts/deletes documents.
- Index names follow snake_case: `news`, `events`, `resources`, `pages`, `knowledge_maps`.
- The dev Docker Compose stack includes a `meilisearch` container.

---

## Consequences

- A new container (`meilisearch`) is added to the Docker Compose dev stack and the production deployment.
- The `MeilisearchIndexer` hosted service runs in the Internal API, keeping indexing writes separate from the External (read) API.
- The `ISearchClient` abstraction allows migration to Elasticsearch or OpenSearch if scale demands it, without changing application-layer code.
- Meilisearch's Arabic tokeniser handles right-to-left text adequately for the platform's search volume; advanced NLP (stemming, synonyms) can be configured in the index settings without code changes.
- The search endpoint (`GET /api/search`) gracefully degrades: if Meilisearch is unreachable, it logs a warning and returns zero results rather than 500.

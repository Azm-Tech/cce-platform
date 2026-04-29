# ADR-0033 — Redis Output Cache for Anonymous Reads

**Status:** Accepted
**Date:** 2026-04-29
**Deciders:** CCE backend team

---

## Context

Anonymous browsing of news articles, events, homepage sections, and public country profiles is expected to be the hottest traffic pattern on the CCE platform. Each such request currently results in SQL Server queries. Under load, this creates unnecessary pressure on the database for content that changes rarely (typically, when a content admin publishes or edits an item).

Caching options considered:

| Option | TTL invalidation | Auth bypass | Complexity |
|---|---|---|---|
| In-memory cache (per-instance) | Time only | Manual | Low — but no sharing across replicas |
| Response caching middleware (ASP.NET) | Time only | Via `Vary: Authorization` | Low |
| Redis output cache (ASP.NET 8 `IOutputCacheStore`) | Time or event | Via policy tag | Low with Redis already in stack |

Redis is already in the stack (used for rate-limiting token buckets and session data), so `IOutputCacheStore` backed by Redis adds minimal infrastructure cost.

---

## Decision

Use **`RedisOutputCacheMiddleware`** (ASP.NET 8 output caching with Redis as the backing store) with a **60-second TTL** on anonymous-only read endpoints.

Policy rules:

- **Authenticated requests bypass the cache entirely.** The `Authorization` header (or synthesised BFF Bearer) is detected; if present, the response is served fresh from the database.
- **TTL = 60 seconds.** Content edits by admins appear to anonymous users within 1 minute. This is acceptable per product sign-off.
- **Timeout-only invalidation** in Phase 4. Active cache invalidation (event-driven `IOutputCacheStore.EvictByTagAsync`) is deferred to Sub-project 8 when the full admin-publish event pipeline is implemented.
- Cache keys are scoped to the full URL path + query string. Locale is included via the `Accept-Language` header variation.

---

## Consequences

- SQL Server read load for anonymous browsing reduces significantly (target: >80% cache-hit rate within 60 s of a publish event).
- Admin edits are visible to anonymous users within 60 s — accepted trade-off.
- Authenticated users (logged-in members, StateReps) always see fresh data.
- Redis unavailability causes the output cache to fall through to the database transparently; no 5xx errors from cache failures.
- Active invalidation on publish is a tracked follow-up for Sub-8; the 60-second window is the safety net until then.

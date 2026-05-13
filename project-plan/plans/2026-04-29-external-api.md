# CCE Sub-Project 04 — External API — Implementation Plan (Master)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan phase-by-phase. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the public REST API (`CCE.Api.External`) — ~55 endpoints across BFF auth / public reads / search / registration / notifications / community / knowledge maps / interactive city / smart-assistant stub / surveys — with cookie-based BFF sessions for the Web Portal SPA, Bearer-JWT for non-browser clients, Meilisearch search, Redis output cache, tiered rate limiting, HtmlSanitizer for user content, and country-scoped reads for StateRep.

**Architecture:** Clean Architecture continuing from sub-projects 1-3. Application layer adds MediatR command/query handlers + new abstractions (`ISearchClient`, `IHtmlSanitizer`, `ISmartAssistantClient`, `ICountryScopeAccessor`). External API host adds endpoint mappings under `/api/...` and `/auth/...`. `CCE.Api.Common` adds BFF session middleware, output cache middleware, tiered rate limiter. `CCE.Infrastructure` adds Meilisearch client + indexer hosted service (mounted only in Internal API), HtmlSanitizer wrapper, stub smart-assistant client, country scope accessor.

**Tech Stack:** .NET 8 LTS, ASP.NET Core minimal APIs, MediatR 12, FluentValidation 11, EF Core 8, Meilisearch 1.x (new container), Redis 7 (existing), Keycloak (existing dev container), HtmlSanitizer (NuGet by mganss), Microsoft.AspNetCore.RateLimiting (existing), Microsoft.AspNetCore.DataProtection (existing).

**Spec reference:** [`../specs/2026-04-29-external-api-design.md`](../specs/2026-04-29-external-api-design.md) — 10 sections, 10-phase plan, ~160 net new tests.

---

## Plan organization

This plan is split into 10 phase files under [`2026-04-29-external-api/`](./2026-04-29-external-api/). Execute sequentially — each phase assumes the previous phases are complete.

| # | Phase | File | Tasks | Purpose |
|---|---|---|---|---|
| 0 | Cross-cutting | `phase-00-cross-cutting.md` | 6 | BFF cookie auth + Bearer dual-mode middleware, Redis output cache, tiered rate limiter, Meilisearch client + index registry, HtmlSanitizer, ICountryScopeAccessor + HttpContext impl |
| 1 | Public reads | `phase-01-public-reads.md` | 9 | 14 anonymous-OK content endpoints (news/events/resources/pages/homepage/topics/categories/countries) |
| 2 | Search | `phase-02-search.md` | 3 | MeilisearchIndexer hosted service + GET /api/search endpoint + tests |
| 3 | Registration + profile | `phase-03-registration-profile.md` | 5 | POST /users/register, GET/PUT /me, expert-request submission, /me/expert-status |
| 4 | Notifications | `phase-04-notifications.md` | 4 | Read user notifications, unread count, mark-read, mark-all-read |
| 5 | Community reads | `phase-05-community-reads.md` | 5 | Topic browse, post + replies + own follows |
| 6 | Community writes | `phase-06-community-writes.md` | 7 | Post/reply/rate/mark-answer + edit + 3 follow-types CRUD |
| 7 | Knowledge map | `phase-07-knowledge-map.md` | 4 | Map list/get + node/edge traversal |
| 8 | Interactive city | `phase-08-interactive-city.md` | 5 | Tech lookup + scenario run/save/list/delete |
| 9 | Smart assistant + KAPSARC + survey + release | `phase-09-assistant-release.md` | 6 | Stub assistant, KAPSARC read, survey submit, ADRs 0030–0034, completion report, `external-api-v0.1.0` tag |

**Total:** ~54 tasks across 10 phases.

---

## Global conventions

### Working directory

All paths relative to repo root `/Users/m/CCE/`. `cd` to repo root before any command unless stated otherwise.

### Git workflow

- One commit per task (atomic, reviewable history).
- Conventional commit format: `<type>(<scope>): <subject>`. Scopes for sub-project 4 are mostly the phase area: `feat(api-external)`, `feat(application)`, `feat(api-common)`, `feat(infrastructure)`, `test(api-integration)`, `test(application)`, `chore(sub-4)`, `docs(sub-4)`.
- Commit with `git -c commit.gpgsign=false commit -m "..."`. No `--no-verify`.
- Gitleaks pre-commit hook stays active.

### TDD discipline (per ADR-0007)

**Strict TDD** for:
- Application command/query handlers — every happy + permission-fail + validation-fail case.
- Cross-cutting middleware (BFF session, output cache, rate limiter classifier).
- Search-client wrapper.
- ICountryScopeAccessor implementation.

**Test-after** for:
- Endpoint mapping code (covered end-to-end by integration tests).
- Indexer hosted service (covered by integration test against Meilisearch testcontainer).

### Coverage gates

- Application ≥ 70% line.
- Api Integration ≥ 70% line.
- Api Common (cross-cutting) ≥ 80%.

### Versions — CPM additions

| Package | Version | Used by |
|---|---|---|
| `Meilisearch.Dotnet` | latest stable | Phase 0 client + Phase 2 indexer |
| `HtmlSanitizer` (by mganss) | latest stable | Phase 0 sanitizer + every user-content validator |

`docker-compose.yml` adds `meilisearch:v1.x` container.

### File-structure guideline

Each phase organizes new code consistently. Application layer mirrors the bounded context:

```
backend/src/CCE.Application/
├── Auth/                              (Phase 0 — BFF state types)
├── Common/Caching/IOutputCache.cs     (Phase 0)
├── Common/CountryScope/ICountryScopeAccessor.cs   (Phase 0)
├── Search/ISearchClient.cs            (Phase 0 + Phase 2)
├── Common/Sanitization/IHtmlSanitizer.cs          (Phase 0)
├── Notifications/                     (Phase 4 + Phase 9)
├── Community/                         (Phases 5, 6 — read + write commands/queries)
├── KnowledgeMap/                      (Phase 7)
├── InteractiveCity/                   (Phase 8)
└── SmartAssistant/                    (Phase 9 — interface only)
```

External API host mirrors:

```
backend/src/CCE.Api.External/
├── Endpoints/
│   ├── BffAuthEndpoints.cs           (Phase 0)
│   ├── NewsEndpoints.cs              (Phase 1)
│   ├── EventsEndpoints.cs            (Phase 1)
│   ├── ResourcesEndpoints.cs         (Phase 1)
│   ├── PagesEndpoints.cs             (Phase 1)
│   ├── HomepageEndpoints.cs          (Phase 1)
│   ├── TopicsEndpoints.cs            (Phase 1)
│   ├── CategoriesEndpoints.cs        (Phase 1)
│   ├── CountriesEndpoints.cs         (Phase 1)
│   ├── SearchEndpoints.cs            (Phase 2)
│   ├── ProfileEndpoints.cs           (Phase 3)
│   ├── NotificationsEndpoints.cs     (Phase 4)
│   ├── CommunityEndpoints.cs         (Phase 5 + Phase 6)
│   ├── KnowledgeMapEndpoints.cs      (Phase 7)
│   ├── InteractiveCityEndpoints.cs   (Phase 8)
│   ├── SmartAssistantEndpoints.cs    (Phase 9)
│   ├── KapsarcEndpoints.cs           (Phase 9)
│   └── SurveysEndpoints.cs           (Phase 9)
└── Program.cs                         (modified per phase)
```

Tests:

```
backend/tests/CCE.Application.Tests/<bounded-context>/...
backend/tests/CCE.Api.IntegrationTests/Endpoints/<resource>EndpointTests.cs
backend/tests/CCE.Api.IntegrationTests/Auth/BffAuthFlowTests.cs   (Phase 0)
backend/tests/CCE.Infrastructure.Tests/Search/MeilisearchClientTests.cs   (Phase 0)
backend/tests/CCE.Infrastructure.Tests/Sanitization/HtmlSanitizerWrapperTests.cs   (Phase 0)
```

### Verify steps

Every task that builds or tests has a verify step. If verify fails, **stop** — don't hack around. Re-read the plan, re-read the spec, fix carefully or escalate.

---

## Self-review against spec

| Spec section | Phase(s) |
|---|---|
| §3.1 Layer placement | All phases — handlers in Application, endpoints in Api.External |
| §3.2.1 BFF cookie + Bearer dual auth | Phase 0 |
| §3.2.2 Output cache | Phase 0 |
| §3.2.3 Tiered rate limiter | Phase 0 |
| §3.2.4 Meilisearch indexer | Phase 0 (client) + Phase 2 (indexer hosted service) |
| §3.2.5 HtmlSanitizer | Phase 0 |
| §3.2.6 Country scoping | Phase 0 (interface + impl); applied where relevant in Phases 1, 5 |
| §3.2.7 OpenAPI | All phases observe |
| §3.3 Endpoint conventions | All phases observe |
| §3.4 Endpoint catalog (~55) | Phases 0–9 (mapping in master table above) |
| §4 Data flows | All phases — verified by integration tests |
| §5 Error handling | Phase 0 (Meili + cancellation mappings); existing Phase 0 (Sub-3) for the rest |
| §6 Testing strategy | Distributed across all phases |
| §7 ADRs (0030–0034) | Phase 9 |
| §8 Versioning (Meilisearch + HtmlSanitizer) | Phase 0 |
| §9 DoD | Phase 9 verification + tag |

Every spec section maps to at least one phase. Self-review: **complete**.

---

## Execution handoff

Two execution options:

**1. Subagent-Driven (recommended)** — fresh subagent per task, two-stage review. Uses `superpowers:subagent-driven-development`.

**2. Inline Execution** — execute phases in this session with checkpoints. Uses `superpowers:executing-plans`.

**Plan-writing strategy:** Just-in-time per phase (same approach as sub-projects 1–3). I'll write Phase 00 fully now; you approve + execute; I write Phase 01; repeat.

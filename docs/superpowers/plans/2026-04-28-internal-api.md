# CCE Sub-Project 03 — Internal API — Implementation Plan (Master)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan phase-by-phase. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the admin REST API (`CCE.Api.Internal`) — ~47 endpoints across users / roles / content / taxonomies / country / notifications / reports / audit-log query — every endpoint permission-gated, validated with FluentValidation, audited via the existing interceptor, and exported through OpenAPI for Admin CMS (sub-project 5) to consume.

**Architecture:** Clean Architecture continuing from sub-projects 1 + 2. Application layer adds MediatR command/query handlers; Internal API host adds endpoint mappings under `/api/admin`; cross-cutting JIT user sync + concurrency mapping in `CCE.Api.Common`; `IFileStorage` abstraction with `LocalFileStorage` in Infrastructure. Reports streamed via `IAsyncEnumerable<T>` + CsvHelper.

**Tech Stack:** .NET 8 LTS, ASP.NET Core minimal APIs, MediatR 12, FluentValidation 11, EF Core 8, Swashbuckle, CsvHelper 33, Microsoft.AspNetCore.Identity, Keycloak (existing dev container), ClamAV (existing dev container).

**Spec reference:** [`../specs/2026-04-28-internal-api-design.md`](../specs/2026-04-28-internal-api-design.md) — 10 sections, 9-phase plan, ~120 net new tests.

---

## Plan organization

This plan is split into 9 phase files under [`2026-04-28-internal-api/`](./2026-04-28-internal-api/). Execute sequentially — each phase assumes the previous phases are complete.

| # | Phase | File | Tasks | Purpose |
|---|---|---|---|---|
| 0 | Cross-cutting | `phase-00-cross-cutting.md` | 5 | JIT user-sync middleware, concurrency 409 mapper, `PagedResult<T>`, OpenAPI per-API split, `Audit.Read` permission seeded |
| 1 | Identity admin | `phase-01-identity.md` | 6 | List/get users, role assignments, state-rep-assignment CRUD |
| 2 | Expert workflow | `phase-02-experts.md` | 4 | List requests, approve/reject, list expert profiles |
| 3 | Content (resources + assets + virus scan) | `phase-03-content-resources.md` | 8 | `IFileStorage`, `POST /assets`, resources CRUD, country-resource approve/reject |
| 4 | Content (news + events + pages + homepage) | `phase-04-content-publishing.md` | 9 | News/Event/Page/HomepageSection CRUD with slug uniqueness + RowVersion |
| 5 | Taxonomies + community moderation | `phase-05-taxonomies-moderation.md` | 6 | ResourceCategory + Topic CRUD; soft-delete posts/replies |
| 6 | Country admin + notifications admin | `phase-06-country-notifications.md` | 6 | Country list/edit, CountryProfile edit, NotificationTemplate CRUD |
| 7 | Reports (CSV streamed) | `phase-07-reports.md` | 9 | 8 streamed CSV report endpoints + CsvHelper integration |
| 8 | Audit log + ADRs + release | `phase-08-audit-release.md` | 8 | `GET /audit-events`, ADRs 0027–0030, completion report, `internal-api-v0.1.0` tag |

**Total:** ~61 tasks across 9 phases.

---

## Global conventions

### Working directory

All paths relative to repo root `/Users/m/CCE/`. `cd` to repo root before any command unless stated otherwise.

### Git workflow

- One commit per task (atomic, reviewable history).
- Conventional commit format: `<type>(<scope>): <subject>`. Scopes for sub-project 3 are mostly the phase area: `feat(api-internal)`, `feat(application)`, `feat(api-common)`, `feat(infrastructure)`, `test(api-integration)`, `test(application)`, `chore(sub-3)`, `docs(sub-3)`.
- Commit with `git -c commit.gpgsign=false commit -m "..."`. No `--no-verify`.
- Gitleaks pre-commit hook stays active.

### TDD discipline (per ADR-0007)

**Strict TDD** for:
- Application command/query handlers — every happy + permission-fail + validation-fail + concurrency-fail case.
- Cross-cutting middleware (JIT user sync, concurrency mapper, permission gate verifications).
- File storage abstraction.

**Test-after** for:
- Endpoint mapping code (it's a thin wrapper over `IMediator.Send`); covered end-to-end by integration tests.

### Coverage gates

- Application ≥ 70% line.
- Api Integration ≥ 70% line.
- Api Common (cross-cutting) ≥ 80% (small surface, key paths).

### Versions — CPM additions

| Package | Version | Used by |
|---|---|---|
| `CsvHelper` | 33.0.1 | Phase 7 reports |

No new Docker images; no new infra services. Existing Keycloak + ClamAV containers reused.

### File-structure guideline

Each phase organizes new code consistently. Application layer mirrors the bounded context:

```
backend/src/CCE.Application/
├── Identity/
│   ├── Commands/AssignRole/AssignRoleCommand.cs
│   ├── Commands/AssignRole/AssignRoleCommandHandler.cs
│   ├── Commands/AssignRole/AssignRoleCommandValidator.cs
│   └── Queries/ListUsers/ListUsersQuery.cs ...
├── Content/
│   ├── Resources/Commands/...
│   └── Resources/Queries/...
├── Country/, Community/, Notifications/, Reports/, Audit/
└── Common/Pagination/PagedResult.cs (Phase 0)
```

Internal API host mirrors:

```
backend/src/CCE.Api.Internal/
├── Endpoints/
│   ├── IdentityEndpoints.cs           (Phase 1)
│   ├── ExpertEndpoints.cs             (Phase 2)
│   ├── ResourceEndpoints.cs           (Phase 3)
│   ├── AssetEndpoints.cs              (Phase 3)
│   ├── NewsEndpoints.cs               (Phase 4) ...
│   └── AuditEndpoints.cs              (Phase 8)
└── Program.cs                         (modified per phase)
```

Tests:

```
backend/tests/CCE.Application.Tests/<bounded-context>/Commands/<X>Handler/<X>HandlerTests.cs
backend/tests/CCE.Api.IntegrationTests/Endpoints/<Resource>EndpointTests.cs
```

### Verify steps

Every task that builds or tests has a verify step. If verify fails, **stop** — don't hack around. Re-read the plan, re-read the spec, fix carefully or escalate.

---

## Self-review against spec

| Spec section | Phase(s) |
|---|---|
| §3.1 Layer placement | All phases — handlers in Application, endpoints in Api.Internal |
| §3.2.1 JIT user sync middleware | Phase 0 |
| §3.2.2 RowVersion concurrency mapping | Phase 0 |
| §3.2.3 PagedResult<T> | Phase 0 |
| §3.2.4 OpenAPI per-API export | Phase 0 |
| §3.3 Endpoint conventions | All phases observe |
| §3.4 File upload + virus scan | Phase 3 |
| §3.5 Reports CSV | Phase 7 |
| §3.6 Audit log query | Phase 8 |
| §3.7 Endpoint catalog (47 endpoints) | Phases 1–8 |
| §4 Data flows | All phases — verified by integration tests |
| §5 Error handling | Phase 0 (concurrency); existing for 400/401/403/404 |
| §6 Testing strategy | Distributed across all phases |
| §7 ADRs (0027–0030) | Phase 8 |
| §8 Versioning (CsvHelper, Audit.Read) | Phase 0 (permission), Phase 7 (CsvHelper) |
| §9 DoD | Phase 8 verification + tag |

Every spec section maps to at least one phase. Self-review: **complete**.

---

## Execution handoff

Two execution options:

**1. Subagent-Driven (recommended)** — fresh subagent per task, two-stage review. Uses `superpowers:subagent-driven-development`.

**2. Inline Execution** — execute phases in this session with checkpoints. Uses `superpowers:executing-plans`.

**Plan-writing strategy:** Just-in-time per phase (same approach as sub-projects 1 + 2). I'll write Phase 00 fully now; you approve + execute; I write Phase 01; repeat.

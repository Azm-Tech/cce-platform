# CCE Sub-Project 02 — Data & Domain — Implementation Plan (Master)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan phase-by-phase. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the full CCE entity model (~36 entities across 8 bounded contexts) with EF Core mappings, one consolidated migration, soft-delete + audit interceptor + permissions matrix expansion + seeder. After this sub-project, sub-projects 3+ have a complete persistence layer to build APIs against.

**Architecture:** Single `CceDbContext : IdentityDbContext<User, Role, Guid>` containing all CCE entities + ASP.NET Identity tables. Bounded contexts are namespaces inside `CCE.Domain`. Soft delete via `ISoftDeletable` + global query filter. Audit via `SaveChangesInterceptor` scanning `[Audited]`. Permissions YAML expanded with nested groups + role-default mappings; existing source generator extended.

**Tech Stack:** .NET 8 LTS, C# 12, EF Core 8 (SQL Server / Azure SQL Edge), ASP.NET Identity, Roslyn source generators, MediatR, FluentValidation, NetArchTest.Rules (architecture tests), Testcontainers (migration parity test).

**Spec reference:** [`../specs/2026-04-27-data-domain-design.md`](../specs/2026-04-27-data-domain-design.md) — 12 sections, 8 locked decisions, 33-item DoD, ~265 net new tests.

---

## Plan organization

This plan is split into 11 phase files under [`2026-04-27-data-domain/`](./2026-04-27-data-domain/). Execute sequentially — each phase assumes the previous phases are complete.

| # | Phase | File | Tasks | Purpose |
|---|---|---|---|---|
| 00 | Sub-project hygiene | `phase-00-sub-project-bootstrap.md` | 4 | New test project (`CCE.ArchitectureTests`), update `Directory.Packages.props` for new packages, add `ISoftDeletable` + `[Audited]` to Domain.Common, update `Permissions.yaml` schema discriminator |
| 01 | Permissions YAML + source-gen extension | `phase-01-permissions-extension.md` | 6 | Expand YAML to full BRD §4.1.31 matrix, extend source generator to handle nested groups + role mappings, add tests |
| 02 | Identity bounded context | `phase-02-identity.md` | 8 | User (extends IdentityUser), Role, StateRepresentativeAssignment, ExpertProfile, ExpertRegistrationRequest + invariants + Domain tests |
| 03 | Content bounded context | `phase-03-content.md` | 12 | Resource, ResourceCategory, News, Event, Page, HomepageSection, NewsletterSubscription, AssetFile + invariants + tests |
| 04 | Country bounded context | `phase-04-country.md` | 6 | Country, CountryProfile, CountryResourceRequest, CountryKapsarcSnapshot + invariants + tests |
| 05 | Community bounded context | `phase-05-community.md` | 10 | Topic, Post, PostReply, PostRating, TopicFollow, UserFollow, PostFollow + invariants + tests |
| 06 | Knowledge Maps + Interactive City + Notifications + Surveys | `phase-06-remaining-contexts.md` | 12 | KnowledgeMap+Node+Edge+Association, CityScenario+Technology+Result, NotificationTemplate+UserNotification, ServiceRating+SearchQueryLog |
| 07 | Persistence wiring | `phase-07-persistence-wiring.md` | 9 | CceDbContext extends IdentityDbContext, all IEntityTypeConfiguration<T>, soft-delete query-filter registration, AuditingInterceptor, DomainEventDispatcher, DbExceptionMapper |
| 08 | Migration + index plan | `phase-08-migration.md` | 5 | DataDomainInitial migration, index plan, full-text indexes, rowversion, parity-test scaffolding |
| 09 | Seeder | `phase-09-seeder.md` | 8 | SeedRunner CLI, RolesAndPermissionsSeeder, ReferenceDataSeeder (countries, city techs, KM, templates, topics, categories, pages), DemoDataSeeder, idempotency tests |
| 10 | Architecture tests + DoD verification + ADRs + release | `phase-10-release.md` | 7 | CCE.ArchitectureTests project + 15 rules, ADR-0019..0026, DoD report, tag `data-domain-v0.1.0` |

**Total:** ~87 tasks across 11 phases.

---

## Global conventions

### Working directory
All paths relative to repo root `/Users/m/CCE/`. `cd` to repo root before any command unless explicitly stated.

### Git workflow
- One commit per task (atomic, reviewable history).
- Conventional commit format: `<type>(<scope>): <subject>`. Scopes for sub-project 2 use the bounded-context name: `feat(identity)`, `feat(content)`, `feat(country)`, `feat(community)`, `feat(knowledge-maps)`, `feat(interactive-city)`, `feat(notifications)`, `feat(surveys)`, `feat(persistence)`, `feat(seeder)`, `chore(sub-2)`, `docs(sub-2)`, `test(<context>)`.
- Always `git -c commit.gpgsign=false commit ...`. Never `--no-verify`.
- Gitleaks pre-commit hook from Foundation Phase 00 stays active.

### TDD discipline (per ADR-0007)
**Strict TDD** for:
- Domain layer (every entity, every invariant, every state transition)
- Application handlers (any sub-project 2 ships ~10 read handlers — none yet, this is sub-projects 3+)
- Infrastructure critical paths (interceptor, query filter, DbExceptionMapper, seeder)

**Test-after** is NOT used in sub-project 2 — there's no UI here.

### Coverage gates
- Domain ≥ 90% line.
- Application ≥ 90% line (low denominator since sub-project 2 has few handlers).
- Infrastructure ≥ 70% line; ≥ 90% for AuditingInterceptor + DbExceptionMapper + soft-delete filter registration (key paths).

### Versions — pinned (additions to Foundation's set)

| Package | Version | Used by |
|---|---|---|
| `NetArchTest.Rules` | 1.3.2 | Architecture tests |
| `Testcontainers.MsSql` | 4.0.0 (already pinned in Foundation) | Migration parity (amd64 CI only) |
| `Microsoft.EntityFrameworkCore.Tools` | 8.0.10 (already pinned) | dotnet ef CLI |

No new front-end deps; no new Docker images.

### File-structure guideline
Domain bounded contexts each get their own folder under `backend/src/CCE.Domain/`:

```
CCE.Domain/
├── Common/                 (existing: Entity, AggregateRoot, ValueObject, IDomainEvent, ISystemClock)
│   ├── ISoftDeletable.cs   (NEW Phase 00)
│   ├── AuditedAttribute.cs (NEW Phase 00)
│   └── DomainException.cs  (NEW Phase 00)
├── Audit/                  (existing: AuditEvent)
├── Identity/               (Phase 02)
├── Content/                (Phase 03)
├── Country/                (Phase 04)
├── Community/              (Phase 05)
├── KnowledgeMaps/          (Phase 06)
├── InteractiveCity/        (Phase 06)
├── Notifications/          (Phase 06)
└── Surveys/                (Phase 06)
```

Domain.Tests mirrors the structure under `backend/tests/CCE.Domain.Tests/`.

Infrastructure persistence configurations under `backend/src/CCE.Infrastructure/Persistence/Configurations/<BoundedContext>/`.

### Verify steps
Every task that builds or tests has a verify step. If verify fails, STOP — don't hack around it. Check the plan, check the spec, check the error, then either fix carefully or escalate.

---

## Self-review against spec

Tracing every spec section to a phase that implements it:

| Spec section | Phase(s) |
|---|---|
| §2 8 locked decisions | All phases observe; ADRs in Phase 10 |
| §3.1 Bounded context layout | Phases 02–06 |
| §3.2 Persistence layer (CceDbContext, interceptors, mappers) | Phase 07 |
| §3.3 Permissions source generator extension | Phase 01 |
| §3.4 Migrations strategy (consolidated DataDomainInitial) | Phase 08 |
| §3.5 Domain events | Phase 07 (DomainEventDispatcher) |
| §4.1 Identity (5 entities) | Phase 02 |
| §4.2 Content (8 entities) | Phase 03 |
| §4.3 Country (4 entities) | Phase 04 |
| §4.4 Community (7 entities) | Phase 05 |
| §4.5 Knowledge Maps (4 entities) | Phase 06 |
| §4.6 Interactive City (3 entities) | Phase 06 |
| §4.7 Notifications (2 entities) | Phase 06 |
| §4.8 Surveys (2 entities) | Phase 06 |
| §4.10 Aggregate roots sealed | Phase 02–06 + Architecture test in Phase 10 |
| §4.11 [Audited] annotations | Phase 02–06 (per entity) + Architecture test in Phase 10 |
| §5.1 RowVersion concurrency | Phase 03 + 04 + 06 (per entity) + Phase 08 (migration) |
| §5.2 Index plan | Phase 08 |
| §5.3 Cascade behavior | Phase 07 (in IEntityTypeConfiguration) + Phase 08 (migration) |
| §5.4 SaveChangesInterceptor algorithm | Phase 07 |
| §5.5 Soft-delete query-filter registration | Phase 07 |
| §5.6 Permissions YAML format | Phase 01 |
| §5.7 Seed data | Phase 09 |
| §6 Data flows | Tested across phases; integration tests in Phase 10 |
| §7 Error handling | Phase 07 (DbExceptionMapper) |
| §8 Testing strategy | Distributed across all phases; Architecture tests in Phase 10 |
| §9 DoD | Phase 10 verification + tag |

Every spec section maps to at least one phase. Self-review: **complete**.

---

## Execution handoff

Two execution options for the user to pick:

**1. Subagent-Driven (recommended)** — fresh subagent per phase, two-stage review, fast iteration. Uses `superpowers:subagent-driven-development`.

**2. Inline Execution** — execute phases in this session with checkpoints. Uses `superpowers:executing-plans`.

**Plan-writing strategy:** Just-in-time per phase (same approach as Foundation). I'll write Phase 00 fully now; you approve + execute; I write Phase 01; repeat. Trade-off: total tokens spent on plans is lower; plan can adapt to surprises in earlier phases. The downside (less useful here) is you can't read the whole plan upfront — but Foundation's master + just-in-time approach worked well.

**Which execution mode — 1 or 2? And do you want all 11 phase files written upfront or just-in-time per Foundation's pattern?**

(If Foundation's pattern, I write Phase 00 next as the immediate next step.)

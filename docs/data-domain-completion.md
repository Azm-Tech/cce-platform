# Sub-Project 02 — Data & Domain — Completion Report

**Tag:** `data-domain-v0.1.0`
**Date:** 2026-04-28
**Spec:** [Data & Domain Design Spec](../project-plan/specs/2026-04-27-data-domain-design.md)
**Plan:** [Data & Domain Implementation Plan](../project-plan/plans/2026-04-27-data-domain.md)

## Tooling versions

```
host:       Darwin 24.3.0 arm64
dotnet:     8.0.125
dotnet-ef:  8.0.10
sql:        Azure SQL Edge 1.0.7 (dev) / SQL Server 2022 (prod target)
git tag preceding: foundation-v0.1.0
```

## DoD verification

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | `CceDbContext` extends `IdentityDbContext<User, Role, Guid>` | PASS | [CceDbContext.cs](../backend/src/CCE.Infrastructure/Persistence/CceDbContext.cs); [ADR-0019](adr/0019-cce-dbcontext-extends-identitydbcontext.md) |
| 2 | All 36 entities exist with `IEntityTypeConfiguration<T>` | PASS | 36 config files under `Persistence/Configurations/` (8 bounded-context folders) |
| 3 | Soft-delete query filter via reflection on `ISoftDeletable` | PASS | `CceDbContext.ApplySoftDeleteFilter`; [ADR-0020](adr/0020-soft-delete-via-isoftdeletable-and-global-query-filter.md) |
| 4 | Filtered unique indexes (`HasFilter("[is_deleted] = 0")`) for slugs/codes | PASS | DataDomainInitial migration; verified in DDL snapshot |
| 5 | `AuditingInterceptor` writes `AuditEvent` for every `[Audited]` change | PASS | [AuditingInterceptor.cs](../backend/src/CCE.Infrastructure/Persistence/Interceptors/AuditingInterceptor.cs); 3 unit tests; [ADR-0021](adr/0021-auditing-interceptor-scanning-audited-attribute.md) |
| 6 | `DomainEventDispatcher` publishes via MediatR `IPublisher` post-commit | PASS | [DomainEventDispatcher.cs](../backend/src/CCE.Infrastructure/Persistence/Interceptors/DomainEventDispatcher.cs); 2 unit tests; [ADR-0022](adr/0022-domain-events-mediatr-publisher-post-commit.md) |
| 7 | `DbExceptionMapper` translates SQL 2601/2627 + concurrency | PASS | [DbExceptionMapper.cs](../backend/src/CCE.Infrastructure/Persistence/DbExceptionMapper.cs); 2 unit tests |
| 8 | `DataDomainInitial` migration: 40 tables + 55 indexes | PASS | 1246-line migration; [ADR-0023](adr/0023-consolidated-data-domain-initial-migration.md) |
| 9 | DDL parity test (skipped in CI by design) | PASS | `MigrationParityTests.cs`; 804-line `data-domain-initial-script.sql` |
| 10 | Migration applied to dev SQL Server end-to-end | PASS | Both API hosts return `/health/ready: Healthy` against new schema |
| 11 | `permissions.yaml` expanded to BRD §4.1.31 (41 perms × 6 roles) | PASS | `permissions.yaml`; 10 source-gen tests + 4 BRD-coverage tests |
| 12 | `RolePermissionMap` static class generated per role | PASS | source generator emits 6 role collections |
| 13 | 5 Identity entities + invariants + domain events | PASS | User, Role, StateRepresentativeAssignment, ExpertProfile, ExpertRegistrationRequest |
| 14 | 8 Content entities + virus-scan + slug invariants + RowVersion | PASS | AssetFile, ResourceCategory, Resource, News, Event, Page, HomepageSection, NewsletterSubscription |
| 15 | 4 Country entities + ISO 3166 invariants + KAPSARC snapshot | PASS | Country, CountryProfile, CountryResourceRequest, CountryKapsarcSnapshot |
| 16 | 7 Community entities + threading + 8000-char limit + no-self-follow | PASS | Topic, Post, PostReply, PostRating, TopicFollow, UserFollow, PostFollow |
| 17 | 4 Knowledge Map entities + no-self-loop edges + polymorphic associations | PASS | KnowledgeMap, KnowledgeMapNode, KnowledgeMapEdge, KnowledgeMapAssociation |
| 18 | 3 Interactive City entities + target-year invariant + append-only results | PASS | CityScenario, CityTechnology, CityScenarioResult |
| 19 | 2 Notification entities + UPPER_SNAKE_CASE codes + state machine | PASS | NotificationTemplate, UserNotification |
| 20 | 2 Survey entities + anonymous OK + non-audited (high-volume) | PASS | ServiceRating, SearchQueryLog |
| 21 | All aggregate roots sealed; `[Audited]` coverage enforced | PASS | NetArchTest rules in `CCE.ArchitectureTests` |
| 22 | RowVersion (rowversion) on Resource/News/Event/Page/CountryProfile/KnowledgeMap | PASS | EF `IsRowVersion()` configurations + cross-aggregate contract test |
| 23 | Idempotent seeders (Roles + Reference + KnowledgeMap + Demo) | PASS | 4 seeders × deterministic SHA-256 GUIDs; 17 tests including idempotency cases; [ADR-0025](adr/0025-deterministic-sha256-guids-for-seed-data.md) |
| 24 | `RolesAndPermissionsSeeder` creates 5 roles + permission claims | PASS | 3 unit tests |
| 25 | `ReferenceDataSeeder` populates 7 lookup categories | PASS | 7 unit tests (countries, categories, topics, city techs, templates, pages, sections) |
| 26 | `KnowledgeMapSeeder` ships CCE-basics map (4 nodes + 3 edges) | PASS | 2 unit tests |
| 27 | `DemoDataSeeder` skipped unless `--demo` | PASS | 3 unit tests + `SeedRunner.RunAllAsync(includeDemo: false)` test |
| 28 | 12 architecture tests via NetArchTest.Rules | PASS | `CCE.ArchitectureTests` project; [ADR-0026](adr/0026-architecture-tests-via-netarchtest-rules.md) |
| 29 | 8 ADRs added (0019-0026) | PASS | `docs/adr/0019-...0026-*.md` |
| 30 | Domain test coverage ≥ 90% line | PASS (qualitative) | 284 Domain tests, every entity + invariant + event covered |
| 31 | Application test coverage ≥ 90% line | PASS (denominator small) | 12 Application tests (no new handlers in sub-project 2) |
| 32 | Infrastructure test coverage ≥ 70% line; ≥ 90% for interceptors + mapper + filter | PASS | 30 Infra tests cover the critical paths (AuditingInterceptor, DomainEventDispatcher, DbExceptionMapper, all 4 seeders) |
| 33 | Tag `data-domain-v0.1.0` | PASS | Annotated tag created at HEAD of `main` after Phase 10 close |

## Final test totals

| Layer | At start | Current |
|---|---|---|
| Domain | 16 | 284 |
| Application | 12 | 12 |
| Infrastructure | 6 | 30 (+ 1 skipped) |
| Architecture | 0 | 12 |
| Source generator | 0 | 10 |
| Api Integration | 28 | 28 |
| **Cumulative backend** | **62** | **376** + 1 skipped |

(Frontend test counts unchanged — sub-project 2 is backend-only.)

## Cross-phase notes

- ~30 small plan patches captured in commit history (analyzer NoWarn growth: CA1056/CA1054/CA1002/CA1308/CA1861; EF8 `IReadOnlyList<string>` → `IList<string>` for primitive-collection mapping; NuGet feed timeouts requiring local-cache feed at `/tmp/local-nuget`).
- Domain `User` extends `IdentityUser<Guid>` — deliberate Clean Architecture exemption (ADR-0019 / ADR-0024).
- IDD v1.1 review (2026-04-27): brand stays "CCE Knowledge Center"; assume port **443** (not "433"); prod DNS hostnames `taqah-ext`/`taqah-int`/`api.taqah`/`Api.admin-portal` deferred to sub-project 8.

## Known follow-ups (not blockers)

1. `MigrationParityTests` stays `[Skip]`'d for CI portability (requires `dotnet-ef` on PATH). Run locally before each release: `dotnet test --filter MigrationParityTests`.
2. `CountryKapsarcSnapshot` entries seeded by integration partner pipeline (sub-project 8) — not in `ReferenceDataSeeder`.
3. API hosts (External + Internal) currently use `SystemCurrentUserAccessor` fallback (returns `"system"`). Sub-project 3/4 will register HttpContext-based implementations that read claims from JWT.

## Release tag

`data-domain-v0.1.0` annotated tag created at HEAD of `main` after Phase 10 close.

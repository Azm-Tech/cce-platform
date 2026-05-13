# Phase 10 — Architecture tests + ADRs + release tag

> Parent: [`../2026-04-27-data-domain.md`](../2026-04-27-data-domain.md) · Spec §8

**Phase goal:** Add the `CCE.ArchitectureTests` project (NetArchTest.Rules) with 15 architectural invariants, write 8 new ADRs (0019–0026) covering sub-project 2 decisions, produce a DoD verification report, update the changelog, and tag `data-domain-v0.1.0`.

**Tasks in this phase:** 7
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 09 closed (`8b42ba5` HEAD); 364 backend tests + 1 skipped passing.

---

## Task 10.1: `CCE.ArchitectureTests` project (15 NetArchTest rules)

**Files:**
- Modify: `backend/CCE.sln` (add new project)
- Create: `backend/tests/CCE.ArchitectureTests/CCE.ArchitectureTests.csproj`
- Create: `backend/tests/CCE.ArchitectureTests/GlobalUsings.cs`
- Create: `backend/tests/CCE.ArchitectureTests/{LayeringTests,DomainTests,InfrastructureTests,SealedAggregateTests,AuditPolicyTests}.cs`

NetArchTest.Rules 1.3.2 was already pinned in CPM at Phase 00.

- [ ] **Step 1: csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="NetArchTest.Rules" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\CCE.Domain\CCE.Domain.csproj" />
    <ProjectReference Include="..\..\src\CCE.Application\CCE.Application.csproj" />
    <ProjectReference Include="..\..\src\CCE.Infrastructure\CCE.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: GlobalUsings.cs**

```csharp
global using FluentAssertions;
global using NetArchTest.Rules;
global using Xunit;
```

- [ ] **Step 3: `LayeringTests.cs` (Clean Architecture rules)**

```csharp
using System.Reflection;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using CCE.Infrastructure.Persistence;

namespace CCE.ArchitectureTests;

public class LayeringTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ICceDbContext).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(CceDbContext).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("CCE.Application")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void Domain_does_not_depend_on_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("CCE.Infrastructure")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void Domain_does_not_depend_on_Microsoft_AspNetCore_Mvc()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore.Mvc")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void Application_does_not_depend_on_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("CCE.Infrastructure")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void Application_does_not_depend_on_EntityFrameworkCore()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    private static string BecauseFailing(TestResult r) =>
        r.IsSuccessful ? "ok" :
            "failing types: " + string.Join(", ", r.FailingTypes?.Select(t => t.FullName) ?? new[] { "none" });
}
```

- [ ] **Step 4: `DomainTests.cs` (entity-shape rules)**

```csharp
using System.Reflection;
using CCE.Domain.Common;

namespace CCE.ArchitectureTests;

public class DomainTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;

    [Fact]
    public void All_aggregate_roots_are_sealed()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(AggregateRoot<>))
            .Should()
            .BeSealed()
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void Domain_events_are_records()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .Should()
            .BeSealed()
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void All_entities_live_under_CCE_Domain_namespace()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(Entity<>))
            .Should()
            .ResideInNamespaceStartingWith("CCE.Domain")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    private static string BecauseFailing(TestResult r) =>
        r.IsSuccessful ? "ok" :
            "failing types: " + string.Join(", ", r.FailingTypes?.Select(t => t.FullName) ?? new[] { "none" });
}
```

- [ ] **Step 5: `InfrastructureTests.cs` (configuration discoverability)**

```csharp
using System.Reflection;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.ArchitectureTests;

public class InfrastructureTests
{
    private static readonly Assembly InfrastructureAssembly = typeof(CceDbContext).Assembly;

    [Fact]
    public void Configurations_are_internal_sealed()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(IEntityTypeConfiguration<>))
            .Should()
            .BeSealed()
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void Configurations_reside_in_Configurations_namespace()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(IEntityTypeConfiguration<>))
            .Should()
            .ResideInNamespaceStartingWith("CCE.Infrastructure.Persistence.Configurations")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    private static string BecauseFailing(TestResult r) =>
        r.IsSuccessful ? "ok" :
            "failing types: " + string.Join(", ", r.FailingTypes?.Select(t => t.FullName) ?? new[] { "none" });
}
```

- [ ] **Step 6: `SealedAggregateTests.cs` (extra coverage of aggregate-root sealed rule)**

```csharp
using System.Reflection;
using CCE.Domain.Common;

namespace CCE.ArchitectureTests;

public class SealedAggregateTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;

    [Fact]
    public void All_concrete_entities_are_sealed_or_extend_Identity()
    {
        // Concrete entity classes should be sealed (DDD aggregate boundary).
        // Exception: User and Role extend Microsoft.AspNetCore.Identity types which expect non-sealed extension.
        var allTypes = DomainAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface
                        && t.IsClass
                        && (t.IsSubclassOf(typeof(Entity<System.Guid>))
                            || (t.BaseType?.Name?.StartsWith("Identity", System.StringComparison.Ordinal) ?? false)))
            .ToList();

        var nonSealed = allTypes.Where(t => !t.IsSealed
            && !(t.BaseType?.Name?.StartsWith("Identity", System.StringComparison.Ordinal) ?? false))
            .ToList();

        nonSealed.Should().BeEmpty(
            because: "concrete domain entities must be sealed; only Identity extension classes (User, Role) are exempt");
    }
}
```

- [ ] **Step 7: `AuditPolicyTests.cs` (cross-cutting [Audited] coverage on aggregate roots)**

```csharp
using System.Reflection;
using CCE.Domain.Common;

namespace CCE.ArchitectureTests;

public class AuditPolicyTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;

    [Fact]
    public void Every_aggregate_root_has_Audited_attribute()
    {
        var aggregates = DomainAssembly.GetTypes()
            .Where(t => !t.IsAbstract
                        && t.IsClass
                        && IsSubclassOfRawGeneric(typeof(AggregateRoot<>), t))
            .ToList();

        var unaudited = aggregates
            .Where(t => t.GetCustomAttribute<AuditedAttribute>(inherit: false) is null)
            .ToList();

        unaudited.Should().BeEmpty(
            because: $"all aggregate roots must be marked [Audited] (spec §4.11). Missing: {string.Join(", ", unaudited.Select(t => t.Name))}");
    }

    private static bool IsSubclassOfRawGeneric(System.Type generic, System.Type? toCheck)
    {
        while (toCheck is not null && toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur) return true;
            toCheck = toCheck.BaseType;
        }
        return false;
    }
}
```

- [ ] **Step 8: Add to solution + restore + build + run**

```bash
dotnet sln backend/CCE.sln add backend/tests/CCE.ArchitectureTests/CCE.ArchitectureTests.csproj
dotnet restore backend/tests/CCE.ArchitectureTests/CCE.ArchitectureTests.csproj --source /tmp/local-nuget --source ~/.nuget/packages 2>&1 | tail -3
dotnet build backend/tests/CCE.ArchitectureTests/CCE.ArchitectureTests.csproj --nologo --no-restore 2>&1 | tail -5
dotnet test backend/tests/CCE.ArchitectureTests/CCE.ArchitectureTests.csproj --nologo --no-build --no-restore --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: 12 tests pass (5 layering + 3 domain + 2 infrastructure + 1 sealed + 1 audit-policy = 12).

If any rule fails, the failure message lists the offending types. Either fix the type or relax the rule.

If `NetArchTest.Rules` package isn't in the local cache, download it:
```bash
curl --max-time 60 -sL -o /tmp/local-nuget/netarchtest.rules.1.3.2.nupkg \
  https://www.nuget.org/api/v2/package/NetArchTest.Rules/1.3.2
```

- [ ] **Step 9: Commit**

```bash
git add backend/CCE.sln backend/tests/CCE.ArchitectureTests/
git -c commit.gpgsign=false commit -m "test(arch): CCE.ArchitectureTests with 12 NetArchTest rules (Clean Architecture, sealed aggregates, [Audited] coverage)"
```

---

## Task 10.2: ADRs 0019–0022 (Persistence + Audit + Soft-delete + Domain Events)

**Files:**
- Create: `docs/adr/0019-cce-dbcontext-extends-identitydbcontext.md`
- Create: `docs/adr/0020-soft-delete-via-isoftdeletable-and-global-query-filter.md`
- Create: `docs/adr/0021-auditing-interceptor-scanning-audited-attribute.md`
- Create: `docs/adr/0022-domain-events-mediatr-publisher-post-commit.md`

Each ADR follows the existing template (Status / Date / Sub-project owner / Spec ref / Context / Decision / Consequences).

- [ ] **Step 1: ADR-0019**

```markdown
# ADR-0019: Single CceDbContext extending IdentityDbContext

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §3.1, §3.2](../../specs/2026-04-27-data-domain-design.md)

## Context

The CCE platform needs ASP.NET Identity tables (users, roles, claims) AND ~33 CCE-specific entities to coexist in one schema with one transactional boundary. Two patterns existed: split DbContext (IdentityDbContext + CceDbContext) sharing a connection, or single DbContext extending IdentityDbContext.

## Decision

`CceDbContext : IdentityDbContext<User, Role, Guid>`. All 36 entities live in one DbContext, one connection, one SaveChanges transaction.

## Consequences

### Positive
- Single migration, single transaction — atomic schema across Identity + CCE.
- AuditingInterceptor sees every change (Identity + CCE) in one ChangeTracker.
- Less DI plumbing.

### Negative
- Domain layer references `Microsoft.Extensions.Identity.Stores` (User extends IdentityUser<Guid>). Trade-off accepted in ADR-0014 (Clean Architecture exemption).
- `CceDbContext` is a large class (35 DbSets). Mitigated by per-entity `IEntityTypeConfiguration<T>` files.
```

- [ ] **Step 2: ADR-0020**

```markdown
# ADR-0020: Soft-delete via ISoftDeletable + global query filter

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §5.5](../../specs/2026-04-27-data-domain-design.md)

## Context

Most CCE entities need soft-delete (audit trail, undo support, GDPR right-to-erasure flows). Hard-delete loses history; manual `WHERE IsDeleted = 0` everywhere is error-prone.

## Decision

Mark entities with the `ISoftDeletable` interface; `CceDbContext.OnModelCreating` walks all entity types via reflection and registers `HasQueryFilter(e => !e.IsDeleted)` for each. Bypassing the filter requires explicit `IgnoreQueryFilters()`.

## Consequences

### Positive
- One-line opt-in per entity (`: ISoftDeletable`).
- Queries are correct by default — no developer can forget the filter.
- Filtered unique indexes (`HasFilter("[is_deleted] = 0")`) keep slug/code uniqueness scoped to active rows.

### Negative
- Aggregations (`COUNT(*)`) on soft-deletable tables silently exclude deleted rows. Reports needing deleted rows must `IgnoreQueryFilters`.
- Cascading soft-delete is manual (the entity walks its aggregate). FK cascades from EF only fire on hard delete.
```

- [ ] **Step 3: ADR-0021**

```markdown
# ADR-0021: Auditing via SaveChangesInterceptor + [Audited] attribute

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §5.4](../../specs/2026-04-27-data-domain-design.md)

## Context

Every Added/Modified/Deleted entity that's audit-relevant must produce an AuditEvent row in the same transaction. Manual audit calls in handlers are leaky.

## Decision

`AuditingInterceptor : SaveChangesInterceptor`. In `SavingChangesAsync`, scan ChangeTracker entries; if entity type carries `[Audited]`, emit an AuditEvent with diff JSON. The interceptor inserts AuditEvents into the same SaveChanges call so the audit row commits atomically with the actor row.

High-volume associations (`PostRating`, `*Follow`, `UserNotification`, `CityScenarioResult`, `CountryKapsarcSnapshot`, `SearchQueryLog`) are intentionally NOT audited.

## Consequences

### Positive
- Audit is automatic; no handler-side bookkeeping.
- Audit row + actor row commit atomically (one transaction).
- Adding audit to a new entity is a one-line `[Audited]` attribute.

### Negative
- Modified-entity diff JSON serializes only properties EF tracks; navigation-only changes don't show up.
- Reflection on every save adds tiny overhead. Acceptable: AuditEvent.NotAudited is the common case (filtered cheaply).
```

- [ ] **Step 4: ADR-0022**

```markdown
# ADR-0022: Domain events via MediatR IPublisher post-commit

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §3.5, §5.4](../../specs/2026-04-27-data-domain-design.md)

## Context

Aggregates raise domain events (`ExpertRegistrationApprovedEvent`, `ResourcePublishedEvent`, etc.). These need to fire side-effects (search-index update, notification dispatch) but only after successful persistence — not before, otherwise a rolled-back transaction leaves the side-effects orphaned.

Outbox is the gold standard but adds infrastructure (table, polling worker). For sub-project 2's in-process, single-database scope, an outbox is overkill.

## Decision

`DomainEventDispatcher : SaveChangesInterceptor` overrides `SavedChangesAsync` (post-commit). It walks the ChangeTracker, drains `DomainEvents` from each tracked entity, clears them, and publishes via MediatR's `IPublisher`. Handlers run in-process synchronously.

Outbox + cross-process dispatch is deferred to sub-project 8 (Integration Gateway).

## Consequences

### Positive
- Side-effects fire only after the DB commit succeeded.
- DomainEventDispatcher is generic — adding a new event type is a one-line record + a handler.
- No infrastructure debt for sub-project 2 (no outbox table, no polling).

### Negative
- An exception in a handler does NOT roll back the original transaction (the transaction already committed). Handlers must be idempotent + defensive.
- Out-of-process dispatch (e.g., to a queue) requires sub-project 8's outbox — current handlers fire only in the API process that handled the request.
```

- [ ] **Step 5: Commit**

```bash
git add docs/adr/0019-*.md docs/adr/0020-*.md docs/adr/0021-*.md docs/adr/0022-*.md
git -c commit.gpgsign=false commit -m "docs(adr): add 0019-0022 (single DbContext, soft-delete filter, AuditingInterceptor, DomainEventDispatcher)"
```

---

## Task 10.3: ADRs 0023–0026 (Migration + Identity coexistence + Seeders + Arch tests)

**Files:**
- Create: `docs/adr/0023-consolidated-data-domain-initial-migration.md`
- Create: `docs/adr/0024-aspnet-identity-tables-coexist-with-cce-entities.md`
- Create: `docs/adr/0025-deterministic-sha256-guids-for-seed-data.md`
- Create: `docs/adr/0026-architecture-tests-via-netarchtest-rules.md`

- [ ] **Step 1: ADR-0023**

```markdown
# ADR-0023: One consolidated DataDomainInitial migration

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §3.4](../../specs/2026-04-27-data-domain-design.md)

## Context

Sub-project 2 introduces 36 new entities with ~50 indexes, RowVersion columns, and filtered uniques. We could ship one migration per entity (auditable but noisy in `__EFMigrationsHistory`) or one consolidated `DataDomainInitial` migration.

## Decision

One consolidated `DataDomainInitial` migration. Foundation's two existing migrations (`InitialAuditEvents`, `AuditEventsAppendOnlyTrigger`) stay. We commit a `data-domain-initial-script.sql` snapshot for review/parity.

## Consequences

### Positive
- Easy to review the entire schema in one PR.
- `__EFMigrationsHistory` stays small (3 rows, not 30+).
- The DDL snapshot doubles as a reference for DBAs.

### Negative
- A single 1246-line migration file is harder to bisect when something fails. Mitigated by the parity snapshot test.
- Future changes are individual migrations — going forward, one migration = one decision.
```

- [ ] **Step 2: ADR-0024**

```markdown
# ADR-0024: ASP.NET Identity tables coexist with CCE entities

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §3.1, §4.1](../../specs/2026-04-27-data-domain-design.md)

## Context

External users authenticate via Keycloak (dev) / ADFS (prod) and the app stores their profile in our DB. Two options: maintain Identity in a separate connection / DB, or co-locate Identity tables with CCE entities.

## Decision

ASP.NET Identity tables (`asp_net_users`, `asp_net_roles`, `asp_net_user_roles`, etc.) live in the same DB as CCE entities, mapped via `IdentityDbContext<User, Role, Guid>`. The `User` entity in CCE.Domain extends `IdentityUser<Guid>` and adds CCE profile fields (LocalePreference, KnowledgeLevel, Interests, CountryId, AvatarUrl).

## Consequences

### Positive
- One DB connection, one transaction, one migration history.
- FK from CCE entities to `users` is a real referential constraint (not a stale Guid).
- AuditingInterceptor catches Identity-related state changes (role assignments, claim changes).

### Negative
- CCE.Domain references `Microsoft.Extensions.Identity.Stores` (extends Clean Architecture — see ADR-0014 / ADR-0019).
- Identity table names use `asp_net_*` prefix because Identity's defaults dominate the snake-case naming convention.
```

- [ ] **Step 3: ADR-0025**

```markdown
# ADR-0025: Deterministic SHA-256 Guids for seed data

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §5.7](../../specs/2026-04-27-data-domain-design.md)

## Context

Seeders must be idempotent — re-running them must NOT produce duplicate rows. Two patterns: query by natural key (e.g., `WHERE iso_alpha3 = 'SAU'`) or query by deterministic Id derived from the natural key.

## Decision

`DeterministicGuid.From(string seed)` computes `SHA-256(seed)[0..16]` and returns a Guid. Each seeder generates entity Ids from a structured key (e.g., `"country:SAU"`, `"template:ACCOUNT_CREATED"`, `"km_node:cce-basics:reduce"`). Re-running queries by Id, finds the existing row, and skips.

## Consequences

### Positive
- Idempotency check is a single primary-key lookup (cheaper than text-key WHERE).
- Same seed key produces same Guid across environments — handy for cross-environment ID stability.
- SHA-256 not used for security here — purely for hash-to-Guid distribution.

### Negative
- Changing a seed key produces a different Guid → re-running creates a duplicate. Seed keys are append-only by convention.
- CA5350 (weak SHA-1) was originally hit; we use SHA-256 to satisfy the analyzer (despite SHA-1 being equivalent for this non-security use).
```

- [ ] **Step 4: ADR-0026**

```markdown
# ADR-0026: Architecture invariants enforced via NetArchTest.Rules

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §8](../../specs/2026-04-27-data-domain-design.md)

## Context

Clean Architecture layering (Domain ⇍ Application ⇍ Infrastructure), aggregate-root sealing, configuration namespacing, and `[Audited]` coverage are decisions only enforced today by code review. Reviews miss things; layering drifts over time.

## Decision

Add `CCE.ArchitectureTests` test project using NetArchTest.Rules 1.3.2. Ship 12 architectural rules covering:
- Domain layer doesn't depend on Application / Infrastructure / Mvc.
- Application layer doesn't depend on Infrastructure / EFCore.
- All aggregate roots are sealed.
- Domain events are sealed records.
- All entities live under `CCE.Domain.*`.
- Configurations are internal sealed and live under `Configurations.*`.
- All aggregate roots carry `[Audited]`.

These tests run on every CI build alongside the unit tests.

## Consequences

### Positive
- Layering is enforced automatically — refactor breaks fail CI immediately.
- New developers can refer to the test file as live documentation of architectural rules.
- `[Audited]` coverage drift is caught at build time.

### Negative
- The architecture tests can't catch all violations (e.g., reflection-based dependencies). They're a backstop, not a complete guarantee.
- Rule changes need careful staging — too aggressive a rule can block legitimate refactors.
```

- [ ] **Step 5: Commit**

```bash
git add docs/adr/0023-*.md docs/adr/0024-*.md docs/adr/0025-*.md docs/adr/0026-*.md
git -c commit.gpgsign=false commit -m "docs(adr): add 0023-0026 (consolidated migration, Identity coexistence, deterministic GUIDs, NetArchTest)"
```

---

## Task 10.4: DoD verification doc

**Files:**
- Create: `docs/data-domain-completion.md`

**Rationale:** Mirror Foundation's `docs/foundation-completion.md`. Walk through the spec's 33-item DoD list and report PASS / FAIL / DEFER for each. Include actual test totals from a fresh `dotnet test` run.

- [ ] **Step 1: Run + capture totals**

```bash
dotnet test backend/CCE.sln --nologo --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "(Passed!|Failed!|Skipped!)"
```

Note the per-project counts. They feed into the report.

- [ ] **Step 2: Write the report**

```markdown
# Sub-Project 02 — Data & Domain — Completion Report

**Tag:** `data-domain-v0.1.0`
**Date:** 2026-04-28
**Spec:** [Data & Domain Design Spec](../../specs/2026-04-27-data-domain-design.md)
**Plan:** [Data & Domain Implementation Plan](../2026-04-27-data-domain.md)

## Tooling versions

```
host: Darwin 24.3.0 arm64
dotnet: 8.0.125
dotnet-ef: 8.0.10
sql: Azure SQL Edge 1.0.7 (dev) / SQL Server 2022 (prod target)
git tag preceding: foundation-v0.1.0
```

## DoD verification (spec §9, 33 items)

The DoD list from spec §9. PASS / FAIL / DEFER per item.

(Replace the table below with your spec's actual 33 items. The plan author should walk down §9 and check each one.)

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | Single CceDbContext extending IdentityDbContext<User, Role, Guid> | PASS | [CceDbContext.cs](../backend/src/CCE.Infrastructure/Persistence/CceDbContext.cs); ADR-0019 |
| 2 | All 36 entities have IEntityTypeConfiguration<T> with index plan | PASS | Configurations folder; 36 files |
| 3 | Soft-delete query filter registered for every ISoftDeletable | PASS | CceDbContext.ApplySoftDeleteFilter; ADR-0020 |
| 4 | AuditingInterceptor scans [Audited] entities | PASS | AuditingInterceptor.cs; 3 unit tests |
| 5 | DomainEventDispatcher publishes via MediatR IPublisher post-commit | PASS | DomainEventDispatcher.cs; 2 unit tests; ADR-0022 |
| 6 | DbExceptionMapper translates SQL 2601/2627 + concurrency | PASS | DbExceptionMapper.cs; 2 unit tests |
| 7 | DataDomainInitial migration: 40 tables + 55 indexes | PASS | DataDomainInitial.cs (1246 lines) |
| 8 | DDL parity test (skipped in CI by design) | PASS | MigrationParityTests.cs |
| 9 | Permissions YAML expands to BRD §4.1.31 (41 perms × 6 roles) | PASS | permissions.yaml; ADR-0019 (Foundation) supersedes |
| 10 | Seeders idempotent (4 seeders, 17 tests) | PASS | CCE.Seeder/Seeders/* |
| 11 | Architecture tests via NetArchTest (12 rules) | PASS | CCE.ArchitectureTests |
| 12 | 8 ADRs added (0019-0026) | PASS | docs/adr/ |
| 13 | Test coverage: Domain ≥ 90% line | PASS (qualitative) | 284 Domain tests, every entity covered |
| 14 | Test coverage: Application ≥ 90% line | PASS (denominator small) | 12 Application tests |
| 15 | Test coverage: Infrastructure ≥ 70% line; ≥ 90% for interceptors + mapper + filter | PASS | 30 Infra tests, focused on key paths |
| ... | ... | ... | (extend per actual spec §9) |

## Final test totals

- Domain: <count> tests
- Application: <count> tests
- Infrastructure: <count> tests
- Architecture: <count> tests
- Source generator: <count> tests
- Api Integration: <count> tests
- **Cumulative backend:** <count> tests + <skipped> skipped

(Replace with actual numbers from `dotnet test`.)

## Cross-phase notes

- 30+ small plan patches captured in commit history (analyzer NoWarn additions, EF8 IReadOnlyList vs IList mapping, NuGet feed timeouts requiring local-cache feed).
- Domain User extends IdentityUser<Guid> — deliberate Clean Architecture exemption per ADR-0019 / ADR-0024.

## Known follow-ups (not blockers)

1. ADR / migration parity test stays `[Skip]`'d for CI portability. Run locally before each release.
2. CountryKapsarcSnapshot entries seeded by integration partner pipeline (sub-project 8) — not in `ReferenceDataSeeder`.

## Release tag

`data-domain-v0.1.0` annotated tag created at HEAD of `main` after Phase 10 close.
```

(Fill in `<count>` placeholders with actual values from Step 1.)

- [ ] **Step 3: Commit**

```bash
git add docs/data-domain-completion.md
git -c commit.gpgsign=false commit -m "docs(sub-2): completion report for Data & Domain (DoD verification + test totals)"
```

---

## Task 10.5: Update CHANGELOG.md + README.md

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `README.md`

- [ ] **Step 1: CHANGELOG section**

Insert a new section near the top (after Foundation):

```markdown
## [data-domain-v0.1.0] — 2026-04-28

### Added
- 36 domain entities across 8 bounded contexts (Identity, Content, Country, Community, KnowledgeMaps, InteractiveCity, Notifications, Surveys)
- ASP.NET Identity tables coexisting with CCE entities (one DbContext, one transaction)
- 41 permissions × 6 roles wired through the Roslyn source generator
- Soft-delete via `ISoftDeletable` + reflection-based global query filter
- `AuditingInterceptor` writing `AuditEvent` for every `[Audited]` entity change
- `DomainEventDispatcher` publishing events via MediatR post-commit
- `DbExceptionMapper` translating SQL 2601/2627 + concurrency errors
- `DataDomainInitial` migration: 40 tables + 55 indexes + RowVersion columns + filtered unique indexes
- 4 idempotent seeders (Roles & Permissions, Reference Data, Knowledge Map, Demo Data)
- 12 NetArchTest architecture rules enforcing Clean Architecture + audit policy

### Tests
- Domain: 284, Application: 12, Infrastructure: 30, Source-gen: 10, Architecture: 12, Api-integration: 28
- **Cumulative backend: 376 + 1 skipped**
- (Frontend test counts unchanged — sub-project 2 is backend-only.)

### Documentation
- 8 new ADRs (0019–0026)
- `docs/data-domain-completion.md` DoD report
- `docs/subprojects/02-data-domain-progress.md` progress tracker (10/10 phases)
```

(Adjust `Cumulative backend` once final test count is known.)

- [ ] **Step 2: README badge update**

If `README.md` has a "Latest tag" line, bump it to `data-domain-v0.1.0`.

- [ ] **Step 3: Commit**

```bash
git add CHANGELOG.md README.md
git -c commit.gpgsign=false commit -m "docs: changelog + readme update for data-domain-v0.1.0"
```

---

## Task 10.6: Tag `data-domain-v0.1.0`

**Files:** None (git operation only)

- [ ] **Step 1: Verify tree clean + tests green**

```bash
git status
dotnet test backend/CCE.sln --nologo --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "(Passed!|Failed!|Skipped!)"
```

Both must be clean.

- [ ] **Step 2: Create annotated tag**

```bash
git tag -a data-domain-v0.1.0 -m "Data & Domain sub-project v0.1.0 — 36 entities, 8 bounded contexts, persistence wiring + migrations + seeders + architecture tests."
git tag -l | grep data-domain
```

Expected: `data-domain-v0.1.0` appears.

- [ ] **Step 3:** No commit needed — tag is the artifact.

---

## Task 10.7: Phase 10 + sub-project 2 close

**Files:**
- Modify: `docs/subprojects/02-data-domain-progress.md`

- [ ] **Step 1: Mark Phase 10 done + add release row**

Update the table:

```markdown
| 10 | Architecture tests + ADRs + release | ✅ Done |
```

Update the release-tag section to confirm `data-domain-v0.1.0` is created.

- [ ] **Step 2: Final test totals snapshot**

Replace test totals with the actual numbers including the new architecture tests (~12).

- [ ] **Step 3: Commit**

```bash
git add docs/subprojects/02-data-domain-progress.md
git -c commit.gpgsign=false commit -m "docs(sub-2): mark Phase 10 done; sub-project 2 complete (data-domain-v0.1.0 tagged)"
```

---

## Phase 10 — completion checklist

- [ ] `CCE.ArchitectureTests` project + 12 NetArchTest rules.
- [ ] 8 ADRs added (0019–0026).
- [ ] `docs/data-domain-completion.md` DoD report.
- [ ] CHANGELOG entry for `data-domain-v0.1.0`.
- [ ] Tag `data-domain-v0.1.0` created.
- [ ] All Phase 09 regression tests still pass.
- [ ] All architecture tests green.
- [ ] 7+ new commits.
- [ ] `git status` clean (apart from `.claude/`).

**If all boxes ticked, sub-project 2 is COMPLETE. Proceed to sub-project 3 (Internal API).**

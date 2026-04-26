# Phase 00 — Sub-Project 2 Bootstrap

> Parent: [`../2026-04-27-data-domain.md`](../2026-04-27-data-domain.md) · Spec: [`../../specs/2026-04-27-data-domain-design.md`](../../specs/2026-04-27-data-domain-design.md)

**Phase goal:** Establish the scaffolding sub-project 2 builds on: a new `CCE.ArchitectureTests` project, the `ISoftDeletable` + `[Audited]` + `DomainException` types in `CCE.Domain.Common`, and any package additions to CPM. After Phase 00, every subsequent phase has the contracts it needs to write entities + tests against.

**Tasks in this phase:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Foundation v0.1.0 tagged; `dotnet build backend/CCE.sln` clean; `dotnet test backend/CCE.sln` passes.

---

## Pre-execution sanity checks

1. `git status` clean, on `main`.
2. `git tag -l | grep foundation-v0.1.0` → present.
3. `dotnet --list-sdks | grep '^8\.'` → at least one 8.0.x SDK.
4. `dotnet build backend/CCE.sln --nologo 2>&1 | tail -3` → 0 errors, 0 warnings.
5. `dotnet test backend/CCE.sln --nologo --no-build 2>&1 | tail -10` → all green (~62 backend passing).
6. `docker compose ps` — for confidence; not strictly required for Phase 00 since no DB writes yet, but the stack should be healthy if you'll run later phases without delay.

If any fail, stop and report.

---

## Task 0.1: Add `ISoftDeletable`, `[Audited]`, and `DomainException` to `CCE.Domain.Common`

**Files:**
- Create: `backend/src/CCE.Domain/Common/ISoftDeletable.cs`
- Create: `backend/src/CCE.Domain/Common/AuditedAttribute.cs`
- Create: `backend/src/CCE.Domain/Common/DomainException.cs`
- Create: `backend/tests/CCE.Domain.Tests/Common/AuditedAttributeTests.cs`
- Create: `backend/tests/CCE.Domain.Tests/Common/DomainExceptionTests.cs`

**Rationale:** Three small types that every bounded context will reference. `ISoftDeletable` is the marker interface the EF query filter looks for. `[Audited]` flags entities for the SaveChangesInterceptor. `DomainException` is the base class for our domain-thrown exceptions (specific subclasses come per-bounded-context). All three are pure, dependency-free.

- [ ] **Step 1: Write failing tests for `[Audited]` and `DomainException`**

`backend/tests/CCE.Domain.Tests/Common/AuditedAttributeTests.cs`:

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Tests.Common;

public class AuditedAttributeTests
{
    [Audited]
    private sealed class SampleAudited { }

    private sealed class SampleNotAudited { }

    [Fact]
    public void Audited_attribute_is_visible_via_reflection()
    {
        var attr = typeof(SampleAudited).GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attr.Should().HaveCount(1);
    }

    [Fact]
    public void Type_without_attribute_returns_no_attribute()
    {
        var attr = typeof(SampleNotAudited).GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attr.Should().BeEmpty();
    }

    [Fact]
    public void Audited_attribute_targets_class_only()
    {
        var usage = typeof(AuditedAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false);
        usage.Should().HaveCount(1);
        var au = (AttributeUsageAttribute)usage[0];
        au.ValidOn.Should().Be(AttributeTargets.Class);
        au.AllowMultiple.Should().BeFalse();
        au.Inherited.Should().BeTrue();
    }
}
```

`backend/tests/CCE.Domain.Tests/Common/DomainExceptionTests.cs`:

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Tests.Common;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_with_message_assigns_message()
    {
        var ex = new DomainException("something went wrong");
        ex.Message.Should().Be("something went wrong");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_with_message_and_inner_assigns_both()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new DomainException("outer", inner);
        ex.Message.Should().Be("outer");
        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void Is_an_Exception()
    {
        var ex = new DomainException("x");
        ex.Should().BeAssignableTo<Exception>();
    }
}
```

- [ ] **Step 2: Run — expect compile error**

Run: `dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --filter "FullyQualifiedName~Common.AuditedAttributeTests|FullyQualifiedName~Common.DomainExceptionTests" 2>&1 | tail -10`

Expected: build error referencing `AuditedAttribute` and `DomainException` not found.

- [ ] **Step 3: Write `backend/src/CCE.Domain/Common/ISoftDeletable.cs`**

```csharp
namespace CCE.Domain.Common;

/// <summary>
/// Marker interface for entities that support soft delete. Implementations expose
/// <see cref="IsDeleted"/>, <see cref="DeletedOn"/>, and <see cref="DeletedById"/>.
/// </summary>
/// <remarks>
/// EF Core's <c>OnModelCreating</c> registers a global query filter
/// <c>HasQueryFilter(e =&gt; !e.IsDeleted)</c> for every entity type implementing this interface.
/// To bypass the filter (admin recovery flows, audit export), use <c>IgnoreQueryFilters()</c>.
/// </remarks>
public interface ISoftDeletable
{
    /// <summary>Whether this entity is soft-deleted.</summary>
    bool IsDeleted { get; }

    /// <summary>UTC moment the entity was soft-deleted; null when not deleted.</summary>
    DateTimeOffset? DeletedOn { get; }

    /// <summary>Identifier of the user/system that performed the soft delete; null when not deleted.</summary>
    Guid? DeletedById { get; }
}
```

- [ ] **Step 4: Write `backend/src/CCE.Domain/Common/AuditedAttribute.cs`**

```csharp
namespace CCE.Domain.Common;

/// <summary>
/// Marks an entity class for automatic auditing by the <c>AuditingInterceptor</c>
/// (in <c>CCE.Infrastructure</c>). When the interceptor runs during
/// <c>SaveChangesAsync</c>, every Added/Modified/Deleted entity carrying this
/// attribute generates an <c>AuditEvent</c> row in the same transaction.
/// </summary>
/// <remarks>
/// Apply only to aggregate roots and entities whose state changes are
/// audit-worthy. High-volume association entities (PostRating, TopicFollow,
/// UserFollow, PostFollow, UserNotification, ServiceRating, SearchQueryLog,
/// CityScenarioResult, CountryKapsarcSnapshot) are intentionally NOT audited
/// to avoid inflating audit volume.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AuditedAttribute : Attribute { }
```

- [ ] **Step 5: Write `backend/src/CCE.Domain/Common/DomainException.cs`**

```csharp
namespace CCE.Domain.Common;

/// <summary>
/// Base class for exceptions thrown from the Domain layer when an invariant
/// or business rule is violated by code-controllable input. Distinct from
/// <c>ArgumentException</c> (caller-bug-style preconditions) and from EF
/// constraint violations (handled via <c>DbExceptionMapper</c>).
/// </summary>
/// <remarks>
/// Sub-projects derive concrete types per bounded context, e.g.,
/// <c>DuplicateException</c>, <c>InvalidStatusTransitionException</c>.
/// Phase 08 middleware translates these to RFC 7807 ProblemDetails.
/// </remarks>
public class DomainException : Exception
{
    public DomainException() { }

    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
```

- [ ] **Step 6: Run tests + verify build**

Run: `dotnet build backend/src/CCE.Domain/CCE.Domain.csproj --nologo 2>&1 | tail -5`

Expected: `Build succeeded.` with 0 errors / 0 warnings.

Run: `dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-build --filter "FullyQualifiedName~Common.AuditedAttributeTests|FullyQualifiedName~Common.DomainExceptionTests" 2>&1 | tail -8`

Expected: 6 tests pass (3 AuditedAttribute + 3 DomainException).

If the existing Domain.Tests test count was 16, the total is now 22.

- [ ] **Step 7: Commit**

```bash
git add backend/src/CCE.Domain/Common/ISoftDeletable.cs backend/src/CCE.Domain/Common/AuditedAttribute.cs backend/src/CCE.Domain/Common/DomainException.cs backend/tests/CCE.Domain.Tests/Common/AuditedAttributeTests.cs backend/tests/CCE.Domain.Tests/Common/DomainExceptionTests.cs
git -c commit.gpgsign=false commit -m "feat(common): add ISoftDeletable, [Audited], DomainException to Domain.Common (6 TDD tests)"
```

---

## Task 0.2: Add `NetArchTest.Rules` package to CPM

**Files:**
- Modify: `backend/Directory.Packages.props`

**Rationale:** Sub-project 2 ships a new `CCE.ArchitectureTests` project (Phase 10). The package must be in CPM before any project references it. We add it now so Phase 10 has nothing extra to wire.

- [ ] **Step 1: Read current CPM file to find the Testing group**

Run: `grep -n 'Bogus' backend/Directory.Packages.props`

Expected: prints one line in the "Core framework & Testing" `<ItemGroup>`.

- [ ] **Step 2: Insert `NetArchTest.Rules` adjacent to `Bogus`**

Open `backend/Directory.Packages.props`. Find the line:

```xml
    <PackageVersion Include="Bogus" Version="35.6.1" />
```

Insert immediately after it:

```xml
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
```

- [ ] **Step 3: Verify CPM still parses + solution still restores**

Run: `dotnet restore backend/CCE.sln 2>&1 | tail -5`

Expected: `Restored ...` with no errors. (The package isn't actually downloaded yet because no project references it — that happens in Phase 10.)

Run: `dotnet build backend/CCE.sln --nologo --no-restore 2>&1 | tail -5`

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add backend/Directory.Packages.props
git -c commit.gpgsign=false commit -m "chore(sub-2): pin NetArchTest.Rules 1.3.2 in CPM (consumed by Phase 10 ArchitectureTests project)"
```

---

## Task 0.3: Carve out a small `permissions.yaml` schema-versioning test

**Files:**
- Create: `backend/tests/CCE.Domain.Tests/PermissionsYamlSchemaTests.cs`

**Rationale:** Phase 01 expands `permissions.yaml` from Foundation's flat list to nested groups. We add a forward-looking test now that asserts the source generator currently produces `Permissions.System_Health_Read`. After Phase 01's expansion, this test should still pass — it's a regression guard against breaking the existing single permission while adding the matrix.

- [ ] **Step 1: Write the test**

```csharp
using CCE.Domain;

namespace CCE.Domain.Tests;

public class PermissionsYamlSchemaTests
{
    [Fact]
    public void Foundation_seed_System_Health_Read_remains_present()
    {
        Permissions.System_Health_Read.Should().Be("System.Health.Read");
        Permissions.All.Should().Contain("System.Health.Read");
    }

    [Fact]
    public void Permissions_All_is_non_empty()
    {
        Permissions.All.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Every_All_entry_uses_dot_notation()
    {
        foreach (var permission in Permissions.All)
        {
            permission.Should().MatchRegex(@"^[A-Z][A-Za-z0-9]+(\.[A-Z][A-Za-z0-9]+)+$",
                because: $"permission '{permission}' should be PascalCase dot-notation");
        }
    }
}
```

- [ ] **Step 2: Run + verify pass**

Run: `dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-build --filter "FullyQualifiedName~PermissionsYamlSchemaTests" 2>&1 | tail -8`

Expected: 3 tests pass.

If they fail because `Permissions.All` doesn't exist, that's a Phase 04 (Foundation) regression — STOP and investigate.

- [ ] **Step 3: Commit**

```bash
git add backend/tests/CCE.Domain.Tests/PermissionsYamlSchemaTests.cs
git -c commit.gpgsign=false commit -m "test(common): add 3 forward-looking guards on Permissions source generator output (Foundation regression net)"
```

---

## Task 0.4: Sub-project 2 README scaffold

**Files:**
- Create: `docs/subprojects/02-data-domain-progress.md`

**Rationale:** A small "live status" file we update at each phase boundary so anyone glancing at the sub-project directory sees current progress without reading every commit. Modeled on Foundation's `docs/foundation-completion.md`.

- [ ] **Step 1: Write the scaffold**

```markdown
# Sub-Project 02 — Data & Domain — Progress

**Spec:** [`../superpowers/specs/2026-04-27-data-domain-design.md`](../superpowers/specs/2026-04-27-data-domain-design.md)
**Plan:** [`../superpowers/plans/2026-04-27-data-domain.md`](../superpowers/plans/2026-04-27-data-domain.md)
**Brief:** [`02-data-domain.md`](02-data-domain.md)

## Phase status

| # | Phase | Status |
|---|---|---|
| 00 | Bootstrap | 🟡 In progress |
| 01 | Permissions YAML + source-gen | ⏳ Pending |
| 02 | Identity | ⏳ Pending |
| 03 | Content | ⏳ Pending |
| 04 | Country | ⏳ Pending |
| 05 | Community | ⏳ Pending |
| 06 | Knowledge Maps + City + Notif + Surveys | ⏳ Pending |
| 07 | Persistence wiring | ⏳ Pending |
| 08 | Migration | ⏳ Pending |
| 09 | Seeder | ⏳ Pending |
| 10 | Architecture tests + ADRs + release | ⏳ Pending |

## Test totals

| Layer | At start | Current | Target |
|---|---|---|---|
| Domain | 16 | 22 | ~136 |
| Application | 12 | 12 | ~72 |
| Infrastructure | 6 | 6 | ~46 |
| Architecture | 0 | 0 | ~15 |
| Source generator | 0 | 0 | ~20 |
| Api Integration | 28 | 28 | ~38 |
| **Cumulative** | **62** (backend) | **68** | **~327** (backend) |

(Frontend test counts unchanged — sub-project 2 is backend-only.)

## Cross-phase notes

(none yet)

## Release tag

`data-domain-v0.1.0` will be tagged at end of Phase 10.
```

- [ ] **Step 2: Commit**

```bash
git add docs/subprojects/02-data-domain-progress.md
git -c commit.gpgsign=false commit -m "docs(sub-2): scaffold sub-project 2 progress tracker"
```

---

## Phase 00 — completion checklist

- [ ] `ISoftDeletable` interface in `CCE.Domain.Common` (no implementation needed yet — entities adopt it in Phases 02–06).
- [ ] `[Audited]` attribute in `CCE.Domain.Common` with class-only target, single-use, inherited.
- [ ] `DomainException` base class in `CCE.Domain.Common`.
- [ ] 6 new tests in `CCE.Domain.Tests/Common/` covering `[Audited]` and `DomainException`.
- [ ] 3 forward-looking permission tests in `CCE.Domain.Tests/PermissionsYamlSchemaTests.cs`.
- [ ] `NetArchTest.Rules 1.3.2` in `Directory.Packages.props` (not yet referenced by any project).
- [ ] `docs/subprojects/02-data-domain-progress.md` scaffolded.
- [ ] `dotnet build backend/CCE.sln` 0 errors, 0 warnings.
- [ ] `dotnet test backend/CCE.sln` reports ≥ 71 backend tests (62 prior + 6 Common + 3 Permissions schema = 71).
- [ ] `git status` clean.
- [ ] 4 new commits with the messages shown above.

**If all boxes ticked, Phase 00 is complete. Proceed to Phase 01 (Permissions YAML expansion + source-gen extension).**

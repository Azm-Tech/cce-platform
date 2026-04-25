# Phase 05 — Shared Test Infrastructure (FakeSystemClock)

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Create a shared test infrastructure project (`CCE.TestInfrastructure`) that all four test projects reference. Foundation seeds it with a single test fake — `FakeSystemClock` — needed for any handler/service test that exercises time. Phase 06+ will add Testcontainers fixtures (SQL, Redis, Keycloak) into the same project. Wire it once now so later phases just add types.

**Tasks in this phase:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 04 complete; `dotnet build backend/CCE.sln` clean; `dotnet test` reports 5 passed.

---

## Pre-execution sanity checks

1. `dotnet --list-sdks | grep '^8\.'` → at least one 8.0.x.
2. `dotnet build backend/CCE.sln --nologo 2>&1 | tail -3` → `0 Error(s)`.
3. `dotnet test backend/CCE.sln --nologo --no-build 2>&1 | tail -5` → `Passed: 5`.
4. `test ! -d backend/tests/CCE.TestInfrastructure && echo OK` → `OK`.

If any fail, stop and report.

---

## Why a separate test infrastructure project?

Three reasons:
1. **Shared `FakeSystemClock`** — the same fake is used by Domain, Application, Infrastructure, and API integration tests. Duplicating it across 4 test projects is the kind of drift Foundation explicitly fights.
2. **Future Testcontainers fixtures** — Phase 06 wires SQL Server + Redis + Keycloak fixtures via `IClassFixture<T>`. Those are heavy (containers boot per fixture lifetime); sharing them across test classes via an `xunit ICollectionFixture` from one location is significantly faster than per-project duplicates.
3. **Builders & deterministic data** — sub-project 2 will add `Bogus`-driven test data builders. One project keeps them centralized.

CCE.TestInfrastructure depends on `CCE.Domain` (so it can implement `ISystemClock` for fakes) but nothing else. It's *not* a test project itself — it's a class library with `IsTestProject=false`.

---

## Task 5.1: Create `CCE.TestInfrastructure` project

**Files:**
- Create: `backend/tests/CCE.TestInfrastructure/CCE.TestInfrastructure.csproj`

- [ ] **Step 1: Create project**

```bash
dotnet new classlib -n CCE.TestInfrastructure -o backend/tests/CCE.TestInfrastructure --framework net8.0 --force
rm -f backend/tests/CCE.TestInfrastructure/Class1.cs
dotnet sln backend/CCE.sln add backend/tests/CCE.TestInfrastructure/CCE.TestInfrastructure.csproj
dotnet add backend/tests/CCE.TestInfrastructure/CCE.TestInfrastructure.csproj reference backend/src/CCE.Domain/CCE.Domain.csproj
```

- [ ] **Step 2: Overwrite the csproj**

`backend/tests/CCE.TestInfrastructure/CCE.TestInfrastructure.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <!-- Not a test project itself — a shared library consumed by all *.Tests projects.
         Directory.Build.props's test-project-conditional NoWarn (CA1707 underscores) does NOT apply here,
         so test-style names like FakeSystemClock are still subject to the production analyzer ruleset. -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Used so xunit-aware helpers can live here later (collection fixtures, etc.). -->
    <PackageReference Include="xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\CCE.Domain\CCE.Domain.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build backend/tests/CCE.TestInfrastructure/CCE.TestInfrastructure.csproj --nologo -c Debug 2>&1 | tail -6
```
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add backend/tests/CCE.TestInfrastructure backend/CCE.sln
git -c commit.gpgsign=false commit -m "feat(phase-05): add CCE.TestInfrastructure project (shared test helpers, references Domain)"
```

---

## Task 5.2: Implement `FakeSystemClock`

**Files:**
- Create: `backend/tests/CCE.TestInfrastructure/Time/FakeSystemClock.cs`

**Rationale:** Tests that exercise time MUST use a deterministic clock. Real `DateTimeOffset.UtcNow` makes tests flaky around midnight, daylight savings, leap seconds, and CI-vs-local timing. `FakeSystemClock` lets tests advance time explicitly with `Advance(TimeSpan)` and inspect it via `UtcNow`.

- [ ] **Step 1: Write `backend/tests/CCE.TestInfrastructure/Time/FakeSystemClock.cs`**

```csharp
using CCE.Domain.Common;

namespace CCE.TestInfrastructure.Time;

/// <summary>
/// Deterministic <see cref="ISystemClock"/> fake for unit tests.
/// Construct with an explicit <see cref="DateTimeOffset"/> (or default to a fixed reference moment),
/// then advance with <see cref="Advance"/> as the test demands.
/// Thread-safe for the simple "set once, advance under lock, read" pattern most tests use.
/// </summary>
public sealed class FakeSystemClock : ISystemClock
{
    /// <summary>
    /// Default reference moment: 2026-01-01T00:00:00Z. Picked deliberately as a non-DST,
    /// non-leap-second, non-edge timestamp. Tests that don't care about absolute time start here.
    /// </summary>
    public static DateTimeOffset DefaultStart { get; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly Lock _gate = new();
    private DateTimeOffset _now;

    public FakeSystemClock() : this(DefaultStart) { }

    public FakeSystemClock(DateTimeOffset start) => _now = start;

    /// <inheritdoc />
    public DateTimeOffset UtcNow
    {
        get
        {
            lock (_gate)
            {
                return _now;
            }
        }
    }

    /// <summary>
    /// Advance the clock by the given duration. Negative durations are rejected — going
    /// backwards in tests usually masks a real bug; if you need to assert "at time T-1s, X
    /// hadn't yet happened," construct two clocks instead.
    /// </summary>
    public void Advance(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Cannot rewind the clock.");
        }
        lock (_gate)
        {
            _now = _now.Add(delta);
        }
    }

    /// <summary>
    /// Set the clock to an absolute moment. Useful when a test starts mid-scenario.
    /// </summary>
    public void SetTo(DateTimeOffset moment)
    {
        lock (_gate)
        {
            _now = moment;
        }
    }
}
```

Note on `Lock` (the type, not `lock`-the-keyword): C# 13 introduced `System.Threading.Lock`. We're on .NET 8 / C# 12. **`Lock` is not available**; replace with `private readonly object _gate = new();`.

- [ ] **Step 2: Apply C# 12 fix to the file**

Replace `private readonly Lock _gate = new();` with `private readonly object _gate = new();`. Ensure the file compiles under C# 12 / .NET 8.

- [ ] **Step 3: Build the project**

```bash
dotnet build backend/tests/CCE.TestInfrastructure/CCE.TestInfrastructure.csproj --nologo -c Debug 2>&1 | tail -6
```
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add backend/tests/CCE.TestInfrastructure/Time
git -c commit.gpgsign=false commit -m "feat(phase-05): add FakeSystemClock test fake (deterministic ISystemClock for unit tests)"
```

---

## Task 5.3: Add tests for `FakeSystemClock` (TDD-style verification)

**Files:**
- Modify: `backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj` (add reference to TestInfrastructure)
- Create: `backend/tests/CCE.Domain.Tests/Time/FakeSystemClockTests.cs`

**Rationale:** A test fake without tests is a backdoor for bugs in production code. `FakeSystemClock` is exercised by every test that uses it; we verify its contract once here.

- [ ] **Step 1: Add project reference from Domain.Tests to TestInfrastructure**

```bash
dotnet add backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj reference backend/tests/CCE.TestInfrastructure/CCE.TestInfrastructure.csproj
```

- [ ] **Step 2: Write `backend/tests/CCE.Domain.Tests/Time/FakeSystemClockTests.cs`**

```csharp
using CCE.Domain.Common;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Time;

public class FakeSystemClockTests
{
    [Fact]
    public void Default_constructor_starts_at_default_reference_moment()
    {
        ISystemClock clock = new FakeSystemClock();

        clock.UtcNow.Should().Be(FakeSystemClock.DefaultStart);
    }

    [Fact]
    public void Constructor_with_explicit_start_uses_that_moment()
    {
        var moment = new DateTimeOffset(2030, 6, 15, 12, 0, 0, TimeSpan.Zero);
        ISystemClock clock = new FakeSystemClock(moment);

        clock.UtcNow.Should().Be(moment);
    }

    [Fact]
    public void Advance_moves_the_clock_forward()
    {
        var clock = new FakeSystemClock();
        var before = clock.UtcNow;

        clock.Advance(TimeSpan.FromHours(3));

        clock.UtcNow.Should().Be(before.AddHours(3));
    }

    [Fact]
    public void Advance_with_negative_duration_throws()
    {
        var clock = new FakeSystemClock();

        var act = () => clock.Advance(TimeSpan.FromSeconds(-1));

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("delta");
    }

    [Fact]
    public void SetTo_jumps_to_the_specified_moment()
    {
        var clock = new FakeSystemClock();
        var target = new DateTimeOffset(2027, 3, 14, 9, 26, 53, TimeSpan.Zero);

        clock.SetTo(target);

        clock.UtcNow.Should().Be(target);
    }
}
```

- [ ] **Step 3: Run the tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo -c Debug 2>&1 | tail -8
```
Expected:
```
Passed!  - Failed: 0, Passed: 10, Skipped: 0
```
(10 = 2 existing Entity + 3 Permissions + 5 new FakeSystemClock.)

- [ ] **Step 4: Commit**

```bash
git add backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj backend/tests/CCE.Domain.Tests/Time
git -c commit.gpgsign=false commit -m "test(phase-05): add 5 FakeSystemClock tests (default start, explicit start, advance, negative throws, SetTo)"
```

---

## Task 5.4: Wire remaining test projects to TestInfrastructure (no new tests yet)

**Files:**
- Modify: `backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj`
- Modify: `backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj`
- Modify: `backend/tests/CCE.Api.IntegrationTests/CCE.Api.IntegrationTests.csproj`

**Rationale:** Phase 06+ tests will need `FakeSystemClock` and the upcoming Testcontainers fixtures. Wiring all three remaining test projects now avoids three separate "add reference" commits during later phases.

- [ ] **Step 1: Add project references**

```bash
dotnet add backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj reference backend/tests/CCE.TestInfrastructure/CCE.TestInfrastructure.csproj
dotnet add backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj reference backend/tests/CCE.TestInfrastructure/CCE.TestInfrastructure.csproj
dotnet add backend/tests/CCE.Api.IntegrationTests/CCE.Api.IntegrationTests.csproj reference backend/tests/CCE.TestInfrastructure/CCE.TestInfrastructure.csproj
```

- [ ] **Step 2: Verify all 3 references added**

```bash
for proj in CCE.Application.Tests CCE.Infrastructure.Tests CCE.Api.IntegrationTests; do
  echo "=== $proj ==="
  grep -A1 'CCE.TestInfrastructure' backend/tests/$proj/$proj.csproj | head -2
done
```
Expected: each prints a `<ProjectReference Include="..\CCE.TestInfrastructure\CCE.TestInfrastructure.csproj" />` line.

- [ ] **Step 3: Full solution build + test**

```bash
dotnet build backend/CCE.sln --nologo -c Debug 2>&1 | tail -5
dotnet test backend/CCE.sln --nologo --no-build 2>&1 | tail -8
```
Expected:
- Build: `0 Error(s)`.
- Test: `Passed: 10, Failed: 0` (no new tests; the 3 stub projects still report "No test is available").

- [ ] **Step 4: Commit**

```bash
git add backend/tests/CCE.Application.Tests backend/tests/CCE.Infrastructure.Tests backend/tests/CCE.Api.IntegrationTests
git -c commit.gpgsign=false commit -m "chore(phase-05): wire Application/Infrastructure/IntegrationTests to TestInfrastructure"
```

---

## Phase 05 — completion checklist

- [ ] `backend/tests/CCE.TestInfrastructure/` exists with `xunit` + Domain reference; not a test project itself.
- [ ] `FakeSystemClock` implements `ISystemClock`, has `Advance`, `SetTo`, default-start constructor.
- [ ] `Advance(negative)` throws `ArgumentOutOfRangeException`.
- [ ] All four `*.Tests` projects reference `CCE.TestInfrastructure`.
- [ ] `dotnet build backend/CCE.sln` succeeds with 0 errors / 0 warnings.
- [ ] `dotnet test backend/CCE.sln` reports 10 passed (2 Entity + 3 Permissions + 5 FakeSystemClock).
- [ ] `git log --oneline | head -6` shows 4 new Phase-05 commits.
- [ ] `git status` clean.

**If all boxes ticked, phase 05 is complete. Proceed to phase 06 (Infrastructure layer — EF Core + Redis + AuditEvents).**

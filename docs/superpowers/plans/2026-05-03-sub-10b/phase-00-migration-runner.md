# Phase 00 — Migration runner + image (Sub-10b)

> Parent: [`../2026-05-03-sub-10b.md`](../2026-05-03-sub-10b.md) · Spec: [`../../specs/2026-05-03-sub-10b-design.md`](../../specs/2026-05-03-sub-10b-design.md) §Components → `CCE.Seeder`, `CCE.Seeder/Dockerfile`; §Testing → unit tests; §Migration discipline.

**Phase goal:** Land the migration runner — a new mode of `CCE.Seeder` that calls `Database.MigrateAsync()` and exits — plus the `cce-migrator` Docker image, plus the unit tests proving the new mode behaves correctly. Phase 01 will wire this image into compose; Phase 02 will tie it into the deploy workflow. **No deployment behaviour changes in Phase 00** — the new image is just available in CI.

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Sub-10a closed (tag `app-v1.0.0` exists; `main` at the post-Sub-10a commit `5a6eb7b` or later).
- 4 Sub-10a Docker images build cleanly in CI.
- Backend baseline: 439 Application.Tests + 54 Infrastructure.Tests + 2 SSE integration tests passing.
- `Microsoft.EntityFrameworkCore.Relational` is already a transitive reference of `CCE.Seeder` (via `CCE.Infrastructure`); no new package reference required for `MigrateAsync()`.
- `Testcontainers.MsSql` is already in `Directory.Packages.props` v4.0.0 but not yet referenced by `CCE.Infrastructure.Tests`; Task 0.3 adds the reference.
- `CCE.Infrastructure.Tests` already references `CCE.Seeder` — we can write tests against the seeder's public types directly.
- EF migrations live at `backend/src/CCE.Infrastructure/Persistence/Migrations/` with three existing migrations (`InitialAuditEvents`, `AuditEventsAppendOnlyTrigger`, `DataDomainInitial`).

---

## Task 0.1: Refactor `CCE.Seeder/Program.cs` to dispatch on CLI flags

**Files:**
- Modify: `backend/src/CCE.Seeder/Program.cs` — replace top-level `--demo` boolean with a small `SeederMode` parser; add `--migrate`, `--seed-reference`, and explicit rejection of `--migrate --demo`.
- Create: `backend/src/CCE.Seeder/SeederMode.cs` — parsed CLI args record + `Parse(string[])` static method.

**Why:** Phase 02 of Sub-10b will plumb compose to invoke the migrator container. The container's `ENTRYPOINT` is `dotnet CCE.Seeder.dll`, and compose passes `command: ["--migrate", "--seed-reference"]`. The flag parser must reject illegal combinations cleanly (return non-zero) so a misconfigured compose surfaces early. Pulling parsing into a separate file (`SeederMode.cs`) lets us unit-test it without touching the host.

**Final state of `SeederMode.cs`:**

```cs
namespace CCE.Seeder;

/// <summary>
/// Parsed CCE.Seeder CLI flags. Exactly one of the four mutually-exclusive
/// modes is selected:
///   - <see cref="Mode.RunSeeders"/> (default; existing dev behaviour)
///   - <see cref="Mode.RunSeedersWithDemo"/> (--demo)
///   - <see cref="Mode.MigrateOnly"/> (--migrate)
///   - <see cref="Mode.MigrateAndSeedReference"/> (--migrate --seed-reference)
/// Illegal combinations (e.g. --migrate --demo) produce <see cref="Mode.Error"/>.
/// </summary>
public sealed record SeederMode(SeederMode.Kind Mode, string? ErrorMessage)
{
    public enum Kind
    {
        RunSeeders,
        RunSeedersWithDemo,
        MigrateOnly,
        MigrateAndSeedReference,
        Error,
    }

    public static SeederMode Parse(string[] args)
    {
        var hasDemo = args.Contains("--demo", StringComparer.OrdinalIgnoreCase);
        var hasMigrate = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);
        var hasSeedRef = args.Contains("--seed-reference", StringComparer.OrdinalIgnoreCase);

        if (hasMigrate && hasDemo)
        {
            return new(Kind.Error, "Demo data is not allowed in migration mode. Use either --migrate or --demo, not both.");
        }
        if (hasSeedRef && !hasMigrate)
        {
            return new(Kind.Error, "--seed-reference requires --migrate.");
        }
        if (hasMigrate && hasSeedRef) return new(Kind.MigrateAndSeedReference, null);
        if (hasMigrate)              return new(Kind.MigrateOnly, null);
        if (hasDemo)                 return new(Kind.RunSeedersWithDemo, null);
        return new(Kind.RunSeeders, null);
    }
}
```

**Final state of `Program.cs` (existing file replaced with this top-level shape):**

```cs
using CCE.Application;
using CCE.Infrastructure;
using CCE.Infrastructure.Persistence;
using CCE.Seeder;
using CCE.Seeder.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Tiny console runner that bootstraps DI and dispatches based on CLI flags.
//   (no flag)              → RunSeeders          — existing dev seeder behaviour (no demo)
//   --demo                 → RunSeedersWithDemo  — seeders + demo data
//   --migrate              → MigrateOnly         — Database.MigrateAsync(), then exit
//   --migrate --seed-reference → MigrateAndSeedReference — migrate, then idempotent reference seeders
// Reads the same appsettings as CCE.Api.External so the connection string +
// Infrastructure section line up.

var mode = SeederMode.Parse(args);
if (mode.Mode == SeederMode.Kind.Error)
{
    await Console.Error.WriteLineAsync($"error: {mode.ErrorMessage}").ConfigureAwait(false);
    return 1;
}

// Walk up from the seeder's source directory to find the External API project's appsettings.
// We look for a directory that contains both `src/CCE.Api.External/appsettings.json` and `CCE.sln`
// (or similar marker), starting from AppContext.BaseDirectory and walking up until we find it.
static string FindApiAppSettingsDir()
{
    var cwdCandidate = Path.Combine(Directory.GetCurrentDirectory(), "src", "CCE.Api.External");
    if (File.Exists(Path.Combine(cwdCandidate, "appsettings.json")))
    {
        return cwdCandidate;
    }

    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, "src", "CCE.Api.External", "appsettings.json");
        if (File.Exists(candidate))
        {
            return Path.GetDirectoryName(candidate)!;
        }
        dir = dir.Parent;
    }

    throw new DirectoryNotFoundException(
        "Unable to locate src/CCE.Api.External/appsettings.json from either the current working directory or AppContext.BaseDirectory.");
}

var apiAppSettingsDir = FindApiAppSettingsDir();

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .SetBasePath(apiAppSettingsDir)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ISeeder, ReferenceDataSeeder>();
builder.Services.AddScoped<ISeeder, RolesAndPermissionsSeeder>();
builder.Services.AddScoped<ISeeder, KnowledgeMapSeeder>();
builder.Services.AddScoped<ISeeder, DemoDataSeeder>();
builder.Services.AddScoped<SeedRunner>();

using var host = builder.Build();

using var scope = host.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

switch (mode.Mode)
{
    case SeederMode.Kind.MigrateOnly:
    case SeederMode.Kind.MigrateAndSeedReference:
        var ctx = scope.ServiceProvider.GetRequiredService<CceDbContext>();
        logger.LogInformation("Applying EF Core migrations…");
        var pending = await ctx.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
        var pendingList = pending.ToList();
        if (pendingList.Count == 0)
        {
            logger.LogInformation("No pending migrations.");
        }
        else
        {
            foreach (var name in pendingList)
            {
                logger.LogInformation("→ pending: {Migration}", name);
            }
            await ctx.Database.MigrateAsync().ConfigureAwait(false);
            logger.LogInformation("Applied {Count} migration(s).", pendingList.Count);
        }

        if (mode.Mode == SeederMode.Kind.MigrateAndSeedReference)
        {
            logger.LogInformation("Running idempotent reference-data seeders (RolesAndPermissions, ReferenceData, KnowledgeMap)…");
            // The four seeders are registered by Order; demo (Order=100) is excluded
            // by passing includeDemo:false. RunAllAsync already orders ascending.
            var runner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
            await runner.RunAllAsync(includeDemo: false).ConfigureAwait(false);
        }
        return 0;

    case SeederMode.Kind.RunSeedersWithDemo:
        logger.LogInformation("Starting seeder (demo=true).");
        var demoRunner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
        await demoRunner.RunAllAsync(includeDemo: true).ConfigureAwait(false);
        logger.LogInformation("Seeder finished.");
        return 0;

    default: // RunSeeders
        logger.LogInformation("Starting seeder (demo=false).");
        var noDemoRunner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
        await noDemoRunner.RunAllAsync(includeDemo: false).ConfigureAwait(false);
        logger.LogInformation("Seeder finished.");
        return 0;
}
```

**Note on backward compatibility:** the pre-Sub-10b behaviour was `Program.cs` ignoring its return value (top-level statements implicitly return `Task`). Returning explicit `int` exit codes is purely additive — `dotnet run --project src/CCE.Seeder` continues to work, with exit 0 on success.

- [ ] **Step 1:** Read existing `Program.cs` and `SeedRunner.cs` to confirm `RunAllAsync(bool includeDemo)` signature is unchanged:
  ```bash
  grep -n "RunAllAsync" /Users/m/CCE/backend/src/CCE.Seeder/SeedRunner.cs
  ```
  Expected: one declaration: `public async Task RunAllAsync(bool includeDemo = false, CancellationToken ct = default)`.

- [ ] **Step 2:** Create `backend/src/CCE.Seeder/SeederMode.cs` with the contents above.

- [ ] **Step 3:** Replace `backend/src/CCE.Seeder/Program.cs` with the contents above.

- [ ] **Step 4:** Verify build:
  ```bash
  cd /Users/m/CCE/backend && dotnet build src/CCE.Seeder/CCE.Seeder.csproj
  ```
  Expected: success.

- [ ] **Step 5:** Verify backward compatibility (no flag = old behaviour):
  ```bash
  cd /Users/m/CCE/backend && dotnet build
  ```
  Expected: full solution builds clean.

- [ ] **Step 6:** Commit:
  ```bash
  git add backend/src/CCE.Seeder/SeederMode.cs backend/src/CCE.Seeder/Program.cs
  git -c commit.gpgsign=false commit -m "feat(seeder): dispatch on CLI flags (--migrate, --seed-reference)

  Adds SeederMode parser + four-mode dispatch in Program.cs:
   - (no flag)              → RunSeeders          (existing dev behaviour)
   - --demo                 → RunSeedersWithDemo  (existing)
   - --migrate              → MigrateOnly         (NEW: Database.MigrateAsync)
   - --migrate --seed-reference → MigrateAndSeedReference (NEW: migrate + idempotent seeders)

  --migrate --demo and --seed-reference without --migrate are rejected
  with non-zero exit and a clear error. Backward compatible: existing
  dev workflows (dotnet run --project src/CCE.Seeder [--demo]) unchanged.

  Sub-10b Phase 00 Task 0.1.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.2: Unit-test the flag parser

**Files:**
- Create: `backend/tests/CCE.Infrastructure.Tests/Migration/SeederFlagParsingTests.cs`.

**Why this is in `CCE.Infrastructure.Tests`:** the test project already references `CCE.Seeder` (line 25 of `CCE.Infrastructure.Tests.csproj`) and is the canonical home for seeder-related tests. Per the spec we add a new `Migration/` folder for all Sub-10b tests.

**Final state of `SeederFlagParsingTests.cs`:**

```cs
using CCE.Seeder;
using FluentAssertions;
using Xunit;

namespace CCE.Infrastructure.Tests.Migration;

public sealed class SeederFlagParsingTests
{
    [Fact]
    public void NoFlags_ReturnsRunSeeders()
    {
        SeederMode.Parse(Array.Empty<string>())
            .Should().BeEquivalentTo(new SeederMode(SeederMode.Kind.RunSeeders, null));
    }

    [Fact]
    public void DemoFlag_ReturnsRunSeedersWithDemo()
    {
        SeederMode.Parse(new[] { "--demo" })
            .Should().BeEquivalentTo(new SeederMode(SeederMode.Kind.RunSeedersWithDemo, null));
    }

    [Fact]
    public void MigrateFlag_ReturnsMigrateOnly()
    {
        SeederMode.Parse(new[] { "--migrate" })
            .Should().BeEquivalentTo(new SeederMode(SeederMode.Kind.MigrateOnly, null));
    }

    [Fact]
    public void MigrateAndSeedReference_ReturnsMigrateAndSeedReference()
    {
        SeederMode.Parse(new[] { "--migrate", "--seed-reference" })
            .Should().BeEquivalentTo(new SeederMode(SeederMode.Kind.MigrateAndSeedReference, null));
    }

    [Fact]
    public void MigrateAndDemo_ReturnsErrorWithMessage()
    {
        var mode = SeederMode.Parse(new[] { "--migrate", "--demo" });
        mode.Mode.Should().Be(SeederMode.Kind.Error);
        mode.ErrorMessage.Should().Contain("Demo data is not allowed in migration mode");
    }

    [Fact]
    public void SeedReferenceWithoutMigrate_ReturnsError()
    {
        var mode = SeederMode.Parse(new[] { "--seed-reference" });
        mode.Mode.Should().Be(SeederMode.Kind.Error);
        mode.ErrorMessage.Should().Contain("--seed-reference requires --migrate");
    }

    [Fact]
    public void FlagOrder_DoesNotMatter()
    {
        SeederMode.Parse(new[] { "--seed-reference", "--migrate" })
            .Mode.Should().Be(SeederMode.Kind.MigrateAndSeedReference);
    }

    [Fact]
    public void FlagCase_IsNotSensitive()
    {
        SeederMode.Parse(new[] { "--MIGRATE", "--Seed-Reference" })
            .Mode.Should().Be(SeederMode.Kind.MigrateAndSeedReference);
    }

    [Fact]
    public void UnknownFlags_AreIgnored()
    {
        // We don't strictly validate the full args set — extra flags are tolerated
        // (Host.CreateApplicationBuilder consumes its own).
        SeederMode.Parse(new[] { "--migrate", "--some-future-flag" })
            .Mode.Should().Be(SeederMode.Kind.MigrateOnly);
    }
}
```

- [ ] **Step 1:** Create `backend/tests/CCE.Infrastructure.Tests/Migration/SeederFlagParsingTests.cs` with the contents above.

- [ ] **Step 2:** Run the tests, confirm they pass:
  ```bash
  cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/ --filter "FullyQualifiedName~SeederFlagParsing"
  ```
  Expected: 9 passing.

- [ ] **Step 3:** Run the full Infrastructure.Tests suite to confirm no regression:
  ```bash
  cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/
  ```
  Expected: 54 + 9 = 63 passing (1 skipped from baseline).

- [ ] **Step 4:** Commit:
  ```bash
  git add backend/tests/CCE.Infrastructure.Tests/Migration/SeederFlagParsingTests.cs
  git -c commit.gpgsign=false commit -m "test(seeder): SeederMode flag parser tests

  Covers all four valid modes plus the two rejected combinations
  (--migrate --demo, --seed-reference without --migrate). Also
  asserts case-insensitivity, flag-order-independence, and that
  unknown flags pass through (Host.CreateApplicationBuilder
  consumes its own).

  Sub-10b Phase 00 Task 0.2.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.3: Add `Testcontainers.MsSql` reference + write migration mode tests

**Files:**
- Modify: `backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj` — add `<PackageReference Include="Testcontainers.MsSql" />`.
- Create: `backend/tests/CCE.Infrastructure.Tests/Migration/MigratorFixture.cs` — xUnit collection fixture that boots a single SQL container and shares it across migration tests.
- Create: `backend/tests/CCE.Infrastructure.Tests/Migration/SeederMigrateModeTests.cs` — `MigrateAsync` runs against a real empty SQL container; tables created; second run is idempotent.
- Create: `backend/tests/CCE.Infrastructure.Tests/Migration/SeederMigrateSeedReferenceTests.cs` — `SeedRunner.RunAllAsync(includeDemo:false)` is idempotent across two consecutive runs (row counts identical, no duplicates).

**Why a shared fixture:** booting MS-SQL takes ~10 seconds. Per-test boot would slow CI by minutes. xUnit's `[CollectionDefinition]` + `IAsyncLifetime` give us one container shared across the migration test class.

**Final state of `MigratorFixture.cs`:**

```cs
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace CCE.Infrastructure.Tests.Migration;

/// <summary>
/// xUnit fixture that boots one SQL Server 2022 container shared across
/// all tests in <see cref="MigratorCollection"/>. Each test gets a fresh
/// database name to keep migrations isolated.
/// </summary>
public sealed class MigratorFixture : IAsyncLifetime
{
    public MsSqlContainer Container { get; } = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync() => await Container.StartAsync().ConfigureAwait(false);
    public async Task DisposeAsync()    => await Container.DisposeAsync().ConfigureAwait(false);

    /// <summary>
    /// Builds a CceDbContext pointing at a freshly-named database on the
    /// shared container. Caller is responsible for disposing.
    /// </summary>
    public CceDbContext CreateContextWithFreshDb(string dbSuffix)
    {
        var baseConn = Container.GetConnectionString();
        // Force a unique Initial Catalog per call so MigrateAsync gets a clean slate.
        var conn = $"{baseConn};Initial Catalog=CCE_{dbSuffix};TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<CceDbContext>()
            .UseSqlServer(conn)
            .Options;
        return new CceDbContext(options);
    }
}

[CollectionDefinition(nameof(MigratorCollection))]
public sealed class MigratorCollection : ICollectionFixture<MigratorFixture> { }
```

**Final state of `SeederMigrateModeTests.cs`:**

```cs
using CCE.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CCE.Infrastructure.Tests.Migration;

[Collection(nameof(MigratorCollection))]
public sealed class SeederMigrateModeTests
{
    private readonly MigratorFixture _fixture;

    public SeederMigrateModeTests(MigratorFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task MigrateAsync_AppliesAllPendingMigrations_ToFreshDatabase()
    {
        // Arrange — fresh DB.
        await using var ctx = _fixture.CreateContextWithFreshDb("migrate_fresh");
        await ctx.Database.EnsureCreatedAsync().ConfigureAwait(false);
        // Note: EnsureCreatedAsync just creates the empty DB; migrations
        // haven't run. Drop and recreate via MigrateAsync below.
        await ctx.Database.EnsureDeletedAsync().ConfigureAwait(false);

        // Act
        var pending = (await ctx.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).ToList();
        await ctx.Database.MigrateAsync().ConfigureAwait(false);

        // Assert: every existing migration is now applied.
        var applied = (await ctx.Database.GetAppliedMigrationsAsync().ConfigureAwait(false)).ToList();
        pending.Should().NotBeEmpty("the repo has at least three EF migrations");
        applied.Should().Contain(pending);
    }

    [Fact]
    public async Task MigrateAsync_IsNoOp_OnAlreadyMigratedDatabase()
    {
        // Arrange — migrate once.
        await using var ctx = _fixture.CreateContextWithFreshDb("migrate_idempotent");
        await ctx.Database.EnsureDeletedAsync().ConfigureAwait(false);
        await ctx.Database.MigrateAsync().ConfigureAwait(false);
        var firstApplied = (await ctx.Database.GetAppliedMigrationsAsync().ConfigureAwait(false)).Count();

        // Act — migrate again on the same DB.
        await using var ctx2 = _fixture.CreateContextWithFreshDb("migrate_idempotent");
        var pendingBefore = (await ctx2.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).ToList();
        await ctx2.Database.MigrateAsync().ConfigureAwait(false);
        var secondApplied = (await ctx2.Database.GetAppliedMigrationsAsync().ConfigureAwait(false)).Count();

        // Assert: second run had nothing pending and didn't change applied count.
        pendingBefore.Should().BeEmpty();
        secondApplied.Should().Be(firstApplied);
    }
}
```

- [ ] **Step 1:** Add the package reference to `CCE.Infrastructure.Tests.csproj`. Final state of the `<ItemGroup>` containing test packages:

  ```xml
    <ItemGroup>
      <PackageReference Include="Microsoft.NET.Test.Sdk" />
      <PackageReference Include="xunit" />
      <PackageReference Include="xunit.runner.visualstudio">
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        <PrivateAssets>all</PrivateAssets>
      </PackageReference>
      <PackageReference Include="xunit.analyzers" />
      <PackageReference Include="FluentAssertions" />
      <PackageReference Include="NSubstitute" />
      <PackageReference Include="NSubstitute.Analyzers.CSharp" />
      <PackageReference Include="coverlet.collector" />
      <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
      <PackageReference Include="Testcontainers.MsSql" />
    </ItemGroup>
  ```

- [ ] **Step 2:** Restore + verify package resolves:
  ```bash
  cd /Users/m/CCE/backend && dotnet restore tests/CCE.Infrastructure.Tests/
  ```
  Expected: success. The package version is pinned by `Directory.Packages.props` at `4.0.0`.

- [ ] **Step 3:** Create `backend/tests/CCE.Infrastructure.Tests/Migration/MigratorFixture.cs` with the contents above.

- [ ] **Step 4:** Create `backend/tests/CCE.Infrastructure.Tests/Migration/SeederMigrateModeTests.cs` with the contents above.

- [ ] **Step 5:** Create `backend/tests/CCE.Infrastructure.Tests/Migration/SeederMigrateSeedReferenceTests.cs` with the contents below. This test wires the same DI graph the migrator container uses (`AddApplication` + `AddInfrastructure` + the four `ISeeder` registrations + `SeedRunner`) and runs `RunAllAsync(includeDemo:false)` twice against the same DB, asserting row counts identical across runs:

  ```cs
  using CCE.Application;
  using CCE.Infrastructure;
  using CCE.Infrastructure.Persistence;
  using CCE.Seeder;
  using CCE.Seeder.Seeders;
  using FluentAssertions;
  using Microsoft.EntityFrameworkCore;
  using Microsoft.Extensions.Configuration;
  using Microsoft.Extensions.DependencyInjection;
  using Xunit;

  namespace CCE.Infrastructure.Tests.Migration;

  [Collection(nameof(MigratorCollection))]
  public sealed class SeederMigrateSeedReferenceTests
  {
      private readonly MigratorFixture _fixture;

      public SeederMigrateSeedReferenceTests(MigratorFixture fixture) => _fixture = fixture;

      [Fact]
      public async Task RunAllAsync_IsIdempotent_OnSecondRun()
      {
          // Arrange — fresh migrated DB.
          await using var ctxSetup = _fixture.CreateContextWithFreshDb("seedref_idempotent");
          await ctxSetup.Database.EnsureDeletedAsync().ConfigureAwait(false);
          await ctxSetup.Database.MigrateAsync().ConfigureAwait(false);

          var baseConn = _fixture.Container.GetConnectionString();
          var connectionString = $"{baseConn};Initial Catalog=CCE_seedref_idempotent;TrustServerCertificate=True";

          // Build a service provider matching the migrator container's DI graph.
          var configuration = new ConfigurationBuilder()
              .AddInMemoryCollection(new Dictionary<string, string?>
              {
                  ["Infrastructure:SqlConnectionString"] = connectionString,
                  ["Infrastructure:RedisConnectionString"] = "localhost:6379", // unused in seeders
                  ["ConnectionStrings:Default"] = connectionString,
              })
              .Build();
          var services = new ServiceCollection();
          services.AddLogging();
          services.AddApplication();
          services.AddInfrastructure(configuration);
          services.AddScoped<ISeeder, ReferenceDataSeeder>();
          services.AddScoped<ISeeder, RolesAndPermissionsSeeder>();
          services.AddScoped<ISeeder, KnowledgeMapSeeder>();
          services.AddScoped<ISeeder, DemoDataSeeder>();
          services.AddScoped<SeedRunner>();
          await using var sp = services.BuildServiceProvider();

          // Act — run once, count rows; run again, count rows.
          static async Task<(int Roles, int KnowledgeMaps, int KnowledgeNodes)> RunAndCount(IServiceProvider sp)
          {
              await using var scope = sp.CreateAsyncScope();
              var runner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
              await runner.RunAllAsync(includeDemo: false).ConfigureAwait(false);
              var ctx = scope.ServiceProvider.GetRequiredService<CceDbContext>();
              return (
                  Roles: await ctx.Set<CCE.Domain.Identity.Role>().CountAsync().ConfigureAwait(false),
                  KnowledgeMaps: await ctx.Set<CCE.Domain.Knowledge.KnowledgeMap>().CountAsync().ConfigureAwait(false),
                  KnowledgeNodes: await ctx.Set<CCE.Domain.Knowledge.KnowledgeMapNode>().CountAsync().ConfigureAwait(false));
          }
          var first = await RunAndCount(sp);
          var second = await RunAndCount(sp);

          // Assert — counts are non-zero (seeders did something) AND identical (idempotent).
          first.Roles.Should().BeGreaterThan(0);
          first.KnowledgeMaps.Should().BeGreaterThan(0);
          first.KnowledgeNodes.Should().BeGreaterThan(0);
          second.Should().BeEquivalentTo(first, "second run must not duplicate any rows");
      }
  }
  ```

  **Note on entity types:** the test imports `CCE.Domain.Identity.Role`, `CCE.Domain.Knowledge.KnowledgeMap`, and `CCE.Domain.Knowledge.KnowledgeMapNode`. Verify those exact namespaces match the repo at edit time:
  ```bash
  grep -rn "namespace CCE.Domain" /Users/m/CCE/backend/src/CCE.Domain/ | grep -E "Identity|Knowledge" | head
  ```
  If a namespace differs (e.g. `CCE.Domain.Community` for `Role`), adjust the imports + `Set<T>()` calls to match.

- [ ] **Step 6:** Build:
  ```bash
  cd /Users/m/CCE/backend && dotnet build tests/CCE.Infrastructure.Tests/
  ```
  Expected: success.

- [ ] **Step 7:** Run the migration tests (requires Docker running locally):
  ```bash
  cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/ --filter "FullyQualifiedName~SeederMigrate"
  ```
  Expected: 3 passing (boot time ~15-30 seconds for the SQL container).

- [ ] **Step 8:** Run the full Infrastructure.Tests suite:
  ```bash
  cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/
  ```
  Expected: 54 + 9 + 3 = 66 passing (1 skipped from baseline).

- [ ] **Step 9:** Commit:
  ```bash
  git add backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj \
          backend/tests/CCE.Infrastructure.Tests/Migration/MigratorFixture.cs \
          backend/tests/CCE.Infrastructure.Tests/Migration/SeederMigrateModeTests.cs \
          backend/tests/CCE.Infrastructure.Tests/Migration/SeederMigrateSeedReferenceTests.cs
  git -c commit.gpgsign=false commit -m "test(seeder): MigrateAsync + reference-seed idempotency on Testcontainers MS-SQL

  MigratorFixture is an xUnit collection fixture that boots one
  mssql/server:2022-latest container shared across all migration
  tests, with each test getting a freshly-named DB on that
  container. Adds Testcontainers.MsSql 4.0.0 package reference
  (already pinned in Directory.Packages.props by Sub-9 baseline).

  Three tests:
   - MigrateAsync applies every pending migration to a fresh DB
   - MigrateAsync is a no-op on an already-migrated DB
   - SeedRunner.RunAllAsync(includeDemo:false) twice produces
     identical row counts (idempotent reference seeders)

  Sub-10b Phase 00 Task 0.3.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.4: Migration runner Dockerfile + CI build step

**Files:**
- Create: `backend/src/CCE.Seeder/Dockerfile` — multistage build, mirror of API Dockerfiles.
- Modify: `.github/workflows/ci.yml` — add a 5th `docker/build-push-action@v6` step for `cce-migrator`. Push to ghcr.io is a Phase 01 concern; Phase 00 keeps `push: false`.

**Final state of `backend/src/CCE.Seeder/Dockerfile`:**

```dockerfile
# syntax=docker/dockerfile:1.7
# Build stage — restore + publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy props + global config first so package restore can cache.
COPY Directory.Packages.props Directory.Build.props NuGet.config* ./

# Copy every csproj before the rest of the source so `dotnet restore`
# layers are cached as long as csproj files don't change.
COPY src/CCE.Api.Common/CCE.Api.Common.csproj           src/CCE.Api.Common/
COPY src/CCE.Api.External/CCE.Api.External.csproj       src/CCE.Api.External/
COPY src/CCE.Application/CCE.Application.csproj         src/CCE.Application/
COPY src/CCE.Domain/CCE.Domain.csproj                   src/CCE.Domain/
COPY src/CCE.Domain.SourceGenerators/CCE.Domain.SourceGenerators.csproj src/CCE.Domain.SourceGenerators/
COPY src/CCE.Infrastructure/CCE.Infrastructure.csproj   src/CCE.Infrastructure/
COPY src/CCE.Integration/CCE.Integration.csproj         src/CCE.Integration/
COPY src/CCE.Seeder/CCE.Seeder.csproj                   src/CCE.Seeder/

RUN dotnet restore "src/CCE.Seeder/CCE.Seeder.csproj"

# Source generator input — CCE.Domain's source generator reads
# permissions.yaml from backend root (two levels up from src/CCE.Domain/).
COPY permissions.yaml ./

# Now copy the rest of the source and publish.
COPY src/ src/
RUN dotnet publish "src/CCE.Seeder/CCE.Seeder.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage — aspnet:8.0 (NOT runtime:8.0) because the seeder
# transitively pulls Microsoft.AspNetCore.Identity.EntityFrameworkCore
# at compile time; the smaller `runtime:8.0` lacks the ASP.NET Core
# shared-framework assemblies. Same image as API Dockerfiles → shares
# layers across registry pulls.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user (aspnet:8.0 ships an `app` user uid 1654).
USER app

COPY --from=build --chown=app:app /app/publish .

# The seeder also needs the API's appsettings.json to resolve the
# connection string. Copy it from the build stage; production
# overrides via env-vars (DOTNET_ConnectionStrings__Default etc.).
COPY --from=build --chown=app:app /src/src/CCE.Api.External/appsettings.json /app/src/CCE.Api.External/appsettings.json

# No HEALTHCHECK — the migrator is a one-shot console.
# No EXPOSE — it doesn't listen on a port.
ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CCE.Seeder.dll"]
# Default command — overridden in compose for prod migration:
#   command: ["--migrate", "--seed-reference"]
CMD []
```

**Note on `appsettings.json` location:** the seeder's `FindApiAppSettingsDir()` walks up looking for `src/CCE.Api.External/appsettings.json`. The Dockerfile copies it to that exact relative path inside the runtime container so the walk-up logic finds it without changes. Production overrides every meaningful key via env-vars (e.g. `DOTNET_ConnectionStrings__Default=...` becomes `ConnectionStrings:Default` in the .NET configuration).

**CI step addition** — append to `.github/workflows/ci.yml` inside the `docker-build` job, after the `Build admin-cms` step (line ~174) and before `Smoke-probe all four runtime containers`:

```yaml
      - name: Build cce-migrator
        uses: docker/build-push-action@v6
        with:
          context: backend
          file: backend/src/CCE.Seeder/Dockerfile
          push: false
          load: true
          tags: cce-migrator:ci
          cache-from: type=gha,scope=migrator
          cache-to: type=gha,mode=max,scope=migrator
```

The smoke-probe section is updated separately in Phase 01 (where we add a probe that runs `docker compose run --rm migrator --migrate` against an inline SQL Server). Phase 00 just verifies the image builds.

- [ ] **Step 1:** Create `backend/src/CCE.Seeder/Dockerfile` with the contents above.

- [ ] **Step 2:** Local smoke build (Docker required):
  ```bash
  cd /Users/m/CCE && docker build -t cce-migrator:dev -f backend/src/CCE.Seeder/Dockerfile backend/
  ```
  Expected: build succeeds; `docker images cce-migrator:dev` shows the image.

- [ ] **Step 3:** Sanity-check that the entrypoint runs and rejects illegal flags:
  ```bash
  docker run --rm cce-migrator:dev --migrate --demo
  ```
  Expected: stderr message `error: Demo data is not allowed in migration mode...` and exit code 1. Verify exit code:
  ```bash
  docker run --rm cce-migrator:dev --migrate --demo; echo "exit: $?"
  ```
  Expected: `exit: 1`.

- [ ] **Step 4:** Add the `Build cce-migrator` step to `.github/workflows/ci.yml`. Locate the line `- name: Smoke-probe all four runtime containers` (around line 176) and insert the new step immediately above it.

- [ ] **Step 5:** Verify the workflow YAML still parses:
  ```bash
  python3 -c "import yaml; yaml.safe_load(open('/Users/m/CCE/.github/workflows/ci.yml'))"
  ```
  Expected: no errors.

- [ ] **Step 6:** Commit:
  ```bash
  git add backend/src/CCE.Seeder/Dockerfile .github/workflows/ci.yml
  git -c commit.gpgsign=false commit -m "feat(seeder): cce-migrator Dockerfile + CI build

  Multistage Dockerfile mirrors the API pattern (sdk:8.0 → aspnet:8.0,
  non-root app user, csproj-first restore-cache layering, copies
  permissions.yaml for the CCE.Domain source generator). Copies the
  External API's appsettings.json so the seeder's FindApiAppSettingsDir
  walk-up succeeds; production overrides config via env-vars.

  ENTRYPOINT is dotnet CCE.Seeder.dll (no default command);
  Phase 01 compose passes [\"--migrate\", \"--seed-reference\"].

  CI gets a 5th docker/build-push-action step (push:false, GHA layer
  cache scoped 'migrator'). Push to ghcr.io is Phase 01.

  Sub-10b Phase 00 Task 0.4.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.5: Forward-only migrations runbook

**Files:**
- Create: `docs/runbooks/migrations.md`.

**Why now:** Phase 02 wires the rollback story (`rollback.ps1` swaps image tags forward-only). PR review for any new migration depends on this discipline being written down before the next migration lands. Phase 00 is the right time because Phase 01 starts wiring compose to invoke the migrator on every deploy — i.e. discipline must exist before automation hits production code paths.

**Final state of `docs/runbooks/migrations.md`:**

```markdown
# Forward-only migrations — discipline + escape hatch

> Sub-10b's rollback story relies on this. Read before authoring a new EF migration.

## The contract

Every CCE migration must be **forward-only and backward-compatible**: an old image (release N-1) running against the new schema (release N) keeps working. This is what makes image-tag rollback (Sub-10b §6) safe — no DB rewind required.

## Rules

1. **Additive only.** Add columns, add tables, add indexes. Never drop or rename in place.
2. **No destructive defaults.** A new `NOT NULL` column needs a `DEFAULT` expression that handles existing rows. Prefer `WITH VALUES` (SQL Server) so the default is materialized into existing rows at migration time, not at row-read time.
3. **Online indexes.** `CREATE INDEX WITH (ONLINE = ON)` against tables with non-trivial row counts — locks are blocked otherwise and we don't have a maintenance window.
4. **Deprecation across releases.** To remove or rename a column:
   - Release **N**: add the replacement column. Application code dual-writes to old + new.
   - Release **N+1**: stop reading the old column. Application code reads from new only.
   - Release **N+2**: drop the old column. Schema is now clean.
5. **No type changes in place.** Add a new column of the new type, dual-write, swap reads, drop old (3-release sequence).
6. **No FK direction flips.** Same 3-release sequence as type changes.

## Why these rules

With these rules, an image from release N-1 can run against schema from release N because:
- New columns it doesn't know about are ignored.
- Old columns it still writes to still exist and have correct types.
- Indexes are additive, not subtractive.

This means `deploy/rollback.ps1 -ToTag <previous-tag>` works without DB intervention.

## Escape hatch — destructive migrations

Some changes can't be staged. Examples:
- Splitting one table into two.
- Migrating data between tenant buckets.
- Backfilling computed columns from row data.

When this is necessary, the change is its own release with these gates:

1. **Separate spec + plan.** `docs/superpowers/specs/<date>-<topic>-design.md` documents the data-migration plan; PR-reviewed before any code.
2. **Backup-and-restore is part of the runbook** — schema rollback isn't possible, so the rollback strategy is "restore the pre-deploy backup". Backup automation lives in Sub-10c, but the destructive release explicitly invokes it.
3. **Maintenance window.** Operator schedules downtime; deploy runs against a frozen system.
4. **No image-tag rollback.** The release explicitly disables `rollback.ps1` for the target tag (or the runbook calls out that it's unavailable).

## When in doubt — ask

A migration with any "wait, will the old image still run?" doubt is a destructive migration. Default to the escape hatch.

## References

- Sub-10b spec §Migration discipline: [`../superpowers/specs/2026-05-03-sub-10b-design.md`](../superpowers/specs/2026-05-03-sub-10b-design.md)
- Rollback runbook (Phase 02): [`./rollback.md`](./rollback.md) (lands in Sub-10b Phase 02)
- EF Core migration docs: <https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/>
```

- [ ] **Step 1:** Create `docs/runbooks/` directory if missing:
  ```bash
  mkdir -p /Users/m/CCE/docs/runbooks
  ```

- [ ] **Step 2:** Create `docs/runbooks/migrations.md` with the contents above.

- [ ] **Step 3:** Commit:
  ```bash
  git add docs/runbooks/migrations.md
  git -c commit.gpgsign=false commit -m "docs(runbook): forward-only migration discipline

  The rule that makes Sub-10b's image-tag rollback safe: every
  migration must be additive + backward-compatible so an N-1 image
  runs against an N schema. Removes are a 3-release sequence
  (add new + dual-write → swap reads → drop old). Destructive
  changes get an explicit escape hatch (separate spec, backup-
  restore, maintenance window, no rollback.ps1).

  Sub-10b Phase 00 Task 0.5.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Phase 00 close-out

After Task 0.5 commits cleanly:

- [ ] **Run the full check:**
  ```bash
  cd /Users/m/CCE/backend && dotnet build && \
    dotnet test tests/CCE.Application.Tests/ tests/CCE.Infrastructure.Tests/
  ```
  Expected: backend build success; 439 Application + 66 Infrastructure tests passing (1 skipped). Migration tests need Docker running locally; CI will run them automatically.

- [ ] **Verify CI green:** push the branch, wait for GitHub Actions. The `docker-build` job should pass with all 5 image build steps green. Check the run output mentions `cce-migrator:ci` builds.

- [ ] **Local image probe** (optional but recommended):
  ```bash
  cd /Users/m/CCE && docker build -t cce-migrator:dev -f backend/src/CCE.Seeder/Dockerfile backend/
  # Boot a throwaway SQL container in the same network namespace
  docker run -d --rm --name cce-test-sql \
    -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Strong!Passw0rd" \
    -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
  sleep 20  # wait for SQL Server to come up
  docker run --rm --network host \
    -e "ConnectionStrings__Default=Server=localhost,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=True;" \
    -e "Infrastructure__SqlConnectionString=Server=localhost,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=True;" \
    -e "Infrastructure__RedisConnectionString=localhost:6379" \
    -e "Keycloak__Authority=http://localhost:8080/realms/cce" \
    -e "Keycloak__Audience=cce-api" \
    -e "Keycloak__RequireHttpsMetadata=false" \
    cce-migrator:dev --migrate
  docker rm -f cce-test-sql
  ```
  Expected: container logs show "Applying EF Core migrations…" and "→ pending: 20260425134009_InitialAuditEvents" (and the other two), then "Applied 3 migration(s)." then exit 0.

- [ ] **Hand off to Phase 01.** Phase 01 wires compose to use this image, writes `deploy.ps1`, and extends CI to push to ghcr.io. Plan file: `phase-01-compose-and-deploy.md` (to be written when we're ready to start it).

**Phase 00 done when:**
- 5 commits land on `main`, each green.
- `CCE.Seeder` accepts `--migrate` and `--migrate --seed-reference` and rejects illegal combinations with non-zero exit.
- `cce-migrator` Docker image builds in CI (push still gated until Phase 01).
- `Testcontainers.MsSql` is referenced by `CCE.Infrastructure.Tests`; 3 new migration tests pass against a real SQL container.
- 9 new flag-parser tests pass.
- `docs/runbooks/migrations.md` is committed and PR-review-ready.
- Test counts: backend Application 439 (unchanged); Infrastructure 66 (was 54, +9 flag parser, +3 migrator). Frontend 502 (unchanged).
- No behavioural change to any existing app — Phase 00 is pure foundation work.

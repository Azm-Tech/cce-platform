# Phase 06 — Infrastructure Layer (EF Core + Redis + AuditEvents)

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Wire the Infrastructure layer with the smallest end-to-end persistence story Foundation needs: EF Core 8 `CceDbContext` (snake_case naming), one entity (`AuditEvent`), one migration, append-only enforcement, Redis connection factory + a tiny key-value smoke service. Tests run against the live Azure SQL Edge + Redis containers via Testcontainers fixtures (managed locally — but in this phase's tests we connect to the already-running docker-compose containers via TCP for speed; Phase 16 wires CI Testcontainers).

**Tasks in this phase:** 9
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 05 complete; `dotnet test backend/CCE.sln` reports 10 passed; Docker stack healthy (sqlserver + redis must be `(healthy)` on `docker compose ps`).

---

## Pre-execution sanity checks

1. `dotnet --list-sdks | grep '^8\.'` → 8.0.x.
2. `dotnet build backend/CCE.sln --nologo 2>&1 | tail -3` → 0 errors.
3. `dotnet test backend/CCE.sln --nologo --no-build 2>&1 | tail -5` → 10 passed.
4. `docker compose ps --format json | grep -c '"Health":"healthy"'` → at least 5.
5. `nc -z -w 2 localhost 1433 && echo OK` → `OK` (SQL reachable).
6. `nc -z -w 2 localhost 6379 && echo OK` → `OK` (Redis reachable).
7. `.env` has `SQL_PASSWORD=` and a value.

If any fail, stop and report.

---

## Foundation database scope

Per spec §5.1 and §10 (project roadmap), Foundation's SQL schema contains exactly:

- `__EFMigrationsHistory` (managed by EF Core)
- `audit_events` (append-only via SQL trigger)

Every business entity (User, Resource, Post, Country, etc.) lands in **sub-project 2** (Data & Domain). Foundation proves the persistence pipeline works end-to-end — migrations, naming conventions, change tracking, append-only enforcement, Redis cache — using `audit_events` as the canonical example.

---

## Task 6.1: Add EF Core packages and naming-convention setup to Infrastructure

**Files:**

- Modify: `backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj`

- [ ] **Step 1: Read current csproj**

```bash
cat backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj
```

Expected: minimal csproj with `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, refs to Application + Domain.

- [ ] **Step 2: Overwrite the csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <!-- NU1608: transitive Roslyn version mismatch. CCE.Domain.SourceGenerators pins
         Microsoft.CodeAnalysis.* at 4.8 (host-compat); EntityFrameworkCore.Design 8.0.10
         transitively wants 4.5. NuGet's "highest wins" picks 4.8 which is backwards-compatible.
         This is the documented industry pattern for source-gen + EF Design coexistence. -->
    <NoWarn>$(NoWarn);NU1608</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Options" />

    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="EFCore.NamingConventions" />

    <PackageReference Include="StackExchange.Redis" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CCE.Application\CCE.Application.csproj" />
    <ProjectReference Include="..\CCE.Domain\CCE.Domain.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Add `Microsoft.Extensions.Configuration.Abstractions` and `Microsoft.Extensions.Options` to CPM**

The plan's CPM file from Phase 03 covers most of these. Verify these two specifically are present in `backend/Directory.Packages.props`. If absent, append to the "Core framework & Testing" group:

```bash
grep 'Microsoft.Extensions.Configuration.Abstractions\|Microsoft.Extensions.Options' backend/Directory.Packages.props
```

Expected: prints both lines. If either is missing, add to the appropriate `<ItemGroup>`:

```xml
    <PackageVersion Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="8.0.2" />
```

- [ ] **Step 4: Restore + build**

```bash
dotnet build backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj --nologo -c Debug 2>&1 | tail -8
```

Expected: 0 errors. NuGet restore may take 1–3 minutes for first-time download of EF Core packages.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj backend/Directory.Packages.props
git -c commit.gpgsign=false commit -m "feat(phase-06): add EF Core 8 + StackExchange.Redis packages to CCE.Infrastructure"
```

---

## Task 6.2: Define `AuditEvent` entity in Domain

**Files:**

- Create: `backend/src/CCE.Domain/Audit/AuditEvent.cs`

**Rationale:** `AuditEvent` is the only Foundation entity. It captures every security-relevant action: actor, resource, verb, correlation id, timestamp, optional diff. Append-only (no updates, no deletes) — enforced by SQL trigger in Task 6.5.

- [ ] **Step 1: Write the failing test first (TDD per ADR-0007)**

Create `backend/tests/CCE.Domain.Tests/Audit/AuditEventTests.cs`:

```csharp
using CCE.Domain.Audit;

namespace CCE.Domain.Tests.Audit;

public class AuditEventTests
{
    [Fact]
    public void Constructor_assigns_provided_values()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var occurredOn = new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero);
        var correlationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var sut = new AuditEvent(
            id,
            occurredOn,
            actor: "admin@cce.local",
            action: "User.Create",
            resource: "User/abc-123",
            correlationId: correlationId,
            diff: """{"field":"email","from":null,"to":"x@y.local"}""");

        sut.Id.Should().Be(id);
        sut.OccurredOn.Should().Be(occurredOn);
        sut.Actor.Should().Be("admin@cce.local");
        sut.Action.Should().Be("User.Create");
        sut.Resource.Should().Be("User/abc-123");
        sut.CorrelationId.Should().Be(correlationId);
        sut.Diff.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_blank_actor(string? actor)
    {
        var act = () => new AuditEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            actor: actor!,
            action: "Test",
            resource: "Res",
            correlationId: Guid.NewGuid(),
            diff: null);

        act.Should().Throw<ArgumentException>().WithParameterName("actor");
    }

    [Fact]
    public void Constructor_rejects_blank_action()
    {
        var act = () => new AuditEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            actor: "x",
            action: "",
            resource: "Res",
            correlationId: Guid.NewGuid(),
            diff: null);

        act.Should().Throw<ArgumentException>().WithParameterName("action");
    }

    [Fact]
    public void Diff_can_be_null_for_actions_without_state_change()
    {
        var sut = new AuditEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            actor: "x",
            action: "User.Login",
            resource: "User/x",
            correlationId: Guid.NewGuid(),
            diff: null);

        sut.Diff.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run the failing test**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo -c Debug --filter FullyQualifiedName~AuditEventTests 2>&1 | tail -10
```

Expected: build error — `AuditEvent` doesn't exist yet. That's the failure we want.

- [ ] **Step 3: Write `backend/src/CCE.Domain/Audit/AuditEvent.cs`**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Audit;

/// <summary>
/// Single immutable record of a security-relevant action. Append-only — never updated, never deleted.
/// Persistence enforces append-only via SQL trigger in <c>CCE.Infrastructure</c> (Phase 06).
/// </summary>
public sealed class AuditEvent : Entity<Guid>
{
    /// <summary>EF Core constructor — bypasses validation. Application code must use the public constructor.</summary>
#pragma warning disable CS8618 // Non-nullable members initialized by EF Core during materialization.
    private AuditEvent(Guid id) : base(id) { }
#pragma warning restore CS8618

    public AuditEvent(
        Guid id,
        DateTimeOffset occurredOn,
        string actor,
        string action,
        string resource,
        Guid correlationId,
        string? diff)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Actor cannot be null or whitespace.", nameof(actor));
        }
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action cannot be null or whitespace.", nameof(action));
        }
        if (string.IsNullOrWhiteSpace(resource))
        {
            throw new ArgumentException("Resource cannot be null or whitespace.", nameof(resource));
        }

        OccurredOn = occurredOn;
        Actor = actor;
        Action = action;
        Resource = resource;
        CorrelationId = correlationId;
        Diff = diff;
    }

    /// <summary>UTC moment the audited action occurred.</summary>
    public DateTimeOffset OccurredOn { get; private set; }

    /// <summary>Identity of the principal that performed the action (typically <c>upn</c> claim).</summary>
    public string Actor { get; private set; } = null!;

    /// <summary>Verb describing the action — convention <c>Resource.Verb</c> (e.g., <c>User.Create</c>).</summary>
    public string Action { get; private set; } = null!;

    /// <summary>Stable resource identifier (e.g., <c>User/abc-123</c>).</summary>
    public string Resource { get; private set; } = null!;

    /// <summary>Cross-system correlation identifier connecting this event to logs, traces, and other events.</summary>
    public Guid CorrelationId { get; private set; }

    /// <summary>Optional JSON describing the state change. Null for actions without a payload (e.g., logins).</summary>
    public string? Diff { get; private set; }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo -c Debug 2>&1 | tail -8
```

Expected: 14 passed (10 existing + 4 new AuditEvent tests).

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Domain/Audit backend/tests/CCE.Domain.Tests/Audit
git -c commit.gpgsign=false commit -m "feat(phase-06): add AuditEvent domain entity (append-only, validated constructor) with 4 TDD tests"
```

---

## Task 6.3: Add `CceDbContext` with snake_case naming + `AuditEvents` set

**Files:**

- Create: `backend/src/CCE.Infrastructure/Persistence/CceDbContext.cs`
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/AuditEventConfiguration.cs`

**Rationale:** EF Core `DbContext` is the persistence boundary. `EFCore.NamingConventions` rewrites C# PascalCase → SQL snake_case so `AuditEvent.OccurredOn` becomes column `occurred_on` — matches typical SQL Server idioms used in Saudi government data dictionaries.

- [ ] **Step 1: Write `backend/src/CCE.Infrastructure/Persistence/CceDbContext.cs`**

```csharp
using CCE.Application.Common.Interfaces;
using CCE.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence;

/// <summary>
/// Application <see cref="DbContext"/>. Configured via <see cref="DependencyInjection"/> to use
/// SQL Server with snake_case naming. Foundation contains exactly one DbSet (audit events);
/// sub-project 2 expands the schema to the full BRD entity set.
/// </summary>
public sealed class CceDbContext : DbContext, ICceDbContext
{
    public CceDbContext(DbContextOptions<CceDbContext> options) : base(options) { }

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CceDbContext).Assembly);
    }
}
```

- [ ] **Step 2: Write `backend/src/CCE.Infrastructure/Persistence/Configurations/AuditEventConfiguration.cs`**

```csharp
using CCE.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();   // application supplies Guid

        builder.Property(e => e.OccurredOn)
            .IsRequired();

        builder.Property(e => e.Actor)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Action)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.Resource)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(e => e.CorrelationId)
            .IsRequired();

        builder.Property(e => e.Diff)
            // SQL Server: nvarchar(max) for arbitrary JSON
            .HasColumnType("nvarchar(max)");

        // Index on actor + occurred_on for fast "what did user X do?" queries
        builder.HasIndex(e => new { e.Actor, e.OccurredOn })
            .HasDatabaseName("ix_audit_events_actor_occurred_on");

        // Index on correlation_id for incident replay
        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("ix_audit_events_correlation_id");
    }
}
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj --nologo -c Debug 2>&1 | tail -6
```

Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add backend/src/CCE.Infrastructure/Persistence
git -c commit.gpgsign=false commit -m "feat(phase-06): add CceDbContext with AuditEvents DbSet + EF configuration (snake_case naming)"
```

---

## Task 6.4: Wire `CceDbContext` into DI with snake_case + connection-string config

**Files:**

- Modify: `backend/src/CCE.Infrastructure/DependencyInjection.cs`
- Create: `backend/src/CCE.Infrastructure/CceInfrastructureOptions.cs`

- [ ] **Step 1: Write `backend/src/CCE.Infrastructure/CceInfrastructureOptions.cs`**

```csharp
namespace CCE.Infrastructure;

/// <summary>
/// Strongly-typed options for the Infrastructure layer. Bound from <c>appsettings.json</c>
/// section <c>"Infrastructure"</c> (or env vars <c>Infrastructure__SqlConnectionString</c> etc.).
/// </summary>
public sealed class CceInfrastructureOptions
{
    public const string SectionName = "Infrastructure";

    /// <summary>SQL Server connection string. Required.</summary>
    public string SqlConnectionString { get; init; } = string.Empty;

    /// <summary>Redis connection string (e.g., <c>localhost:6379</c>). Required.</summary>
    public string RedisConnectionString { get; init; } = string.Empty;
}
```

- [ ] **Step 2: Overwrite `backend/src/CCE.Infrastructure/DependencyInjection.cs`**

```csharp
using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CCE.Infrastructure;

/// <summary>
/// Composition-root extension methods for the Infrastructure layer.
/// Web APIs call <see cref="AddInfrastructure"/> from <c>Program.cs</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<CceInfrastructureOptions>()
            .Bind(configuration.GetSection(CceInfrastructureOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Clock
        services.AddSingleton<ISystemClock, SystemClock>();

        // EF Core — SQL Server with snake_case naming
        services.AddDbContext<CceDbContext>((sp, opts) =>
        {
            var infraOpts = sp.GetRequiredService<IOptions<CceInfrastructureOptions>>().Value;
            opts.UseSqlServer(infraOpts.SqlConnectionString);
            opts.UseSnakeCaseNamingConvention();
        });
        services.AddScoped<ICceDbContext>(sp => sp.GetRequiredService<CceDbContext>());

        // Redis — singleton multiplexer
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var infraOpts = sp.GetRequiredService<IOptions<CceInfrastructureOptions>>().Value;
            return ConnectionMultiplexer.Connect(infraOpts.RedisConnectionString);
        });

        return services;
    }
}
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj --nologo -c Debug 2>&1 | tail -6
```

Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add backend/src/CCE.Infrastructure/DependencyInjection.cs backend/src/CCE.Infrastructure/CceInfrastructureOptions.cs
git -c commit.gpgsign=false commit -m "feat(phase-06): wire CceDbContext + Redis multiplexer into DI with options binding"
```

---

## Task 6.5: Create the initial EF migration

**Files:**

- Create: `backend/src/CCE.Infrastructure/Persistence/Migrations/*` (auto-generated)

**Rationale:** EF Core scaffolds C# migration code from the model snapshot. This is committed so production deploys apply migrations without local model evaluation.

- [ ] **Step 1: Install/refresh the `dotnet-ef` global tool**

```bash
dotnet tool install --global dotnet-ef --version 8.0.10 || dotnet tool update --global dotnet-ef --version 8.0.10
dotnet ef --version
```

Expected: prints `Entity Framework Core .NET Command-line Tools 8.0.10`. The tool installs to `~/.dotnet/tools` — ensure that's on PATH (it usually is, but `export PATH="$HOME/.dotnet/tools:$PATH"` if not).

- [ ] **Step 2: Generate the migration**

EF Core needs an executable entry point to run migrations. We use `CCE.Api.External` as the migration host (it composes the full Infrastructure registration).

The Api projects don't yet pass the `IConfiguration` to `AddInfrastructure` — they call `AddInfrastructure()` without args (Phase 03 left them that way). For migration tooling we provide the config via env vars — the migrations scaffolder calls `AddInfrastructure(configuration)` only when wired in `Program.cs`. Since that's a later step in this phase (Task 6.7 makes Program.cs use the new signature), we use a temporary design-time factory.

Create `backend/src/CCE.Infrastructure/Persistence/CceDbContextDesignTimeFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CCE.Infrastructure.Persistence;

/// <summary>
/// EF Core design-time factory used by <c>dotnet ef migrations add/update</c>.
/// Reads the connection string from the <c>CCE_DESIGN_SQL_CONN</c> env var with a
/// reasonable localhost default for the dev container. Production migrations are
/// applied from the API's runtime composition, never this factory.
/// </summary>
public sealed class CceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CceDbContext>
{
    public CceDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("CCE_DESIGN_SQL_CONN")
                   ?? "Server=localhost,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=true;";
        var options = new DbContextOptionsBuilder<CceDbContext>()
            .UseSqlServer(conn)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new CceDbContext(options);
    }
}
```

Then run:

```bash
cd backend/src/CCE.Infrastructure
dotnet ef migrations add InitialAuditEvents \
  --project CCE.Infrastructure.csproj \
  --output-dir Persistence/Migrations \
  --context CceDbContext \
  --verbose 2>&1 | tail -15
cd ../../..
```

Expected: prints `Done.` and creates 3 files in `backend/src/CCE.Infrastructure/Persistence/Migrations/`:

- `<timestamp>_InitialAuditEvents.cs`
- `<timestamp>_InitialAuditEvents.Designer.cs`
- `CceDbContextModelSnapshot.cs`

- [ ] **Step 3: Inspect the migration to confirm snake_case + audit_events**

```bash
ls backend/src/CCE.Infrastructure/Persistence/Migrations/
grep 'audit_events\|occurred_on\|correlation_id' backend/src/CCE.Infrastructure/Persistence/Migrations/*_InitialAuditEvents.cs | head -10
```

Expected: prints lines referencing `audit_events`, `occurred_on`, `correlation_id` — confirms snake_case mapping is in the migration SQL.

- [ ] **Step 4: Apply the migration to the running SQL container**

```bash
export CCE_DESIGN_SQL_CONN="Server=localhost,1433;Database=CCE;User Id=sa;Password=$(grep ^SQL_PASSWORD .env | cut -d= -f2-);TrustServerCertificate=true;"
cd backend/src/CCE.Infrastructure
dotnet ef database update \
  --project CCE.Infrastructure.csproj \
  --context CceDbContext \
  --verbose 2>&1 | tail -10
cd ../../..
```

Expected: prints `Done.` (or "No migrations were applied" if previously run). Database `CCE` and table `audit_events` now exist in the running SQL Edge container.

- [ ] **Step 5: Verify the table from the host**

```bash
docker run --rm --network cce_cce-net -i mcr.microsoft.com/azure-sql-edge:1.0.7 \
  /opt/mssql-tools/bin/sqlcmd 2>/dev/null \
  -S sqlserver,1433 -U sa -P "$(grep ^SQL_PASSWORD .env | cut -d= -f2-)" \
  -d CCE -Q "SELECT name FROM sys.tables ORDER BY name;" \
  || echo "Falling back to docker exec"

# Fallback: install sqlcmd-equivalent in alpine sidecar (slower but works on arm64)
docker run --rm --network cce_cce-net alpine:3 sh -c "
  apk add --quiet --no-cache postgresql-client >/dev/null 2>&1
  apk add --quiet --no-cache sqlcmd 2>/dev/null
  echo 'Tables created (verify below): audit_events, __EFMigrationsHistory'
"
```

Note: `mcr.microsoft.com/azure-sql-edge` is arm64-native but its bundled `sqlcmd` may be amd64-only. If both attempts fail, we'll verify via integration test in Task 6.6 — that's authoritative.

If you prefer a quick host-side check, install `sqlcmd` natively: `brew install sqlcmd` (or use any GUI client — DBeaver, Azure Data Studio).

- [ ] **Step 6: Commit**

```bash
git add backend/src/CCE.Infrastructure/Persistence
git -c commit.gpgsign=false commit -m "feat(phase-06): scaffold InitialAuditEvents EF migration + design-time factory"
```

---

## Task 6.6: Integration test: write + read AuditEvent through CceDbContext

**Files:**

- Create: `backend/tests/CCE.Infrastructure.Tests/Persistence/CceDbContextTests.cs`

**Rationale:** Proves the persistence pipeline works end-to-end against the live SQL container. This is the canonical Phase 06 success test.

- [ ] **Step 1: Write the test**

```csharp
using CCE.Domain.Audit;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Tests.Persistence;

public class CceDbContextTests
{
    private static string ConnectionString =>
        $"Server=localhost,1433;Database=CCE;User Id=sa;Password={GetPassword()};TrustServerCertificate=true;";

    private static string GetPassword()
    {
        var envFile = Path.Combine(GetRepoRoot(), ".env");
        if (!File.Exists(envFile))
        {
            return "Strong!Passw0rd";
        }
        foreach (var line in File.ReadAllLines(envFile))
        {
            if (line.StartsWith("SQL_PASSWORD=", StringComparison.Ordinal))
            {
                return line["SQL_PASSWORD=".Length..];
            }
        }
        return "Strong!Passw0rd";
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, ".env.example")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root.");
    }

    private static CceDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<CceDbContext>()
            .UseSqlServer(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new CceDbContext(options);
    }

    [Fact]
    public async Task Round_trips_AuditEvent_through_SQL()
    {
        await using var ctx = NewContext();

        var id = Guid.NewGuid();
        var occurredOn = new DateTimeOffset(2026, 4, 25, 14, 0, 0, TimeSpan.Zero);
        var correlationId = Guid.NewGuid();
        var entity = new AuditEvent(
            id,
            occurredOn,
            actor: "test-user@cce.local",
            action: "Test.Insert",
            resource: $"AuditEvent/{id}",
            correlationId: correlationId,
            diff: "{\"smoke\":true}");

        ctx.AuditEvents.Add(entity);
        await ctx.SaveChangesAsync();

        // Re-fetch in a new context to confirm the row hit SQL (not just change-tracker)
        await using var ctx2 = NewContext();
        var found = await ctx2.AuditEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

        found.Should().NotBeNull();
        found!.Actor.Should().Be("test-user@cce.local");
        found.Action.Should().Be("Test.Insert");
        found.CorrelationId.Should().Be(correlationId);
        found.Diff.Should().Be("{\"smoke\":true}");
        found.OccurredOn.Should().Be(occurredOn);

        // Cleanup so re-runs are deterministic
        ctx2.AuditEvents.Remove(found);
        await ctx2.SaveChangesAsync();
    }

    [Fact]
    public async Task Schema_has_expected_indexes()
    {
        await using var ctx = NewContext();

        var indexes = await ctx.Database.SqlQuery<string>(
            $"SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('audit_events') AND name IS NOT NULL ORDER BY name")
            .ToListAsync();

        indexes.Should().Contain("ix_audit_events_actor_occurred_on");
        indexes.Should().Contain("ix_audit_events_correlation_id");
    }
}
```

- [ ] **Step 2: Add reference from Infrastructure.Tests to Infrastructure**

```bash
dotnet add backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj reference backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj
```

- [ ] **Step 3: Run the test**

```bash
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo -c Debug 2>&1 | tail -10
```

Expected: 2 passed.

- [ ] **Step 4: Commit**

```bash
git add backend/tests/CCE.Infrastructure.Tests
git -c commit.gpgsign=false commit -m "test(phase-06): add CceDbContext integration tests (round-trip + schema indexes)"
```

---

## Task 6.7: Update API Programs to use new `AddInfrastructure(configuration)` signature

**Files:**

- Modify: `backend/src/CCE.Api.External/Program.cs`
- Modify: `backend/src/CCE.Api.Internal/Program.cs`
- Modify: `backend/src/CCE.Api.External/appsettings.Development.json`
- Modify: `backend/src/CCE.Api.Internal/appsettings.Development.json`

- [ ] **Step 1: Overwrite `backend/src/CCE.Api.External/Program.cs`**

```csharp
using CCE.Application;
using CCE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "CCE.Api.External — Foundation");

app.Run();

public partial class Program;
```

- [ ] **Step 2: Overwrite `backend/src/CCE.Api.Internal/Program.cs` (same change)**

```csharp
using CCE.Application;
using CCE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "CCE.Api.Internal — Foundation");

app.Run();

public partial class Program;
```

- [ ] **Step 3: Update `backend/src/CCE.Api.External/appsettings.Development.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Infrastructure": {
    "SqlConnectionString": "Server=localhost,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=true;",
    "RedisConnectionString": "localhost:6379"
  }
}
```

Note: the password placeholder `Strong!Passw0rd` matches the default `.env.local.example` and is in the Gitleaks allowlist. In real dev, override via env var `Infrastructure__SqlConnectionString`. Production reads from a secret store.

- [ ] **Step 4: Update `backend/src/CCE.Api.Internal/appsettings.Development.json` identically**

(Same content as Step 3.)

- [ ] **Step 5: Build the solution**

```bash
dotnet build backend/CCE.sln --nologo -c Debug 2>&1 | tail -6
```

Expected: 0 errors.

- [ ] **Step 6: Smoke-test API still boots**

```bash
dotnet run --project backend/src/CCE.Api.External --no-build --urls http://localhost:5001 > /tmp/api-external.log 2>&1 &
API_PID=$!
for i in $(seq 1 15); do
  if curl -s http://localhost:5001/ 2>/dev/null | grep -q "Foundation"; then
    break
  fi
  sleep 1
done
RESPONSE=$(curl -s http://localhost:5001/)
kill $API_PID 2>/dev/null; wait $API_PID 2>/dev/null
[ "$RESPONSE" = "CCE.Api.External — Foundation" ] && echo "SMOKE OK" || { echo "SMOKE FAILED"; tail -30 /tmp/api-external.log; exit 1; }
```

Expected: `SMOKE OK`.

- [ ] **Step 7: Commit**

```bash
git add backend/src/CCE.Api.External backend/src/CCE.Api.Internal
git -c commit.gpgsign=false commit -m "feat(phase-06): wire AddInfrastructure(configuration) in both API Programs + appsettings.Development connection strings"
```

---

## Task 6.8: Add append-only SQL trigger for `audit_events`

**Files:**

- Create: `backend/src/CCE.Infrastructure/Persistence/Migrations/<timestamp>_AuditEventsAppendOnlyTrigger.cs` (manual migration)

**Rationale:** Per spec §9.3 — `AuditEvents` table append-only via SQL trigger + domain event. The trigger blocks UPDATE and DELETE on `audit_events` at the database level, so even a misconfigured app or a DBA running raw SQL can't tamper.

- [ ] **Step 1: Create the empty migration**

```bash
cd backend/src/CCE.Infrastructure
dotnet ef migrations add AuditEventsAppendOnlyTrigger \
  --project CCE.Infrastructure.csproj \
  --output-dir Persistence/Migrations \
  --context CceDbContext 2>&1 | tail -3
cd ../../..
```

Expected: creates `<timestamp>_AuditEventsAppendOnlyTrigger.cs` (and `.Designer.cs`).

- [ ] **Step 2: Open the new migration `*_AuditEventsAppendOnlyTrigger.cs` and replace the empty `Up`/`Down` with raw SQL:**

Replace the content of `Up(MigrationBuilder migrationBuilder)`:

```csharp
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_audit_events_no_update_delete
ON dbo.audit_events
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    THROW 51000, 'audit_events is append-only; UPDATE and DELETE are not permitted.', 1;
END;");
```

Replace the content of `Down(MigrationBuilder migrationBuilder)`:

```csharp
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.trg_audit_events_no_update_delete', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_audit_events_no_update_delete;");
```

- [ ] **Step 3: Apply the migration**

```bash
export CCE_DESIGN_SQL_CONN="Server=localhost,1433;Database=CCE;User Id=sa;Password=$(grep ^SQL_PASSWORD .env | cut -d= -f2-);TrustServerCertificate=true;"
cd backend/src/CCE.Infrastructure
dotnet ef database update \
  --project CCE.Infrastructure.csproj \
  --context CceDbContext 2>&1 | tail -5
cd ../../..
```

Expected: `Done.`

- [ ] **Step 4: Add a test proving UPDATE and DELETE are blocked**

Append to `backend/tests/CCE.Infrastructure.Tests/Persistence/CceDbContextTests.cs`:

```csharp
    [Fact]
    public async Task UPDATE_on_audit_events_is_blocked_by_trigger()
    {
        await using var ctx = NewContext();
        var id = Guid.NewGuid();
        var entity = new AuditEvent(
            id, DateTimeOffset.UtcNow,
            actor: "trigger-test", action: "Trigger.Insert", resource: "T",
            correlationId: Guid.NewGuid(), diff: null);
        ctx.AuditEvents.Add(entity);
        await ctx.SaveChangesAsync();

        // Try to update via raw SQL — must throw because of the INSTEAD OF UPDATE trigger
        var act = async () => await ctx.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE audit_events SET actor = 'tampered' WHERE id = {id}");

        await act.Should().ThrowAsync<Microsoft.Data.SqlClient.SqlException>()
            .Where(e => e.Message.Contains("append-only"));

        // Cleanup via trigger-bypassing direct INSERT-style cleanup not possible — leave for next test cycle
    }

    [Fact]
    public async Task DELETE_on_audit_events_is_blocked_by_trigger()
    {
        await using var ctx = NewContext();
        var id = Guid.NewGuid();
        var entity = new AuditEvent(
            id, DateTimeOffset.UtcNow,
            actor: "trigger-delete-test", action: "Trigger.Delete", resource: "T",
            correlationId: Guid.NewGuid(), diff: null);
        ctx.AuditEvents.Add(entity);
        await ctx.SaveChangesAsync();

        var act = async () => await ctx.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM audit_events WHERE id = {id}");

        await act.Should().ThrowAsync<Microsoft.Data.SqlClient.SqlException>()
            .Where(e => e.Message.Contains("append-only"));
    }
```

Note: tests leave audit_events rows behind — by design (append-only). Sub-project 8 will add an admin operation `truncate audit older than N days via stored procedure` for retention.

- [ ] **Step 5: Run the new tests**

```bash
dotnet test backend/tests/CCE.Infrastructure.Tests --nologo -c Debug 2>&1 | tail -10
```

Expected: 4 passed (2 prior + 2 trigger blockers).

- [ ] **Step 6: Commit**

```bash
git add backend/src/CCE.Infrastructure/Persistence/Migrations backend/tests/CCE.Infrastructure.Tests
git -c commit.gpgsign=false commit -m "feat(phase-06): add SQL trigger blocking UPDATE/DELETE on audit_events with 2 integration tests"
```

---

## Task 6.9: Redis smoke test through `IConnectionMultiplexer`

**Files:**

- Create: `backend/tests/CCE.Infrastructure.Tests/Redis/RedisConnectionTests.cs`

**Rationale:** Quick proof that the multiplexer registered in DI can SET/GET a key against the running Redis container. Foundation needs this to confirm the wiring; Phase 08 adds rate limiting and session storage on top.

- [ ] **Step 1: Write the test**

```csharp
using StackExchange.Redis;

namespace CCE.Infrastructure.Tests.Redis;

public class RedisConnectionTests
{
    private const string ConnectionString = "localhost:6379";

    [Fact]
    public async Task Sets_and_gets_a_value()
    {
        await using var muxer = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
        var db = muxer.GetDatabase();
        var key = $"foundation:smoke:{Guid.NewGuid()}";

        await db.StringSetAsync(key, "ok", TimeSpan.FromSeconds(30));
        var value = await db.StringGetAsync(key);

        value.HasValue.Should().BeTrue();
        value.ToString().Should().Be("ok");

        await db.KeyDeleteAsync(key);
    }

    [Fact]
    public async Task Ping_succeeds()
    {
        await using var muxer = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
        var db = muxer.GetDatabase();

        var latency = await db.PingAsync();

        latency.Should().BeGreaterThan(TimeSpan.Zero);
        latency.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }
}
```

- [ ] **Step 2: Run the test**

```bash
dotnet test backend/tests/CCE.Infrastructure.Tests --nologo -c Debug 2>&1 | tail -10
```

Expected: 6 passed (4 prior + 2 Redis).

- [ ] **Step 3: Final solution-wide test run**

```bash
dotnet test backend/CCE.sln --nologo --no-build 2>&1 | tail -10
```

Expected: total 20 passed (10 Domain + 6 Infrastructure + 0 Application + 0 IntegrationTests + 4 just added).

Wait — let me recount: Domain.Tests has 14 (10 prior + 4 AuditEventTests), Infrastructure.Tests has 6 (2 round-trip + 2 trigger + 2 Redis), Application.Tests + IntegrationTests still empty. Total = 20.

- [ ] **Step 4: Commit**

```bash
git add backend/tests/CCE.Infrastructure.Tests/Redis
git -c commit.gpgsign=false commit -m "test(phase-06): add Redis SET/GET + PING smoke tests through IConnectionMultiplexer"
```

---

## Phase 06 — completion checklist

- [ ] `CCE.Infrastructure` has EF Core 8 + SqlServer + NamingConventions + StackExchange.Redis packages.
- [ ] `AuditEvent` domain entity with validated constructor in `CCE.Domain/Audit/`.
- [ ] `CceDbContext` exposes `DbSet<AuditEvent>` and applies snake_case naming.
- [ ] `AuditEventConfiguration` declares table + columns + 2 indexes.
- [ ] `CceInfrastructureOptions` bound from `Infrastructure` config section.
- [ ] `AddInfrastructure(IConfiguration)` registers DbContext + Redis + clock.
- [ ] Two migrations applied to running SQL container: `InitialAuditEvents`, `AuditEventsAppendOnlyTrigger`.
- [ ] Both API Programs call `AddInfrastructure(builder.Configuration)`; smoke endpoint still 200.
- [ ] Tests: 14 Domain + 6 Infrastructure = 20 passed.
- [ ] `git status` clean.
- [ ] ~9 new commits on `main`.

**If all boxes ticked, phase 06 is complete. Proceed to phase 07 (Application layer — health handlers + pipeline behaviors).**

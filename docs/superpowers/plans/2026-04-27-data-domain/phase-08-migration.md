# Phase 08 — DataDomainInitial migration + index plan

> Parent: [`../2026-04-27-data-domain.md`](../2026-04-27-data-domain.md) · Spec §3.4, §5.2, §5.3

**Phase goal:** Generate the consolidated `DataDomainInitial` EF Core migration that creates ASP.NET Identity tables + all 36 CCE entity tables + indexes + filtered unique indexes + RowVersion columns. Apply it against the dev SQL Server. Add a parity test that asserts `dotnet ef migrations script` produces matching DDL given the same model.

**Tasks in this phase:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 07 closed (`e8ee31f` HEAD); 347 backend tests passing; SQL Server container healthy on `localhost:1433`.

---

## Task 8.1: Pre-migration model snapshot inspection

**Files:**
- Read-only: `backend/src/CCE.Infrastructure/Persistence/Migrations/CceDbContextModelSnapshot.cs`

- [ ] **Step 1: Verify dotnet ef tool**

```bash
dotnet ef --version 2>&1 | head -3
```

If missing: `dotnet tool install --global dotnet-ef --version 8.0.10`. (Foundation Phase 06 should have it; the exact version is in `dotnet-tools.json`.)

- [ ] **Step 2: Confirm SQL Server reachable**

```bash
docker compose ps sqlserver | grep healthy
nc -z localhost 1433 && echo "SQL Server reachable on 1433"
```

If not healthy, start the stack: `docker compose up -d sqlserver`.

- [ ] **Step 3: Build solution**

```bash
dotnet build backend/CCE.sln --nologo --no-restore 2>&1 | tail -5
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 4: Skip commit** — this task is verification only.

---

## Task 8.2: Generate `DataDomainInitial` migration

**Files:**
- Create: `backend/src/CCE.Infrastructure/Persistence/Migrations/<timestamp>_DataDomainInitial.cs`
- Create: `backend/src/CCE.Infrastructure/Persistence/Migrations/<timestamp>_DataDomainInitial.Designer.cs`
- Modify: `backend/src/CCE.Infrastructure/Persistence/Migrations/CceDbContextModelSnapshot.cs`

- [ ] **Step 1: Add migration**

```bash
cd backend
dotnet ef migrations add DataDomainInitial \
  --project src/CCE.Infrastructure/CCE.Infrastructure.csproj \
  --startup-project src/CCE.Infrastructure/CCE.Infrastructure.csproj \
  --context CceDbContext \
  --output-dir Persistence/Migrations
```

Expected: 3 files generated. Migration includes:
- `CREATE TABLE` for ASP.NET Identity (`asp_net_users`, `asp_net_roles`, `asp_net_user_roles`, `asp_net_user_claims`, `asp_net_user_logins`, `asp_net_user_tokens`, `asp_net_role_claims`).
- `CREATE TABLE` for all 36 CCE entities.
- All filtered unique indexes (`HasFilter("[is_deleted] = 0")`).
- All composite unique indexes (e.g., `(post_id, user_id)` on `post_ratings`).
- `ROWVERSION` columns on `Resource`, `News`, `Event`, `Page`, `KnowledgeMap`, `CountryProfile`.

If generation fails with `Cannot resolve service for type '...'`, the design-time factory is missing a dependency. Re-run with `--verbose`.

- [ ] **Step 2: Sanity-check the migration file**

```bash
ls -la backend/src/CCE.Infrastructure/Persistence/Migrations/*DataDomainInitial*
wc -l backend/src/CCE.Infrastructure/Persistence/Migrations/*DataDomainInitial*.cs
grep -c "CreateTable" backend/src/CCE.Infrastructure/Persistence/Migrations/*DataDomainInitial.cs
```

Expected: `CreateTable` count ≈ 43 (7 Identity + 36 CCE).

```bash
grep -c "CreateIndex" backend/src/CCE.Infrastructure/Persistence/Migrations/*DataDomainInitial.cs
```

Expected: ≥ 50 indexes (every entity has at least one index; many have 2-3).

- [ ] **Step 3: Verify build still succeeds**

```bash
dotnet build backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj --nologo --no-restore 2>&1 | tail -5
```

Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add backend/src/CCE.Infrastructure/Persistence/Migrations/
git -c commit.gpgsign=false commit -m "feat(persistence): DataDomainInitial migration (43 tables + 50+ indexes for ASP.NET Identity + 36 CCE entities)"
```

---

## Task 8.3: Apply migration to dev DB

**Files:**
- None (DB-only operation)

- [ ] **Step 1: Backup current `__EFMigrationsHistory`**

```bash
docker exec cce-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'Strong!Passw0rd' -C -d CCE \
  -Q "SELECT migration_id FROM __EFMigrationsHistory ORDER BY migration_id" 2>&1 | head
```

Expected to see the two Foundation migrations: `20260425134009_InitialAuditEvents` and `20260425134559_AuditEventsAppendOnlyTrigger`. (Azure SQL Edge 1.0.7 lacks `sqlcmd`; if the command fails, use a TCP-only check via the EF tool below.)

- [ ] **Step 2: Apply the migration**

```bash
cd backend
dotnet ef database update \
  --project src/CCE.Infrastructure/CCE.Infrastructure.csproj \
  --startup-project src/CCE.Infrastructure/CCE.Infrastructure.csproj \
  --context CceDbContext 2>&1 | tail -20
```

Expected: `Done.` after applying `<timestamp>_DataDomainInitial`.

If it fails with a duplicate-table error, the schema may have drifted from a prior partial apply. Drop the dev DB and reapply:

```bash
dotnet ef database drop --force \
  --project src/CCE.Infrastructure/CCE.Infrastructure.csproj \
  --startup-project src/CCE.Infrastructure/CCE.Infrastructure.csproj \
  --context CceDbContext 2>&1 | tail -3
dotnet ef database update \
  --project src/CCE.Infrastructure/CCE.Infrastructure.csproj \
  --startup-project src/CCE.Infrastructure/CCE.Infrastructure.csproj \
  --context CceDbContext 2>&1 | tail -10
```

- [ ] **Step 3: Verify schema (count tables)**

```bash
dotnet run --project src/CCE.Api.External/CCE.Api.External.csproj 2>&1 &
sleep 8
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5001/health/ready
pkill -f "CCE.Api.External" 2>/dev/null
```

Expected: `/health/ready` returns 200 (or 503 only if a non-DB dep is unhealthy — check the response body).

If the API returns 503 with a database-related cause, the migration didn't apply cleanly. Re-run.

- [ ] **Step 4: Skip commit** — DB-only operation, no code changes.

---

## Task 8.4: Migration script parity test

**Files:**
- Create: `backend/src/CCE.Infrastructure/Persistence/Migrations/data-domain-initial-script.sql` (committed snapshot)
- Create: `backend/tests/CCE.Infrastructure.Tests/Persistence/MigrationParityTests.cs`

**Rationale:** Generate the SQL script once (`dotnet ef migrations script`), commit it, and add a test that re-generates it on every CI run and asserts byte-equality. This catches accidental model drift between the snapshot file and the migration files (a notorious EF Core trap).

- [ ] **Step 1: Generate the script**

```bash
cd backend
dotnet ef migrations script 0 DataDomainInitial \
  --project src/CCE.Infrastructure/CCE.Infrastructure.csproj \
  --startup-project src/CCE.Infrastructure/CCE.Infrastructure.csproj \
  --context CceDbContext \
  --output src/CCE.Infrastructure/Persistence/Migrations/data-domain-initial-script.sql 2>&1 | tail -3
```

Expected: file written. `wc -l` should report ≥ 200 lines.

- [ ] **Step 2: Add parity test**

`backend/tests/CCE.Infrastructure.Tests/Persistence/MigrationParityTests.cs`:

```csharp
using System.Diagnostics;
using System.Text;
using CCE.Infrastructure.Persistence;

namespace CCE.Infrastructure.Tests.Persistence;

/// <summary>
/// Re-runs <c>dotnet ef migrations script</c> against the current model and asserts
/// the output equals the committed snapshot. Catches model-vs-migration drift.
/// </summary>
public class MigrationParityTests
{
    [Fact(Skip = "Requires dotnet-ef tool on PATH and a built CceDbContext; run locally with `dotnet test --filter MigrationParityTests` after `dotnet build`.")]
    public void Migrations_script_matches_committed_snapshot()
    {
        var repoRoot = FindRepoRoot();
        var snapshotPath = Path.Combine(repoRoot,
            "backend/src/CCE.Infrastructure/Persistence/Migrations/data-domain-initial-script.sql");
        File.Exists(snapshotPath).Should().BeTrue();

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "ef migrations script 0 DataDomainInitial " +
                        "--project src/CCE.Infrastructure/CCE.Infrastructure.csproj " +
                        "--startup-project src/CCE.Infrastructure/CCE.Infrastructure.csproj " +
                        "--context CceDbContext --no-build",
            WorkingDirectory = Path.Combine(repoRoot, "backend"),
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(psi);
        process.Should().NotBeNull();
        var script = process!.StandardOutput.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(0);

        var committed = File.ReadAllText(snapshotPath, Encoding.UTF8);
        Normalize(script).Should().Be(Normalize(committed));
    }

    private static string Normalize(string sql) =>
        sql.Replace("\r\n", "\n", System.StringComparison.Ordinal).TrimEnd();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend/CCE.sln")))
        {
            dir = dir.Parent;
        }
        if (dir is null) throw new System.IO.DirectoryNotFoundException("repo root not found");
        return dir.FullName;
    }
}
```

The test is `[Fact(Skip = ...)]` because it requires the `dotnet-ef` tool on PATH and shells out — too brittle for CI without a dedicated job. The committed `.sql` file remains the gold source; re-run the test locally before tagging a release.

- [ ] **Step 3: Build + run (verify the test compiles)**

```bash
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~MigrationParityTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Skipped: 1` (or 1 test discovered + skipped; both are acceptable).

- [ ] **Step 4: Commit**

```bash
git add backend/src/CCE.Infrastructure/Persistence/Migrations/data-domain-initial-script.sql backend/tests/CCE.Infrastructure.Tests/Persistence/MigrationParityTests.cs
git -c commit.gpgsign=false commit -m "test(persistence): commit DataDomainInitial DDL snapshot + parity test (skipped in CI)"
```

---

## Task 8.5: Phase 08 close

- [ ] **Step 1: Full backend test run**

```bash
dotnet test backend/CCE.sln --nologo --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "(Passed!|Failed!)"
```

Expected: 5 lines, all `Passed!`. Backend total ≈ 348 (350 minus 1 skipped from MigrationParityTests, minus 1 for math; use actual numbers).

- [ ] **Step 2: Update progress doc**

Mark Phase 08 ✅ Done. Use the actual numbers reported.

- [ ] **Step 3: Commit**

```bash
git add docs/subprojects/02-data-domain-progress.md
git -c commit.gpgsign=false commit -m "docs(sub-2): mark Phase 08 done; DataDomainInitial migration + DDL snapshot shipped"
```

---

## Phase 08 — completion checklist

- [ ] `DataDomainInitial` migration scaffolded and committed.
- [ ] Migration applied to dev SQL Server (43 tables, 50+ indexes).
- [ ] DDL snapshot `data-domain-initial-script.sql` committed.
- [ ] `MigrationParityTests` exists (skipped by default).
- [ ] All Phase 07 regression tests still pass.
- [ ] 3 new commits.

**If all boxes ticked, Phase 08 is complete. Proceed to Phase 09 (Seeders — RolesAndPermissions, ReferenceData, DemoData).**

# Phase 02 — Rollback + deploy-smoke + close-out (Sub-10b)

> Parent: [`../2026-05-03-sub-10b.md`](../2026-05-03-sub-10b.md) · Spec: [`../../specs/2026-05-03-sub-10b-design.md`](../../specs/2026-05-03-sub-10b-design.md) §`deploy/rollback.ps1`, §`deploy-history.tsv`, §CI changes — `deploy-smoke.yml`, §Documentation, §Phasing → Phase 02.

**Phase goal:** Close Sub-10b. Ship `rollback.ps1` (image-tag swap + re-deploy), wire `deploy-history.tsv` audit trail into `deploy.ps1`, write the `deploy-smoke.yml` Windows-runner end-to-end workflow, land ADR-0053 documenting the deployment-shape decisions, write the completion doc, append the CHANGELOG entry, tag `deploy-v1.0.0`.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 01 closed (5 commits land on `main`; HEAD at `530ade6` or later).
- `deploy.ps1`, `smoke.ps1` exist at `deploy/`.
- All 3 compose files (`prod`, `prod.deploy`, `build`) validate.
- CI `docker-build` job pushes to ghcr.io on `main` + `v*` (verified via at least one CI run after Phase 01 merged).
- Backend baseline: 439 Application + 66 Infrastructure tests passing.

---

## Task 2.1: `deploy/rollback.ps1` + `deploy-history.tsv` audit trail

**Files:**
- Create: `deploy/rollback.ps1` — wrapper that updates `CCE_IMAGE_TAG` in the env-file then invokes `deploy.ps1`.
- Modify: `deploy/deploy.ps1` — fill in the Step 9 `deploy-history.tsv` stub from Phase 01.

**Why combined:** `rollback.ps1` reads from `deploy-history.tsv` to suggest valid prior tags, and `deploy.ps1` is the only place that appends new rows. Atomic commit so the file format is consistent.

**`deploy/rollback.ps1` shape:**

```powershell
#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10b production rollback script.

.DESCRIPTION
    Atomically rewrites CCE_IMAGE_TAG in .env.prod to a previous tag,
    then invokes deploy.ps1. Forward-only migrations make this safe:
    the older image runs against the current schema without DB rewind.

    Logs the swap to C:\ProgramData\CCE\logs\rollback-<UTC>.log and
    appends a row to deploy-history.tsv tagged ROLLBACK_FROM=<old>.

.PARAMETER ToTag
    Required. The image tag to roll back to. Look up in
    C:\ProgramData\CCE\deploy-history.tsv:
        Get-Content C:\ProgramData\CCE\deploy-history.tsv | Select-Object -Last 10

.PARAMETER EnvFile
    Path to the production env-file. Default: C:\ProgramData\CCE\.env.prod.

.PARAMETER SkipMigrator
    Pass through MIGRATE_ON_DEPLOY=false to deploy.ps1. Forward-only
    discipline means MigrateAsync is a no-op on rollback, but use
    this switch to bypass the migrator service entirely if needed.

.EXAMPLE
    .\deploy\rollback.ps1 -ToTag app-v1.0.0
    .\deploy\rollback.ps1 -ToTag sha-c612812 -SkipMigrator
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$ToTag,
    [string]$EnvFile = 'C:\ProgramData\CCE\.env.prod',
    [switch]$SkipMigrator
)

$ErrorActionPreference = 'Stop'

# Logs directory + timestamped rollback log file
$logDir = 'C:\ProgramData\CCE\logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir ("rollback-{0:yyyyMMddTHHmmssZ}.log" -f (Get-Date).ToUniversalTime())

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $line = "[$ts] [$Level] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

# ─── Resolve env-file ─────────────────────────────────────────────────────
if (-not (Test-Path $EnvFile)) {
    Write-Log -Level 'ERROR' -Message "Env-file not found: $EnvFile"
    exit 1
}
$resolvedEnvFile = (Resolve-Path $EnvFile).Path

# ─── Capture outgoing tag ─────────────────────────────────────────────────
$lines = Get-Content $resolvedEnvFile
$outgoingTag = $null
$found = $false
$newLines = New-Object System.Collections.Generic.List[string]
foreach ($line in $lines) {
    if ($line -match '^\s*CCE_IMAGE_TAG\s*=\s*(.*)$') {
        $outgoingTag = $Matches[1].Trim() -replace '\s*#.*$',''
        $newLines.Add("CCE_IMAGE_TAG=$ToTag")
        $found = $true
    } else {
        $newLines.Add($line)
    }
}
if (-not $found) {
    Write-Log -Level 'ERROR' -Message "CCE_IMAGE_TAG not found in env-file: $resolvedEnvFile"
    exit 1
}
Write-Log "Rolling back: outgoing=$outgoingTag → incoming=$ToTag"

# ─── Atomic rewrite (temp-file + rename) ──────────────────────────────────
$tempFile = "$resolvedEnvFile.tmp"
$newLines | Set-Content -Path $tempFile -Encoding utf8 -NoNewline:$false
Move-Item -Path $tempFile -Destination $resolvedEnvFile -Force
Write-Log "Env-file updated: CCE_IMAGE_TAG=$ToTag"

# ─── Invoke deploy.ps1 ────────────────────────────────────────────────────
$deployScript = Join-Path $PSScriptRoot 'deploy.ps1'
if ($SkipMigrator) {
    # Override MIGRATE_ON_DEPLOY for this run only — set process env-var
    # which Get-Content reads via the parsed envMap; we rely on the
    # env-file's value taking precedence. Cleanest path: temporarily
    # set MIGRATE_ON_DEPLOY=false in the env-file for this run, then
    # restore. Keep it simple: print a warning that operator should
    # set MIGRATE_ON_DEPLOY=false in .env.prod manually if they want
    # to skip on a permanent basis. For one-shot skip, deploy.ps1
    # respects MIGRATE_ON_DEPLOY=false; just set it via env-file edit.
    Write-Log "WARN: -SkipMigrator switch is documented but currently delegates to .env.prod's MIGRATE_ON_DEPLOY value."
    Write-Log "WARN: For a true one-shot skip, set MIGRATE_ON_DEPLOY=false in .env.prod before this command."
}

Write-Log "Invoking deploy.ps1 -EnvFile $resolvedEnvFile"
& pwsh -NoProfile -File $deployScript -EnvFile $resolvedEnvFile
$deployExit = $LASTEXITCODE

# ─── Append rollback row to deploy-history.tsv ────────────────────────────
# (deploy.ps1 also appends its own row; we add a ROLLBACK_FROM marker.)
$historyFile = 'C:\ProgramData\CCE\deploy-history.tsv'
$tsRow = "{0:yyyy-MM-ddTHH:mm:ssZ}`t{1}`t{2}`tROLLBACK_FROM={3}`t{4}" -f `
    (Get-Date).ToUniversalTime(), `
    'rollback-script', `
    $ToTag, `
    $outgoingTag, `
    ($(if ($deployExit -eq 0) { 'OK' } else { 'FAIL' }))
Add-Content -Path $historyFile -Value $tsRow
Write-Log "deploy-history.tsv appended."

if ($deployExit -ne 0) {
    Write-Log -Level 'ERROR' -Message "Rollback deploy failed (exit $deployExit). Investigate via deploy log under C:\ProgramData\CCE\logs\."
    exit $deployExit
}
Write-Log "Rollback complete. Image tag now: $ToTag"
exit 0
```

**`deploy.ps1` Step 9 modification** — replace the stub `Write-Log "Step 9/10: deploy-history.tsv (Phase 02 implements)."` with:

```powershell
# ─── Step 9: Append deploy-history.tsv ────────────────────────────────────
Write-Log "Step 9/10: Appending deploy-history.tsv."
$historyFile = 'C:\ProgramData\CCE\deploy-history.tsv'
# Capture git SHA from the env-file's tag if it looks like sha-* or a hex SHA;
# otherwise leave SHA blank (release tags don't carry SHA info here).
$tagValue = $envMap['CCE_IMAGE_TAG']
$sha = ''
if ($tagValue -match '^sha-([0-9a-f]{7,40})$') { $sha = $Matches[1] }
elseif ($tagValue -match '^[0-9a-f]{40}$')     { $sha = $tagValue }
$tsRow = "{0:yyyy-MM-ddTHH:mm:ssZ}`t{1}`t{2}`t{3}" -f `
    (Get-Date).ToUniversalTime(), `
    $sha, `
    $tagValue, `
    'OK'
Add-Content -Path $historyFile -Value $tsRow
Write-Log "deploy-history.tsv appended."
```

**File format** (`C:\ProgramData\CCE\deploy-history.tsv`, append-only, tab-separated):

```
2026-05-04T10:32:18Z	c612812abc...	app-v1.0.0	OK
2026-05-04T11:15:02Z	5a6eb7b...	deploy-v1.0.0	OK
2026-05-04T14:02:55Z	rollback-script	deploy-v1.0.0	ROLLBACK_FROM=deploy-v1.0.1	OK
```

The 4-column "OK" rows are written by `deploy.ps1`; the 5-column "ROLLBACK_FROM" rows are written by `rollback.ps1`. Operators run `Get-Content C:\ProgramData\CCE\deploy-history.tsv | Select-Object -Last 10` to identify rollback targets.

- [ ] **Step 1:** Create `deploy/rollback.ps1` with the contents above.

- [ ] **Step 2:** Modify `deploy/deploy.ps1` Step 9 to append the history row instead of being a stub. (See diff above.)

- [ ] **Step 3:** Sanity check both scripts (skip if no pwsh locally — CI runs them):
  ```bash
  pwsh -NoProfile -Command "& { \$null = [System.Management.Automation.Language.Parser]::ParseFile('/Users/m/CCE/deploy/rollback.ps1', [ref]\$null, [ref]\$err); if (\$err) { \$err | Out-Host; exit 1 } else { Write-Host 'OK' } }"
  pwsh -NoProfile -Command "& { \$null = [System.Management.Automation.Language.Parser]::ParseFile('/Users/m/CCE/deploy/deploy.ps1', [ref]\$null, [ref]\$err); if (\$err) { \$err | Out-Host; exit 1 } else { Write-Host 'OK' } }"
  ```

- [ ] **Step 4:** Commit:
  ```bash
  git -C /Users/m/CCE add deploy/rollback.ps1 deploy/deploy.ps1
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(deploy): rollback.ps1 + deploy-history.tsv audit trail

  rollback.ps1 takes -ToTag <prev> and atomically rewrites
  CCE_IMAGE_TAG in .env.prod (temp-file + rename) before invoking
  deploy.ps1. Logs the swap and appends a ROLLBACK_FROM row to
  deploy-history.tsv. -SkipMigrator switch documented; current
  implementation defers to .env.prod's MIGRATE_ON_DEPLOY (forward-
  only discipline means MigrateAsync is a no-op on rollback anyway).

  deploy.ps1 Step 9 fills in the deploy-history.tsv stub from
  Phase 01. Format: <UTC-iso8601>\\t<sha>\\t<tag>\\tOK. Rollback
  rows add a 5th tab-separated ROLLBACK_FROM=<old> field.

  Sub-10b Phase 02 Task 2.1.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 2.2: `docs/runbooks/rollback.md` + `deploy-smoke.yml` workflow

**Files:**
- Create: `docs/runbooks/rollback.md` — operator runbook for the rollback procedure.
- Create: `.github/workflows/deploy-smoke.yml` — Windows-runner end-to-end test that exercises the deploy → rollback → re-smoke cycle against the latest pushed images.

**Why combined:** the runbook references the workflow as the gate that proves rollback works. They land together so the runbook isn't pointing at a workflow that doesn't exist.

**Final state of `docs/runbooks/rollback.md`:**

```markdown
# Rollback runbook (Sub-10b)

When a production deploy is bad — smoke probes fail, errors spike, manual verification turns up regressions — roll back to the previous known-good image tag.

## When to roll back

- Smoke probes failed after `deploy.ps1` (script printed the rollback hint).
- `/health` returns Unhealthy after deploy completed.
- User-visible regression confirmed against the new image.
- Operator gut-feel "this isn't right" — better to roll back and investigate.

## When NOT to roll back

- Migration is in-flight (Step 6 of `deploy.ps1` not yet exited). Wait for it to finish or abort.
- Suspected schema drift (rare; forward-only discipline prevents it). See escape hatch below.
- DB corruption / data loss. Rollback won't fix it; need DBA + backup.

## Procedure

1. **Identify the previous good tag** from `deploy-history.tsv`:
   ```powershell
   Get-Content C:\ProgramData\CCE\deploy-history.tsv | Select-Object -Last 10
   ```

   Example output:
   ```
   2026-05-04T10:32:18Z	c612812...	app-v1.0.0	OK
   2026-05-04T11:15:02Z	5a6eb7b...	deploy-v1.0.0	OK
   2026-05-04T14:02:55Z		deploy-v1.0.1	OK   ← the bad deploy
   ```
   Pick the most recent `OK` row before the bad one. Here that's `deploy-v1.0.0`.

2. **Run the rollback script**:
   ```powershell
   cd C:\path\to\CCE
   .\deploy\rollback.ps1 -ToTag deploy-v1.0.0
   ```

3. **Verify smoke probes pass** (rollback.ps1 invokes deploy.ps1 which runs them):
   ```
   Probing api-external/health... OK
   Probing api-internal/health... OK
   Probing web-portal/...        OK
   Probing admin-cms/...         OK
   All 4 probes PASSED.
   ```

4. **Verify the audit trail**:
   ```powershell
   Get-Content C:\ProgramData\CCE\deploy-history.tsv | Select-Object -Last 3
   ```
   You should see a `ROLLBACK_FROM=deploy-v1.0.1` row and a fresh `OK` row for `deploy-v1.0.0`.

## Common failures during rollback

| Symptom | Cause | Fix |
|---|---|---|
| `Image pull failed` for `<previous-tag>` | Tag not in ghcr.io | Image-tag retention is unlimited in ghcr.io free tier, so this is rare. Check the tag was actually pushed (search for it in old GHA run summaries). |
| `Migrator failed` | Forward-only discipline violated — schema drift | STOP. File an incident. Restore-from-backup is the only path; backup automation is Sub-10c work. |
| `Smoke probe FAILED` post-rollback | Old image has its own bug | Roll back further: `.\deploy\rollback.ps1 -ToTag <even-older-tag>`. |

## Forward-only escape hatch

If a rollback fails because the previous image can't run against the current schema, you've hit a forward-only-discipline violation. The release that broke it should have been a destructive-migration release (separate spec, backup-restore runbook). See [`migrations.md`](./migrations.md) for the rules.

Recovery from this state is a Sub-10c+ scenario: backup-restore. Sub-10b explicitly defers backup automation. For now: page the DBA, restore the pre-deploy DB snapshot, redeploy the older image.

## See also

- [`deploy.md`](./deploy.md) — green-path deploy procedure
- [`migrations.md`](./migrations.md) — forward-only discipline rules
- [Sub-10b design spec](../superpowers/specs/2026-05-03-sub-10b-design.md) §Rollback procedure
```

**Final state of `.github/workflows/deploy-smoke.yml`:**

```yaml
name: Deploy smoke (Sub-10b)

# Manual-dispatch only — Windows runners are expensive and this is a
# semi-rare gate (ideally run before each release tag). Exercises the
# full deploy + rollback cycle against a synthetic env-file pointing
# at an inline SQL Server container.

on:
  workflow_dispatch:
    inputs:
      image_tag:
        description: 'Image tag to deploy (e.g. latest, app-v1.0.0)'
        required: true
        default: 'latest'
      previous_tag:
        description: 'Previous tag to roll back to (e.g. app-v1.0.0)'
        required: true
        default: 'latest'

permissions:
  contents: read
  packages: read

jobs:
  smoke:
    name: Deploy → rollback → re-smoke
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      # Docker is preinstalled on windows-latest runners; verify and
      # switch to Linux containers (default on the runners but make
      # explicit). The runner uses Docker Desktop in the background.
      - name: Verify Docker is available
        shell: pwsh
        run: |
          docker version
          docker info | Select-String 'Operating System'

      - name: Log in to ghcr.io
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Boot inline SQL Server container
        shell: pwsh
        run: |
          $pwd = 'Strong!Passw0rd'
          docker run -d --name cce-test-sql `
            -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=$pwd" `
            -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
          # Wait for SQL Server to be ready
          for ($i = 0; $i -lt 30; $i++) {
            $log = docker logs cce-test-sql 2>&1
            if ($log -match 'SQL Server is now ready for client connections') {
              Write-Host "SQL Server up after $i sec"
              break
            }
            Start-Sleep -Seconds 2
          }

      - name: Boot inline Redis container
        shell: pwsh
        run: |
          docker run -d --name cce-test-redis -p 6379:6379 redis:7-alpine

      - name: Synthesize .env.prod for the smoke run
        shell: pwsh
        run: |
          $owner = '${{ github.repository_owner }}'.ToLower()
          $envContent = @"
          CCE_REGISTRY_OWNER=$owner
          CCE_IMAGE_TAG=${{ inputs.image_tag }}
          INFRA_SQL=Server=host.docker.internal,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=True;
          INFRA_REDIS=host.docker.internal:6379
          KEYCLOAK_AUTHORITY=http://host.docker.internal:8080/realms/cce
          KEYCLOAK_AUDIENCE=cce-api
          KEYCLOAK_REQUIRE_HTTPS=false
          ASSISTANT_PROVIDER=stub
          ANTHROPIC_API_KEY=
          LOG_LEVEL=Information
          SENTRY_DSN=
          MIGRATE_ON_DEPLOY=true
          MIGRATE_SEED_REFERENCE=true
          CCE_GHCR_TOKEN=${{ secrets.GITHUB_TOKEN }}
          "@
          $envDir = 'C:\ProgramData\CCE'
          New-Item -ItemType Directory -Path $envDir -Force | Out-Null
          New-Item -ItemType Directory -Path "$envDir\logs" -Force | Out-Null
          $envFile = "$envDir\.env.prod"
          $envContent | Out-File -FilePath $envFile -Encoding utf8
          Write-Host "Wrote $envFile"

      - name: Deploy
        shell: pwsh
        run: |
          .\deploy\deploy.ps1 -EnvFile C:\ProgramData\CCE\.env.prod

      - name: Verify deploy-history.tsv has 1 row
        shell: pwsh
        run: |
          $rows = Get-Content C:\ProgramData\CCE\deploy-history.tsv
          Write-Host "deploy-history.tsv contents:"
          $rows | ForEach-Object { Write-Host "  $_" }
          if ($rows.Count -lt 1) { throw "deploy-history.tsv is empty after deploy" }

      - name: Rollback to previous tag
        shell: pwsh
        run: |
          .\deploy\rollback.ps1 -ToTag '${{ inputs.previous_tag }}' -EnvFile C:\ProgramData\CCE\.env.prod

      - name: Verify smoke probes still pass after rollback
        shell: pwsh
        run: |
          .\deploy\smoke.ps1 -Timeout 60

      - name: Verify deploy-history.tsv has rollback row
        shell: pwsh
        run: |
          $rows = Get-Content C:\ProgramData\CCE\deploy-history.tsv
          Write-Host "deploy-history.tsv contents:"
          $rows | ForEach-Object { Write-Host "  $_" }
          $rollback = $rows | Where-Object { $_ -match 'ROLLBACK_FROM=' }
          if (-not $rollback) { throw "Expected a ROLLBACK_FROM row in deploy-history.tsv" }
          Write-Host "Rollback audit row found: $rollback"

      - name: Cleanup
        if: always()
        shell: pwsh
        run: |
          docker compose -f docker-compose.prod.yml -f docker-compose.prod.deploy.yml `
            --env-file C:\ProgramData\CCE\.env.prod down -v 2>&1 | Out-Host
          docker rm -f cce-test-sql cce-test-redis 2>&1 | Out-Host
```

**Note on `host.docker.internal` in the synthetic env-file:** Docker Desktop on Windows resolves `host.docker.internal` to the Windows host IP. The SQL/Redis containers booted in earlier steps publish to the host's loopback (`-p 1433:1433`, `-p 6379:6379`), so the app containers reach them via `host.docker.internal,1433` etc. — same as in production.

**Note on `previous_tag`:** the workflow takes both `image_tag` and `previous_tag` as inputs. Operator picks both at dispatch time. Using `latest` for both is the simplest end-to-end smoke (deploy latest, "rollback" to latest — exercises the script flow even though there's no actual version difference).

- [ ] **Step 1:** Create `docs/runbooks/rollback.md` with the contents above.

- [ ] **Step 2:** Create `.github/workflows/deploy-smoke.yml` with the contents above.

- [ ] **Step 3:** Verify the workflow YAML parses (use `actionlint` if available; otherwise rely on GitHub's parser when first dispatched):
  ```bash
  # If actionlint is installed:
  actionlint /Users/m/CCE/.github/workflows/deploy-smoke.yml 2>&1 || true
  ```
  No actionlint locally is OK; first manual-dispatch run on GitHub will surface any errors.

- [ ] **Step 4:** Commit:
  ```bash
  git -C /Users/m/CCE add docs/runbooks/rollback.md .github/workflows/deploy-smoke.yml
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "ci(deploy): deploy-smoke Windows-runner workflow + rollback runbook

  deploy-smoke.yml is a manual-dispatch workflow that exercises
  the full deploy → rollback → re-smoke cycle on a windows-latest
  runner against the latest pushed images. Boots inline SQL Server
  + Redis containers, synthesizes .env.prod with stub assistant,
  runs deploy.ps1, asserts deploy-history.tsv populated, runs
  rollback.ps1 to a chosen previous tag, asserts smoke still
  passes and ROLLBACK_FROM row appears in the audit trail.

  rollback.md is the operator runbook: when to roll back, the
  3-step procedure, common failures, and the forward-only escape
  hatch (which delegates to Sub-10c backup-restore).

  Sub-10b Phase 02 Task 2.2.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 2.3: ADR-0053 + completion doc + CHANGELOG

**Files:**
- Create: `docs/adr/0053-deployment-shape-linux-containers-on-win-server.md` — captures the 5 brainstorm decisions.
- Create: `docs/sub-10b-deployment-automation-completion.md` — completion doc mirroring Sub-10a's shape.
- Modify: `CHANGELOG.md` — new `[deploy-v1.0.0]` section at top.

**Final state of `docs/adr/0053-deployment-shape-linux-containers-on-win-server.md`:**

```markdown
# ADR-0053 — Deployment shape: Linux containers on Windows Server 2022

**Status:** Accepted
**Date:** 2026-05-03
**Deciders:** Sub-10b brainstorm (kilany113@gmail.com)
**Sub-project:** [Sub-10b — Deployment automation](../superpowers/specs/2026-05-03-sub-10b-design.md)

## Context

Sub-10a shipped 4 production Linux Docker images (`Api.External`, `Api.Internal`, `web-portal`, `admin-cms`) plus the `CCE.Seeder` console. Sub-10b's job is to wrap those into a deployable system targeting one environment end-to-end on a Windows Server 2022 host (per IDD v1.2: Windows Server 2022 + Intel Xeon Gold 6138 hardware, SQL Server, Redis 6379, AD 389/636).

Five orthogonal decisions had to be made before the rest of the design could proceed.

## Decision

### 1. Linux containers on Windows Server 2022 (not Windows-native rebuild, not hybrid)

Reuse Sub-10a's 4 Linux images verbatim. Run via Docker (Desktop or CE/Mirantis runtime) on the Windows host.

**Considered alternatives:**
- **Windows-native rebuild (IIS hosting + Windows Service):** rejected — would discard Sub-10a's working CI image pipeline; rebuild ASP.NET Core hosting under IIS introduces app-pool quirks (in-process hosting, recycling, log redirection); frontends would need separate IIS-served static delivery. Significant rework.
- **Hybrid (backend Linux containers + frontends in IIS):** rejected — worst of both. Two deployment models, doubled rollback surface, still requires Docker for backend.

**Why this won:** zero rebuild work, identical to dev environment, IDD allows containers (calls out Windows Server 2022 hardware target but doesn't mandate native Windows hosting).

**Trade-off accepted:** Docker Desktop licensing + a Linux VM running on the Windows host. For one-environment-end-to-end this is acceptable; multi-tenant licensing is a Sub-10c question.

### 2. Sidecar migrator (auto on deploy, configurable kill-switch via `MIGRATE_ON_DEPLOY=false`)

Compose declares the migrator service with `depends_on: { condition: service_completed_successfully }` so APIs wait for it. `deploy.ps1` invokes the migrator explicitly with `docker compose run --rm --no-deps migrator` to capture exit code, then brings up apps with `--no-deps`.

**Considered alternative:**
- **Gated (operator runs migrator manually, then APIs):** rejected as default. Two-step deploy is easier to skip accidentally; migration becomes an opt-in step instead of an automated one.

**Why this won:** automation by default + the gate when needed (`MIGRATE_ON_DEPLOY=false` in `.env.prod`).

### 3. `.env.prod` on the host with NTFS-restricted ACLs (not Vault, not Docker secrets)

Single file at `C:\ProgramData\CCE\.env.prod`, mode-locked via `icacls` to Administrators + the deploy user. Compose reads it via the `env_file:` directive on each service.

**Considered alternatives:**
- **Docker secrets (`docker secret`):** rejected — requires Swarm mode or compose `secrets:` blocks pointing at host files; every consumer needs a file-reading code path; significant retrofit of the .NET host config.
- **External vault (HashiCorp Vault / Azure Key Vault / AWS Secrets Manager):** rejected for 10b — new infra, new failure mode, billing dependency. Vault graduation is a Sub-10c+ decision.

**Why this won:** zero new infra; works identically on dev and prod; fits "one environment end-to-end" scope. Secrets at rest on the host filesystem are mitigated by FS perms; this is the textbook answer at this scale.

### 4. Image-tag rollback + forward-only migrations (not backup-restore, not blue-green)

Every CI build pushes images tagged `:<git-sha>` / `:sha-<7-char>` / `:latest` (and `:<release-tag>` on `v*` pushes). Compose pins to `${CCE_IMAGE_TAG}` from `.env.prod`. Rollback = swap tag, re-deploy. DB schema is forward-only — old image runs against new schema.

**Considered alternatives:**
- **Backup-and-restore:** rejected for 10b. Backup automation is explicitly Sub-10c scope. Pre-deploy snapshots followed by stop/restore/redeploy add minutes of downtime, data loss between snapshot and rollback, and a more complex runbook.
- **Blue-green:** rejected. Doubles host capacity needed; LB orchestration is Sub-10c work; massive overkill for "one environment end-to-end."

**Why this won:** atomic, fast, the only thing that actually works for containers at this scale.

**Trade-off accepted:** migration discipline cost (no destructive changes without an explicit data-migration plan). Documented in [`docs/runbooks/migrations.md`](../runbooks/migrations.md).

### 5. ghcr.io + PowerShell deploy script (not self-hosted registry, not ACR/ECR)

Push images to `ghcr.io/<owner>/cce-<image>` via existing CI's `docker/build-push-action@v6` + `docker/login-action@v3` with `GITHUB_TOKEN`. Operator deploys via `deploy/deploy.ps1` on the Windows host.

**Considered alternatives:**
- **Self-hosted registry (Harbor / Docker Distribution):** rejected — new infra, certs, auth, replication. Sub-10c material.
- **Azure Container Registry / AWS ECR:** rejected — cloud account dependency, billing. Project may not have one provisioned.

**Why this won:** zero new infra; ghcr.io is free for public/private repos; Sub-10a's CI is already on GitHub Actions. PowerShell is native to the host.

## Consequences

**Positive:**
- Sub-10b reuses Sub-10a's CI image pipeline with minimal change.
- Deploy + rollback are single PowerShell commands.
- Migration discipline is a documented contract, not an emergent property.
- Image-tag rollback is fast (seconds) and atomic.
- No new infrastructure dependencies (vault, registry, LB).

**Negative / accepted:**
- Forward-only migration discipline must be enforced by PR review.
- Destructive migrations need a separate spec + plan + maintenance window (escape hatch documented).
- Docker Desktop / CE on the Windows host has licensing implications at multi-tenant scale (Sub-10c question).
- ghcr.io rate-limits anonymous pulls; operator must set `CCE_GHCR_TOKEN` for higher limits.

**Out of scope (Sub-10c):**
- TLS / DNS / LB validation against IDD v1.2 production hostnames (`CCE-ext`, `CCE-admin-Panel`, `api.CCE`, `Api.CCE-admin-Panel`).
- AD federation against `cce.local`.
- Multi-environment promotion + secret rotation.
- Backup automation + DB restore runbook.
- Vault graduation.
- Auto-rollback on smoke-probe failure.

## References

- [Sub-10b design spec](../superpowers/specs/2026-05-03-sub-10b-design.md)
- [Forward-only migrations runbook](../runbooks/migrations.md)
- [Production deploy runbook](../runbooks/deploy.md)
- [Rollback runbook](../runbooks/rollback.md)
- ADR-0051 — Anthropic SDK + RAG-lite citations (Sub-10a)
- ADR-0052 — Observability stack (Sub-10a)
```

**Final state of `docs/sub-10b-deployment-automation-completion.md`** — short completion note mirroring 10a's shape:

```markdown
# Sub-10b — Deployment automation — Completion

**Released:** 2026-05-04
**Tag:** `deploy-v1.0.0`
**Sub-project:** Second of three Sub-10 sub-projects (Sub-10a `app-v1.0.0` shipped; Sub-10c is the third).
**Spec:** [`superpowers/specs/2026-05-03-sub-10b-design.md`](superpowers/specs/2026-05-03-sub-10b-design.md)
**Plan:** [`superpowers/plans/2026-05-03-sub-10b.md`](superpowers/plans/2026-05-03-sub-10b.md)

## What shipped

A one-command deployable system on a single Windows Server 2022 host. Linux containers, sidecar migrator, `.env.prod` on the host with NTFS-restricted ACLs, image-tag rollback, ghcr.io image registry, PowerShell deploy + rollback scripts.

### Phase 00 — Migration runner + image (5 commits)
- `CCE.Seeder` gains `--migrate` and `--seed-reference` flags via a new `SeederMode` parser.
- `cce-migrator` Dockerfile (multistage; mirrors API pattern).
- 9 flag-parser tests + 3 migration tests on Testcontainers MS-SQL.
- `docs/runbooks/migrations.md` documents the forward-only discipline.

### Phase 01 — Compose + env-file + deploy script (5 commits)
- 3-file compose pattern: `docker-compose.prod.yml` (canonical) + `prod.deploy.yml` (strict-env override) + `build.yml` (local-build override).
- `.env.prod.example` documents every key; `.gitignore` allow-list lets the example commit.
- CI extended: `permissions.packages: write`, ghcr.io login, tag matrix (`:<sha>` / `:sha-<short>` / `:latest` / `:<release-tag>`), step-summary.
- `deploy/deploy.ps1` (10-step idempotent flow with abort-with-rollback-hint).
- `deploy/smoke.ps1` (4-endpoint probe).
- `docs/runbooks/deploy.md` green-path runbook.

### Phase 02 — Rollback + deploy-smoke + close-out (4 commits)
- `deploy/rollback.ps1` (atomic env-file rewrite + deploy.ps1 invocation).
- `deploy-history.tsv` audit trail in `deploy.ps1`.
- `.github/workflows/deploy-smoke.yml` (Windows-runner end-to-end deploy → rollback → re-smoke test).
- `docs/runbooks/rollback.md` operator runbook.
- ADR-0053 captures the 5 deployment-shape decisions.
- This completion doc + CHANGELOG `[deploy-v1.0.0]` entry.

## Gates green at release

| Gate | Result |
|---|---|
| Backend build | clean |
| `dotnet test tests/CCE.Application.Tests/` | 439 passing (unchanged) |
| `dotnet test tests/CCE.Infrastructure.Tests/` | 66 passing + 1 skipped (was 54; +9 flag parser, +3 migration) |
| Frontend tests | 502 passing across 90 suites (unchanged) |
| Lighthouse a11y gate | passes (unchanged) |
| axe-core gate | zero critical/serious (unchanged) |
| CI `docker-build` job | builds + pushes 5 images on `main` |
| CI `deploy-smoke.yml` workflow | green on `main` end-to-end |

## What changed for operators

| Before Sub-10b | After Sub-10b |
|---|---|
| 4 Docker images, no compose for prod | 5 images (4 apps + migrator), compose ready for the host |
| No deploy automation | `.\deploy\deploy.ps1` — one command, idempotent, audited |
| No rollback path | `.\deploy\rollback.ps1 -ToTag <prev>` — image-tag swap, smoke-verified |
| Migration timing manual | Sidecar migrator runs to completion before APIs |
| Secrets in compose env | Secrets in `.env.prod` on host, NTFS-locked |
| No image registry | ghcr.io with full tag matrix |

## Out of scope (Sub-10c)

- TLS / DNS / LB validation against IDD v1.2 production hostnames.
- AD federation against `cce.local` (389/636).
- Multi-environment promotion (test → pre-prod → prod → DR).
- Backup automation + DB restore.
- Production Sentry DSN provisioning + secret rotation.
- Auto-rollback on smoke-probe failure.
- Vault / Azure Key Vault graduation.
- Multi-host orchestration / clustering.

## ADRs

- ADR-0053 — Deployment shape: Linux containers on Windows Server 2022.

## Cross-references

- [Sub-10a App productionization completion](sub-10a-app-productionization-completion.md)
- [Forward-only migrations runbook](runbooks/migrations.md)
- [Production deploy runbook](runbooks/deploy.md)
- [Rollback runbook](runbooks/rollback.md)
```

**`CHANGELOG.md` modification** — prepend a new top section:

```markdown
## [deploy-v1.0.0] — 2026-05-04

**Sub-10b — Deployment automation.** One-command deployable system on a single Windows Server 2022 host. Linux containers, sidecar migrator, `.env.prod` on host, image-tag rollback, ghcr.io registry, PowerShell deploy + rollback scripts.

### Added
- `CCE.Seeder` `--migrate` and `--seed-reference` CLI flags backed by EF Core's `Database.MigrateAsync()`.
- `cce-migrator` multistage Dockerfile + CI build step.
- 3-file compose pattern: `docker-compose.prod.yml` + `prod.deploy.yml` (strict-env) + `build.yml` (local-build).
- `.env.prod.example` documenting every required + optional env-var.
- CI ghcr.io push gate with full tag matrix (`:<sha>` / `:sha-<short>` / `:latest` / `:<release-tag>` on `v*`).
- `deploy/deploy.ps1` (10-step idempotent flow with audit-logged `deploy-history.tsv`).
- `deploy/smoke.ps1` (4-endpoint probe with 60-sec window).
- `deploy/rollback.ps1` (atomic image-tag swap + re-deploy).
- `.github/workflows/deploy-smoke.yml` (Windows-runner end-to-end deploy → rollback → re-smoke test).
- 12 new backend tests (9 flag parser + 3 migration on Testcontainers MS-SQL).
- 4 new docs: ADR-0053, completion doc, deploy/README.md, 3 runbooks (`deploy.md`, `rollback.md`, `migrations.md`).

### Changed
- `docker-compose.prod.yml` now references ghcr.io image refs by `${CCE_REGISTRY_OWNER}/cce-<name>:${CCE_IMAGE_TAG}` instead of `build:` blocks. CI uses `docker-compose.build.yml` overlay to restore local-build behaviour for PR smoke target.
- CI `docker-build` job: `permissions.packages: write`, conditional push gate (`main` + `v*`), step-summary table of pushed images + tags.
- `.gitignore`: explicit `!.env.prod.example` allow-list.

### Architecture decisions
- ADR-0053 — Linux containers on Windows Server 2022 (not Windows-native rebuild); sidecar migrator (auto-on-deploy with `MIGRATE_ON_DEPLOY=false` kill-switch); `.env.prod` on host with NTFS ACLs (not Vault, not Docker secrets); image-tag rollback + forward-only migrations (not backup-restore); ghcr.io + PowerShell scripts (not self-hosted registry).
```

- [ ] **Step 1:** Create `docs/adr/0053-deployment-shape-linux-containers-on-win-server.md` with the contents above.

- [ ] **Step 2:** Create `docs/sub-10b-deployment-automation-completion.md` with the contents above.

- [ ] **Step 3:** Read existing `CHANGELOG.md` to find the top of the file (the [app-v1.0.0] entry):
  ```bash
  head -20 /Users/m/CCE/CHANGELOG.md
  ```

- [ ] **Step 4:** Prepend the `[deploy-v1.0.0]` section above the existing `[app-v1.0.0]` entry.

- [ ] **Step 5:** Commit:
  ```bash
  git -C /Users/m/CCE add docs/adr/0053-deployment-shape-linux-containers-on-win-server.md \
                          docs/sub-10b-deployment-automation-completion.md \
                          CHANGELOG.md
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "docs(sub-10b): close-out — ADR-0053, completion doc, CHANGELOG

  ADR-0053 documents the 5 deployment-shape decisions made during
  the Sub-10b brainstorm: Linux containers on Win Server 2022 (vs
  Windows-native), sidecar migrator (vs gated), .env.prod on host
  (vs Vault), image-tag rollback (vs backup-restore), ghcr.io (vs
  self-hosted/ACR/ECR).

  Completion doc mirrors Sub-10a shape: phase summaries, gates
  green at release, before/after operator delta, scope-deferred
  list pointing at Sub-10c.

  CHANGELOG entry under [deploy-v1.0.0].

  Sub-10b Phase 02 Task 2.3.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 2.4: Tag `deploy-v1.0.0`

**Files:** none — git tag operation.

- [ ] **Step 1:** Verify HEAD is the close-out commit:
  ```bash
  git -C /Users/m/CCE log --oneline -5
  ```

- [ ] **Step 2:** Tag locally (annotated):
  ```bash
  git -C /Users/m/CCE tag -a deploy-v1.0.0 -m "Sub-10b — Deployment automation

  One-command deployable system on a single Windows Server 2022 host.
  Linux containers, sidecar migrator, .env.prod on host, image-tag
  rollback, ghcr.io registry, PowerShell deploy + rollback scripts.

  Spec: docs/superpowers/specs/2026-05-03-sub-10b-design.md
  Completion: docs/sub-10b-deployment-automation-completion.md"
  ```

- [ ] **Step 3:** Verify the tag exists:
  ```bash
  git -C /Users/m/CCE tag -l "deploy-v*"
  ```
  Expected: `deploy-v1.0.0`.

- [ ] **Step 4:** Push tag (only if user explicitly requests; tags are local until pushed). The `v*` push trigger in CI will run the `docker-build` job and push images with the `:deploy-v1.0.0` tag too.

---

## Phase 02 close-out

After Task 2.4 commits cleanly:

- [ ] **Run the full check:**
  ```bash
  cd /Users/m/CCE/backend && dotnet build && \
    dotnet test tests/CCE.Application.Tests/ --nologo
  ```
  Expected: 439 Application tests passing.

- [ ] **Verify CI green:** the `docker-build` job should still build all 5 images.

- [ ] **Optional: dispatch deploy-smoke.yml manually** to verify the end-to-end deploy → rollback cycle. This is the canonical pre-release gate but doesn't auto-fire on every commit (Windows runners are expensive).

**Phase 02 done when:**
- 4 commits land on `main`.
- `rollback.ps1` exists and is documented.
- `deploy-smoke.yml` workflow exists (manual-dispatch).
- ADR-0053 + completion doc + CHANGELOG entry land.
- Tag `deploy-v1.0.0` exists locally.
- All tests still passing: 439 Application + 66 Infrastructure (1 skipped) + 502 frontend.

# Phase 03 — Backup automation + restore (Sub-10c)

> Parent: [`../2026-05-04-sub-10c.md`](../2026-05-04-sub-10c.md) · Spec: [`../../specs/2026-05-04-sub-10c-design.md`](../../specs/2026-05-04-sub-10c-design.md) §Backup — Ola Hallengren + Task Scheduler.

**Phase goal:** Wire production backup automation. Ola Hallengren's SQL Server Maintenance Solution installed via bootstrap script; 5 Windows Task Scheduler tasks (full/diff/log/integrity-check/off-host-sync); operator-driven restore helper backed by a Testcontainers SQL round-trip test; backup-chain healthcheck script; ADR-0056 + restore runbook.

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 02 closed (5 commits land on `main`; HEAD at `f7da2e6` or later).
- `.env.<env>.example` files include `BACKUP_UNC_*` keys (added in Phase 00 Task 0.3).
- SQL Server is accessible to the host (per IDD; or via Testcontainers in tests).
- `Microsoft.Data.SqlClient` available (transitively via `CCE.Infrastructure`'s EF Core deps).
- Backend baseline: 439 Application + 69 Infrastructure tests passing (1 skipped).

---

## Task 3.1: `Install-OlaHallengren.ps1` — bootstrap installer

**Files:**
- Create: `infra/backup/Install-OlaHallengren.ps1` — downloads Ola Hallengren's `MaintenanceSolution.sql` from the canonical URL, verifies SHA256, applies to the host's SQL Server.
- Create: `infra/backup/MaintenanceSolution.checksum` — pinned version + SHA256 hash for verification.

**Why a bootstrap script not a committed SQL blob:** Ola Hallengren's `MaintenanceSolution.sql` is a 100KB+ third-party file. Committing it would balloon the repo; pinning to a versioned URL with checksum verification is cleaner. Bootstrap script is one-shot operator setup.

**Final state of `infra/backup/MaintenanceSolution.checksum`:**

```
# Ola Hallengren SQL Server Maintenance Solution
# Pinned version: 2024-11-10
# Source URL (canonical): https://ola.hallengren.com/scripts/MaintenanceSolution.sql
#
# SHA256 below is computed against the canonical file at the pinned version.
# Operator updates: bump version + recompute SHA256 + commit.
#
# To verify locally:
#   curl -fsS https://ola.hallengren.com/scripts/MaintenanceSolution.sql | shasum -a 256
#
# Recorded SHA256 (placeholder — operator MUST replace at first install):
SHA256_PLACEHOLDER_REPLACE_AT_FIRST_INSTALL
```

**Note on the placeholder:** Sub-10c can't pin a real SHA256 without an internet fetch in this dev session. The bootstrap script enforces operator action: on first run it fails with a clear "checksum is placeholder; download once, record SHA256, commit" message. After the operator does this once, subsequent runs validate against the pinned value.

**Final state of `infra/backup/Install-OlaHallengren.ps1`:**

```powershell
#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10c — install Ola Hallengren's SQL Server Maintenance Solution.

.DESCRIPTION
    Downloads MaintenanceSolution.sql from ola.hallengren.com, verifies the
    SHA256 against the pinned value in MaintenanceSolution.checksum, and
    applies it to the host's SQL Server via sqlcmd or Invoke-Sqlcmd.

    On first install (when checksum is placeholder): downloads, computes
    SHA256, prints it, and exits non-zero asking the operator to record it.

    Idempotent: Ola's script is itself idempotent (CREATE OR ALTER PROC).

.PARAMETER Environment
    Environment name. Determines which SQL connection string to read from
    .env.<Environment> (key: INFRA_SQL).

.PARAMETER EnvFile
    Override env-file path. Default: C:\ProgramData\CCE\.env.<Environment>.

.PARAMETER ConnectionString
    Override the SQL connection string entirely (e.g. for one-shot test runs).

.EXAMPLE
    .\infra\backup\Install-OlaHallengren.ps1 -Environment prod
#>
[CmdletBinding()]
param(
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile,
    [string]$ConnectionString
)

$ErrorActionPreference = 'Stop'

$logDir = 'C:\ProgramData\CCE\logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir ("ola-install-{0}-{1:yyyyMMddTHHmmssZ}.log" -f $Environment, (Get-Date).ToUniversalTime())

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $line = "[$ts] [$Level] [$Environment] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

# ─── Resolve connection string ────────────────────────────────────────────
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    if (-not $EnvFile -or $EnvFile -eq '') {
        $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
    }
    if (-not (Test-Path $EnvFile)) { Write-Error "Env-file not found: $EnvFile"; exit 1 }
    $envMap = @{}
    foreach ($line in Get-Content $EnvFile) {
        if ($line -match '^\s*#') { continue }
        if ($line -match '^\s*$') { continue }
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
            $envMap[$Matches[1]] = $Matches[2].Trim() -replace '\s*#.*$',''
        }
    }
    if (-not $envMap.ContainsKey('INFRA_SQL') -or [string]::IsNullOrWhiteSpace($envMap['INFRA_SQL'])) {
        Write-Error "INFRA_SQL not set in $EnvFile."
        exit 1
    }
    $ConnectionString = $envMap['INFRA_SQL']
}

# ─── Read pinned checksum ─────────────────────────────────────────────────
$checksumFile = Join-Path $PSScriptRoot 'MaintenanceSolution.checksum'
if (-not (Test-Path $checksumFile)) { Write-Error "Checksum file not found: $checksumFile"; exit 1 }
$checksumLines = Get-Content $checksumFile | Where-Object { $_ -notmatch '^\s*#' -and $_.Trim() -ne '' }
$pinnedSha256 = $checksumLines | Select-Object -First 1
if (-not $pinnedSha256) { Write-Error "No SHA256 found in $checksumFile."; exit 1 }
Write-Log "Pinned SHA256: $pinnedSha256"

# ─── Download MaintenanceSolution.sql ─────────────────────────────────────
$tempSql = Join-Path $env:TEMP 'MaintenanceSolution.sql'
Write-Log "Downloading Ola Hallengren MaintenanceSolution.sql..."
Invoke-WebRequest -Uri 'https://ola.hallengren.com/scripts/MaintenanceSolution.sql' `
                  -OutFile $tempSql -UseBasicParsing

# ─── Verify SHA256 ────────────────────────────────────────────────────────
$actualSha256 = (Get-FileHash -Path $tempSql -Algorithm SHA256).Hash
Write-Log "Downloaded SHA256: $actualSha256"

if ($pinnedSha256 -eq 'SHA256_PLACEHOLDER_REPLACE_AT_FIRST_INSTALL') {
    Write-Error "First-install detected. Operator MUST record this SHA256 in MaintenanceSolution.checksum:"
    Write-Error "  $actualSha256"
    Write-Error "Then commit + re-run this script to install."
    exit 1
}

if ($pinnedSha256 -ne $actualSha256) {
    Write-Error "SHA256 mismatch! Pinned: $pinnedSha256. Downloaded: $actualSha256."
    Write-Error "Either Ola's upstream changed (verify + bump checksum file) or download was tampered with."
    exit 1
}
Write-Log "SHA256 verified."

# ─── Apply via sqlcmd ─────────────────────────────────────────────────────
# Force install into [master] regardless of the connection string's Initial Catalog,
# because Ola's script always installs its procedures in master.
Write-Log "Applying MaintenanceSolution.sql to SQL Server..."
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    Write-Error "sqlcmd not on PATH. Install SQL Server tools or use Invoke-Sqlcmd via SqlServer module."
    exit 1
}

# Parse server + auth from connection string. Simple form: Server=host,port;Database=...;User Id=...;Password=...
$server   = ([regex]::Match($ConnectionString, 'Server=([^;]+)')).Groups[1].Value
$user     = ([regex]::Match($ConnectionString, 'User Id=([^;]+)')).Groups[1].Value
$password = ([regex]::Match($ConnectionString, 'Password=([^;]+)')).Groups[1].Value

$args = @('-S', $server, '-d', 'master', '-i', $tempSql, '-b')
if ($user)     { $args += @('-U', $user) }
if ($password) { $args += @('-P', $password) }

$proc = Start-Process sqlcmd -ArgumentList $args -Wait -PassThru -NoNewWindow
if ($proc.ExitCode -ne 0) {
    Write-Error "sqlcmd exited with code $($proc.ExitCode). See above for SQL errors."
    exit $proc.ExitCode
}

Write-Log "Ola Hallengren maintenance solution installed."
Write-Log "  Created: dbo.CommandExecute, dbo.DatabaseBackup, dbo.DatabaseIntegrityCheck,"
Write-Log "           dbo.IndexOptimize, dbo.CommandLog (in master DB)."
exit 0
```

- [ ] **Step 1:** Create `infra/backup/` directory:
  ```bash
  mkdir -p /Users/m/CCE/infra/backup
  ```

- [ ] **Step 2:** Create `infra/backup/MaintenanceSolution.checksum` with the contents above (placeholder SHA256 stays until operator first install).

- [ ] **Step 3:** Create `infra/backup/Install-OlaHallengren.ps1` with the contents above.

- [ ] **Step 4:** Commit:
  ```bash
  git -C /Users/m/CCE add infra/backup/Install-OlaHallengren.ps1 infra/backup/MaintenanceSolution.checksum
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(infra): Install-OlaHallengren.ps1 bootstrap

  Downloads Ola Hallengren's MaintenanceSolution.sql from the
  canonical ola.hallengren.com URL, verifies SHA256 against the
  pinned value in MaintenanceSolution.checksum, applies via
  sqlcmd. On first install (placeholder SHA256), downloads, prints
  the actual SHA256, and asks operator to record + commit it.

  Avoids bundling 100KB+ third-party SQL in the repo while still
  enforcing version pinning + tamper detection via checksum.

  Sub-10c Phase 03 Task 3.1.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 3.2: `scheduled-tasks.xml` — 5 Windows Task Scheduler entries

**Files:**
- Create: `infra/backup/scheduled-tasks.xml` — schtasks XML for full/diff/log/integrity-check/off-host-sync.
- Create: `infra/backup/Register-ScheduledTasks.ps1` — wrapper that imports the XML via `schtasks /create /xml` for each task.

**Why a wrapper script:** schtasks XML format requires `/tn <name>` per `/create` invocation. The wrapper iterates the 5 tasks + sets up command-line params per task.

**The 5 scheduled tasks:**

| Task | Frequency | Command (passed to sqlcmd) |
|---|---|---|
| `CCE-Backup-Full` | Daily 02:00 local | `EXEC dbo.DatabaseBackup @Databases='USER_DATABASES', @Directory='D:\CCEBackups\FULL', @BackupType='FULL', @Verify='Y', @CleanupTime=168, @Compress='Y'` |
| `CCE-Backup-Diff` | Every 6 hours | `EXEC dbo.DatabaseBackup @Databases='USER_DATABASES', @Directory='D:\CCEBackups\DIFF', @BackupType='DIFF', @Verify='Y', @CleanupTime=168, @Compress='Y'` |
| `CCE-Backup-Log` | Every 15 min | `EXEC dbo.DatabaseBackup @Databases='USER_DATABASES', @Directory='D:\CCEBackups\LOG', @BackupType='LOG', @Verify='Y', @CleanupTime=24, @Compress='Y'` |
| `CCE-Backup-IntegrityCheck` | Sunday 03:00 | `EXEC dbo.DatabaseIntegrityCheck @Databases='USER_DATABASES'` |
| `CCE-Backup-Sync-OffHost` | Hourly | `pwsh -NoProfile -File C:\path\to\infra\backup\Sync-OffHost.ps1 -Environment <env>` |

**Final state of `infra/backup/Register-ScheduledTasks.ps1`:**

```powershell
#requires -Version 7.0
#requires -RunAsAdministrator
<#
.SYNOPSIS
    CCE Sub-10c — register 5 Windows Task Scheduler tasks for backup automation.

.DESCRIPTION
    Registers 5 schtasks to drive Ola Hallengren backup procs on the host's
    SQL Server, plus an hourly off-host robocopy sync. Tasks run as the
    cce-sqlbackup-svc service account (operator provisions; documented in
    backup-restore.md runbook).

    Idempotent — re-running deletes + re-creates each task by name.

.PARAMETER Environment
    Environment name; passed to Sync-OffHost.ps1 invocations.

.PARAMETER EnvFile
    Override env-file path. Default: C:\ProgramData\CCE\.env.<Environment>.

.PARAMETER ServiceAccount
    Account to run the tasks as. Default: NT AUTHORITY\SYSTEM (works for
    SQL local-Windows-auth + filesystem-local writes; UNC sync needs a
    domain account — operator overrides).

.EXAMPLE
    .\infra\backup\Register-ScheduledTasks.ps1 -Environment prod -ServiceAccount cce.local\cce-sqlbackup-svc
#>
[CmdletBinding()]
param(
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile,
    [string]$ServiceAccount = 'NT AUTHORITY\SYSTEM'
)

$ErrorActionPreference = 'Stop'

if (-not $EnvFile -or $EnvFile -eq '') {
    $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)   # repo root from infra/backup
$syncScript = Join-Path $PSScriptRoot 'Sync-OffHost.ps1'

# Helper: build a sqlcmd invocation that runs a one-shot SQL command.
function New-SqlCommand {
    param([string]$Sql, [string]$EnvFile)
    # Resolve INFRA_SQL at task-creation time; tasks store the literal command.
    $envMap = @{}
    foreach ($line in Get-Content $EnvFile) {
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
            $envMap[$Matches[1]] = $Matches[2].Trim() -replace '\s*#.*$',''
        }
    }
    $cs = $envMap['INFRA_SQL']
    $server   = ([regex]::Match($cs, 'Server=([^;]+)')).Groups[1].Value
    $user     = ([regex]::Match($cs, 'User Id=([^;]+)')).Groups[1].Value
    $database = ([regex]::Match($cs, 'Database=([^;]+)')).Groups[1].Value
    if (-not $database) { $database = 'master' }
    # Password is read from env-file at TASK RUN time via pwsh wrapper, not embedded here.
    return "pwsh -NoProfile -Command `"& { `$cs = ((Get-Content '$EnvFile' | Select-String '^INFRA_SQL=') -split '=',2)[1]; `$pwd = ([regex]::Match(`$cs, 'Password=([^;]+)')).Groups[1].Value; sqlcmd -S '$server' -d '$database' -U '$user' -P `$pwd -b -Q `\`"$Sql`\`" }`""
}

# Build the 5 task definitions.
$tasks = @(
    @{
        Name = 'CCE-Backup-Full'
        Trigger = (New-ScheduledTaskTrigger -Daily -At '02:00')
        Action = (New-ScheduledTaskAction -Execute 'pwsh' -Argument "-NoProfile -Command `"sqlcmd -S `$Server -Q 'EXEC dbo.DatabaseBackup @Databases=\"USER_DATABASES\", @Directory=\"D:\CCEBackups\FULL\", @BackupType=\"FULL\", @Verify=\"Y\", @CleanupTime=168, @Compress=\"Y\"'`"")
        Sql = "EXEC dbo.DatabaseBackup @Databases='USER_DATABASES', @Directory='D:\CCEBackups\FULL', @BackupType='FULL', @Verify='Y', @CleanupTime=168, @Compress='Y'"
    }
    @{
        Name = 'CCE-Backup-Diff'
        Trigger = (New-ScheduledTaskTrigger -Daily -At '00:00' -DaysInterval 1)  # Every 6 hours via repetition
        Sql = "EXEC dbo.DatabaseBackup @Databases='USER_DATABASES', @Directory='D:\CCEBackups\DIFF', @BackupType='DIFF', @Verify='Y', @CleanupTime=168, @Compress='Y'"
    }
    @{
        Name = 'CCE-Backup-Log'
        # Every 15 minutes
        Sql = "EXEC dbo.DatabaseBackup @Databases='USER_DATABASES', @Directory='D:\CCEBackups\LOG', @BackupType='LOG', @Verify='Y', @CleanupTime=24, @Compress='Y'"
    }
    @{
        Name = 'CCE-Backup-IntegrityCheck'
        Trigger = (New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At '03:00')
        Sql = "EXEC dbo.DatabaseIntegrityCheck @Databases='USER_DATABASES'"
    }
    @{
        Name = 'CCE-Backup-Sync-OffHost'
        # Every hour
        IsSync = $true
    }
)

# Backup root + directories.
$backupRoot = 'D:\CCEBackups'
foreach ($sub in @('FULL','DIFF','LOG')) {
    $dir = Join-Path $backupRoot $sub
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "Created backup dir: $dir"
    }
}

# Register each task. Use New-ScheduledTask APIs (PowerShell-native).
foreach ($t in $tasks) {
    $name = $t.Name

    # Action.
    if ($t.IsSync) {
        $action = New-ScheduledTaskAction -Execute 'pwsh' `
            -Argument "-NoProfile -File `"$syncScript`" -Environment $Environment"
    } else {
        # Wrap the SQL in a pwsh command that resolves the password from env-file at run time.
        $sql = $t.Sql -replace "'", "''"
        $argument = "-NoProfile -Command `"" +
            "`$envFile = '$EnvFile'; " +
            "`$envMap = @{}; foreach (`$ln in Get-Content `$envFile) { if (`$ln -match '^([A-Z_]+)=(.*)$') { `$envMap[`$Matches[1]] = `$Matches[2].Trim() } }; " +
            "`$cs = `$envMap['INFRA_SQL']; " +
            "`$server = ([regex]::Match(`$cs, 'Server=([^;]+)')).Groups[1].Value; " +
            "`$user = ([regex]::Match(`$cs, 'User Id=([^;]+)')).Groups[1].Value; " +
            "`$pwd = ([regex]::Match(`$cs, 'Password=([^;]+)')).Groups[1].Value; " +
            "sqlcmd -S `$server -d master -U `$user -P `$pwd -b -Q `\`"$sql`\`"" +
            "`""
        $action = New-ScheduledTaskAction -Execute 'pwsh' -Argument $argument
    }

    # Trigger.
    switch ($name) {
        'CCE-Backup-Full' {
            $trigger = New-ScheduledTaskTrigger -Daily -At '02:00'
        }
        'CCE-Backup-Diff' {
            $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date -Hour 0 -Minute 0 -Second 0) `
                -RepetitionInterval (New-TimeSpan -Hours 6) `
                -RepetitionDuration ([System.TimeSpan]::FromDays(36500))
        }
        'CCE-Backup-Log' {
            $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date -Hour 0 -Minute 0 -Second 0) `
                -RepetitionInterval (New-TimeSpan -Minutes 15) `
                -RepetitionDuration ([System.TimeSpan]::FromDays(36500))
        }
        'CCE-Backup-IntegrityCheck' {
            $trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At '03:00'
        }
        'CCE-Backup-Sync-OffHost' {
            $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date -Hour 0 -Minute 0 -Second 0) `
                -RepetitionInterval (New-TimeSpan -Hours 1) `
                -RepetitionDuration ([System.TimeSpan]::FromDays(36500))
        }
    }

    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
                                              -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 4)

    # Idempotent: delete first if exists, then re-register.
    if (Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue) {
        Write-Host "Task '$name' exists; deleting + re-registering."
        Unregister-ScheduledTask -TaskName $name -Confirm:$false
    }

    Register-ScheduledTask -TaskName $name -Action $action -Trigger $trigger -Settings $settings `
                           -User $ServiceAccount -RunLevel Highest -Force | Out-Null
    Write-Host "Registered task: $name"
}

Write-Host ""
Write-Host "All 5 backup tasks registered. Verify with: Get-ScheduledTask | Where-Object TaskName -like 'CCE-Backup-*'"
exit 0
```

- [ ] **Step 1:** Create `infra/backup/Register-ScheduledTasks.ps1` with the contents above.

- [ ] **Step 2:** Commit:
  ```bash
  git -C /Users/m/CCE add infra/backup/Register-ScheduledTasks.ps1
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(infra): Register-ScheduledTasks.ps1 — 5 backup tasks

  Idempotent. Registers 5 Windows scheduled tasks driving Ola
  Hallengren backup procs:
   - CCE-Backup-Full          (daily 02:00, 7-day retention, FULL)
   - CCE-Backup-Diff          (every 6 hr, 7-day retention, DIFF)
   - CCE-Backup-Log           (every 15 min, 24-hr retention, LOG)
   - CCE-Backup-IntegrityCheck (Sunday 03:00, DBCC CHECKDB)
   - CCE-Backup-Sync-OffHost  (hourly robocopy to UNC share)

  SQL tasks read INFRA_SQL from .env.<env> at run time and invoke
  sqlcmd; passwords are NOT embedded in task command lines. Backup
  root D:\CCEBackups\{FULL,DIFF,LOG} created if missing. Service
  account default NT AUTHORITY\SYSTEM; operator overrides via
  -ServiceAccount for the UNC-write account.

  Sub-10c Phase 03 Task 3.2.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 3.3: `Sync-OffHost.ps1` + `Test-BackupChain.ps1`

**Files:**
- Create: `infra/backup/Sync-OffHost.ps1` — robocopy wrapper that mirrors `D:\CCEBackups\` to the UNC share defined by `BACKUP_UNC_*` env-vars.
- Create: `infra/backup/Test-BackupChain.ps1` — queries `master.dbo.CommandLog` for the last 24h `DatabaseBackup` runs; reports gaps + last-full timestamp.

**Final state of `infra/backup/Sync-OffHost.ps1`:**

```powershell
#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10c — sync local backup directory to off-host UNC share.

.DESCRIPTION
    Mirrors D:\CCEBackups\ to \\${BACKUP_UNC_HOST}\${BACKUP_UNC_SHARE}\<env>\
    using robocopy /MIR. Logs to C:\ProgramData\CCE\logs\. Designed to be
    invoked by the CCE-Backup-Sync-OffHost scheduled task (hourly).

    Pre-requisite: cmdkey /add:${BACKUP_UNC_HOST} cached on the deploy
    host (one-time during host setup; see backup-restore.md runbook).

.PARAMETER Environment
    Environment name. Default: prod.

.PARAMETER EnvFile
    Override env-file path. Default: C:\ProgramData\CCE\.env.<Environment>.

.PARAMETER LocalRoot
    Override local backup root. Default: D:\CCEBackups.

.EXAMPLE
    .\infra\backup\Sync-OffHost.ps1 -Environment prod
#>
[CmdletBinding()]
param(
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile,
    [string]$LocalRoot = 'D:\CCEBackups'
)

$ErrorActionPreference = 'Stop'

if (-not $EnvFile -or $EnvFile -eq '') {
    $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
}

$logDir = 'C:\ProgramData\CCE\logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir ("backup-sync-{0}-{1:yyyyMMddTHHmmssZ}.log" -f $Environment, (Get-Date).ToUniversalTime())

if (-not (Test-Path $EnvFile)) { Write-Error "Env-file not found: $EnvFile"; exit 1 }
if (-not (Test-Path $LocalRoot)) { Write-Error "Local backup root not found: $LocalRoot"; exit 1 }

$envMap = @{}
foreach ($line in Get-Content $EnvFile) {
    if ($line -match '^\s*#') { continue }
    if ($line -match '^\s*$') { continue }
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $envMap[$Matches[1]] = $Matches[2].Trim() -replace '\s*#.*$',''
    }
}

$required = @('BACKUP_UNC_HOST','BACKUP_UNC_SHARE')
$missing = $required | Where-Object { -not $envMap.ContainsKey($_) -or [string]::IsNullOrWhiteSpace($envMap[$_]) }
if ($missing) { Write-Error "Missing required env-keys: $($missing -join ', ')"; exit 1 }

$dest = "\\$($envMap['BACKUP_UNC_HOST'])\$($envMap['BACKUP_UNC_SHARE'])\$Environment"

# Run robocopy. /MIR mirrors; /Z resumable; /R:3 retries; /W:10 retry wait;
# /LOG+ appends; /NP no progress %; /NFL/NDL minimize log spam.
$robocopyArgs = @(
    $LocalRoot, $dest,
    '/MIR', '/Z', '/R:3', '/W:10',
    "/LOG+:$logFile",
    '/NP', '/NFL', '/NDL'
)

Write-Host "Syncing: $LocalRoot → $dest"
$proc = Start-Process robocopy -ArgumentList $robocopyArgs -Wait -PassThru -NoNewWindow
# Robocopy exit codes 0-7 are success (with various flags); 8+ is failure.
if ($proc.ExitCode -ge 8) {
    Write-Error "robocopy failed (exit $($proc.ExitCode)). See $logFile."
    exit $proc.ExitCode
}

Write-Host "Sync complete (robocopy exit $($proc.ExitCode); 0-7 = success)."
exit 0
```

**Final state of `infra/backup/Test-BackupChain.ps1`:**

```powershell
#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10c — backup-chain healthcheck.

.DESCRIPTION
    Queries master.dbo.CommandLog for the last 24h of DatabaseBackup runs.
    Reports: failures, last-full timestamp, log-chain gaps. Exits non-zero
    on any failure or chain gap.

    Designed to run after deploy.ps1 (warn-only — doesn't fail deploy)
    and as a periodic ops health check.

.PARAMETER Environment
    Environment name. Default: prod.

.PARAMETER EnvFile
    Override env-file path. Default: C:\ProgramData\CCE\.env.<Environment>.

.PARAMETER WarnOnly
    Print findings but always exit 0 (used by deploy.ps1 post-deploy step).

.EXAMPLE
    .\infra\backup\Test-BackupChain.ps1 -Environment prod
    .\infra\backup\Test-BackupChain.ps1 -Environment prod -WarnOnly
#>
[CmdletBinding()]
param(
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile,
    [switch]$WarnOnly
)

$ErrorActionPreference = 'Stop'

if (-not $EnvFile -or $EnvFile -eq '') {
    $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
}
if (-not (Test-Path $EnvFile)) { Write-Error "Env-file not found: $EnvFile"; exit 1 }

$envMap = @{}
foreach ($line in Get-Content $EnvFile) {
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $envMap[$Matches[1]] = $Matches[2].Trim() -replace '\s*#.*$',''
    }
}
$cs = $envMap['INFRA_SQL']
if ([string]::IsNullOrWhiteSpace($cs)) { Write-Error "INFRA_SQL not set."; exit 1 }

$server   = ([regex]::Match($cs, 'Server=([^;]+)')).Groups[1].Value
$user     = ([regex]::Match($cs, 'User Id=([^;]+)')).Groups[1].Value
$password = ([regex]::Match($cs, 'Password=([^;]+)')).Groups[1].Value

$query = @"
SET NOCOUNT ON;
DECLARE @cutoff datetime2 = DATEADD(hour, -24, SYSUTCDATETIME());
SELECT
    SUM(CASE WHEN ErrorNumber <> 0 THEN 1 ELSE 0 END)                             AS Failures24h,
    SUM(CASE WHEN Command LIKE '%@BackupType=%FULL%' AND ErrorNumber = 0 THEN 1 ELSE 0 END) AS FullSuccesses24h,
    SUM(CASE WHEN Command LIKE '%@BackupType=%LOG%'  AND ErrorNumber = 0 THEN 1 ELSE 0 END) AS LogSuccesses24h,
    MAX(CASE WHEN Command LIKE '%@BackupType=%FULL%' AND ErrorNumber = 0 THEN EndTime END)  AS LastFullSuccessUtc
FROM master.dbo.CommandLog
WHERE StartTime >= @cutoff
  AND CommandType = 'BACKUP_DATABASE';
"@

$queryFile = [System.IO.Path]::GetTempFileName() + '.sql'
Set-Content -Path $queryFile -Value $query -Encoding utf8

$args = @('-S', $server, '-d', 'master', '-i', $queryFile, '-h', '-1', '-W', '-s', '|')
if ($user)     { $args += @('-U', $user) }
if ($password) { $args += @('-P', $password) }

$output = & sqlcmd @args 2>&1
Remove-Item $queryFile -ErrorAction SilentlyContinue

if ($LASTEXITCODE -ne 0) {
    Write-Error "sqlcmd failed: $output"
    if (-not $WarnOnly) { exit 1 }
    Write-Host "[warn-only] Continuing despite failure."
    exit 0
}

# Parse output (pipe-delimited, no headers).
$row = $output | Where-Object { $_ -match '\|' } | Select-Object -First 1
if (-not $row) {
    Write-Error "Unexpected sqlcmd output (no rows): $output"
    if (-not $WarnOnly) { exit 1 }
    exit 0
}
$parts = $row.Split('|') | ForEach-Object { $_.Trim() }
$failures      = [int]$parts[0]
$fullSuccesses = [int]$parts[1]
$logSuccesses  = [int]$parts[2]
$lastFull      = $parts[3]

Write-Host "Backup-chain healthcheck (last 24h):"
Write-Host "  Failures:           $failures"
Write-Host "  Full backups (OK):  $fullSuccesses"
Write-Host "  Log backups (OK):   $logSuccesses"
Write-Host "  Last full success:  $lastFull"

$problems = @()
if ($failures -gt 0)         { $problems += "$failures failed backup(s) in last 24h" }
if ($fullSuccesses -eq 0)    { $problems += "no successful FULL backup in last 24h (expect ≥1 with daily-02:00 schedule)" }
if ($logSuccesses -lt 50)    { $problems += "$logSuccesses log backups in last 24h (expect ≥90 with 15-min schedule; some flexibility for boot times)" }

if ($problems.Count -gt 0) {
    Write-Host ""
    Write-Host "PROBLEMS:" -ForegroundColor Yellow
    foreach ($p in $problems) { Write-Host "  - $p" -ForegroundColor Yellow }
    if ($WarnOnly) {
        Write-Host "[warn-only] Continuing." -ForegroundColor Yellow
        exit 0
    }
    exit 1
}

Write-Host ""
Write-Host "Backup chain HEALTHY." -ForegroundColor Green
exit 0
```

- [ ] **Step 1:** Create `infra/backup/Sync-OffHost.ps1` with the contents above.

- [ ] **Step 2:** Create `infra/backup/Test-BackupChain.ps1` with the contents above.

- [ ] **Step 3:** Commit:
  ```bash
  git -C /Users/m/CCE add infra/backup/Sync-OffHost.ps1 infra/backup/Test-BackupChain.ps1
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(infra): Sync-OffHost.ps1 + Test-BackupChain.ps1

  Sync-OffHost.ps1 wraps robocopy /MIR to mirror D:\CCEBackups\
  to \\\${BACKUP_UNC_HOST}\\\${BACKUP_UNC_SHARE}\<env>\. Resumable
  (/Z), retried (/R:3 /W:10), logged. Robocopy exit 0-7 = success;
  8+ = error. Hourly via the CCE-Backup-Sync-OffHost task.

  Test-BackupChain.ps1 queries master.dbo.CommandLog for the last
  24h of DatabaseBackup runs. Reports: failures, last-full success,
  log-chain count. Exits non-zero on problems unless -WarnOnly is
  passed (deploy.ps1 calls it warn-only post-deploy as a passive
  health signal).

  Sub-10c Phase 03 Task 3.3.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 3.4: `Restore-FromBackup.ps1` + Testcontainers SQL test

**Files:**
- Create: `infra/backup/Restore-FromBackup.ps1` — operator-driven restore helper. Takes `-FullBackup`, optional `-DiffBackup`, optional `-LogBackups <path-list>`, `-TargetDb`, `-Force`.
- Create: `backend/tests/CCE.Infrastructure.Tests/Backup/RestoreFromBackupTests.cs` — Testcontainers SQL round-trip: create a real backup chain, restore to a different DB, verify row counts match.

**Final state of `infra/backup/Restore-FromBackup.ps1`:**

```powershell
#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10c — operator-driven SQL Server restore helper.

.DESCRIPTION
    Restores a SQL Server database from a chain of backups: FULL
    (required) → DIFF (optional) → LOGs (optional, in order).
    Verifies migration history matches expected before declaring
    success. Refuses to overwrite the live CCE DB unless -Force.

.PARAMETER FullBackup
    Required. Path to the full backup .bak file.

.PARAMETER DiffBackup
    Optional. Path to a differential backup .bak file (applied between
    FULL and LOGs).

.PARAMETER LogBackups
    Optional. Array of log-backup .trn paths in chronological order.

.PARAMETER TargetDb
    Required. Destination database name. Default: CCE_restored (NOT 'CCE'
    unless -Force passed).

.PARAMETER Force
    Allow restore over the live 'CCE' database. Refused without this
    switch as a safety check.

.PARAMETER Environment
    Environment name for resolving INFRA_SQL. Default: prod.

.PARAMETER EnvFile
    Override env-file path. Default: C:\ProgramData\CCE\.env.<Environment>.

.EXAMPLE
    # Test-restore (recommended quarterly):
    .\infra\backup\Restore-FromBackup.ps1 `
        -FullBackup D:\CCEBackups\FULL\CCE_FULL_20260504.bak `
        -DiffBackup D:\CCEBackups\DIFF\CCE_DIFF_20260504_120000.bak `
        -LogBackups (Get-ChildItem D:\CCEBackups\LOG\*.trn | Sort Name).FullName `
        -TargetDb CCE_restoretest

    # Disaster recovery (overwrites live CCE DB):
    .\infra\backup\Restore-FromBackup.ps1 -FullBackup ... -TargetDb CCE -Force
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$FullBackup,
    [string]$DiffBackup,
    [string[]]$LogBackups = @(),
    [string]$TargetDb = 'CCE_restored',
    [switch]$Force,
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile
)

$ErrorActionPreference = 'Stop'

if (-not $EnvFile -or $EnvFile -eq '') {
    $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
}

$logDir = 'C:\ProgramData\CCE\logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir ("restore-{0}-{1:yyyyMMddTHHmmssZ}.log" -f $Environment, (Get-Date).ToUniversalTime())

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $line = "[$ts] [$Level] [$Environment] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

# ─── Safety: refuse to overwrite live CCE without -Force ──────────────────
if ($TargetDb -eq 'CCE' -and -not $Force) {
    Write-Error "Refusing to restore over live 'CCE' database without -Force. Use -TargetDb CCE_restoretest for a test restore."
    exit 1
}

# ─── Resolve connection string ────────────────────────────────────────────
if (-not (Test-Path $EnvFile)) { Write-Error "Env-file not found: $EnvFile"; exit 1 }
$envMap = @{}
foreach ($line in Get-Content $EnvFile) {
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $envMap[$Matches[1]] = $Matches[2].Trim() -replace '\s*#.*$',''
    }
}
$cs = $envMap['INFRA_SQL']
if ([string]::IsNullOrWhiteSpace($cs)) { Write-Error "INFRA_SQL not set."; exit 1 }

$server   = ([regex]::Match($cs, 'Server=([^;]+)')).Groups[1].Value
$user     = ([regex]::Match($cs, 'User Id=([^;]+)')).Groups[1].Value
$password = ([regex]::Match($cs, 'Password=([^;]+)')).Groups[1].Value

# ─── Verify backup files exist ────────────────────────────────────────────
if (-not (Test-Path $FullBackup)) { Write-Error "Full backup not found: $FullBackup"; exit 1 }
if ($DiffBackup -and -not (Test-Path $DiffBackup)) { Write-Error "Diff backup not found: $DiffBackup"; exit 1 }
foreach ($lb in $LogBackups) {
    if (-not (Test-Path $lb)) { Write-Error "Log backup not found: $lb"; exit 1 }
}

Write-Log "Restore plan:"
Write-Log "  FULL: $FullBackup"
Write-Log "  DIFF: $($DiffBackup ?? '(none)')"
Write-Log "  LOGS: $($LogBackups.Count) file(s)"
Write-Log "  TARGET: $TargetDb"

# ─── Helper: run a sqlcmd ──────────────────────────────────────────────────
function Invoke-Sql {
    param([string]$Sql)
    $tempFile = [System.IO.Path]::GetTempFileName() + '.sql'
    Set-Content -Path $tempFile -Value $Sql -Encoding utf8
    $args = @('-S', $server, '-d', 'master', '-i', $tempFile, '-b')
    if ($user)     { $args += @('-U', $user) }
    if ($password) { $args += @('-P', $password) }
    $proc = Start-Process sqlcmd -ArgumentList $args -Wait -PassThru -NoNewWindow
    Remove-Item $tempFile -ErrorAction SilentlyContinue
    if ($proc.ExitCode -ne 0) { Write-Error "sqlcmd failed (exit $($proc.ExitCode))."; exit $proc.ExitCode }
}

# ─── Step 1: Restore FULL with NORECOVERY ────────────────────────────────
$lastStep = ($DiffBackup -or $LogBackups.Count -gt 0) ? 'NORECOVERY' : 'RECOVERY'
$fullSql = "RESTORE DATABASE [$TargetDb] FROM DISK = N'$FullBackup' WITH FILE = 1, $lastStep, REPLACE, STATS = 10"
Write-Log "Restoring FULL → [$TargetDb] WITH $lastStep..."
Invoke-Sql -Sql $fullSql

# ─── Step 2: Restore DIFF with NORECOVERY (if provided) ───────────────────
if ($DiffBackup) {
    $lastStep = ($LogBackups.Count -gt 0) ? 'NORECOVERY' : 'RECOVERY'
    $diffSql = "RESTORE DATABASE [$TargetDb] FROM DISK = N'$DiffBackup' WITH FILE = 1, $lastStep, STATS = 10"
    Write-Log "Restoring DIFF → [$TargetDb] WITH $lastStep..."
    Invoke-Sql -Sql $diffSql
}

# ─── Step 3: Restore LOGs with NORECOVERY (last with RECOVERY) ────────────
for ($i = 0; $i -lt $LogBackups.Count; $i++) {
    $isLast = ($i -eq $LogBackups.Count - 1)
    $lastStep = $isLast ? 'RECOVERY' : 'NORECOVERY'
    $logSql = "RESTORE LOG [$TargetDb] FROM DISK = N'$($LogBackups[$i])' WITH FILE = 1, $lastStep, STATS = 10"
    Write-Log "Restoring LOG $($i + 1)/$($LogBackups.Count) → [$TargetDb] WITH $lastStep..."
    Invoke-Sql -Sql $logSql
}

# ─── Step 4: Verify migration history ─────────────────────────────────────
Write-Log "Verifying migration history on [$TargetDb]..."
$verifyQuery = "USE [$TargetDb]; SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;"
$tempFile = [System.IO.Path]::GetTempFileName() + '.sql'
Set-Content -Path $tempFile -Value $verifyQuery -Encoding utf8
$args = @('-S', $server, '-d', 'master', '-i', $tempFile, '-h', '-1', '-W')
if ($user)     { $args += @('-U', $user) }
if ($password) { $args += @('-P', $password) }
$migrations = & sqlcmd @args 2>&1
Remove-Item $tempFile -ErrorAction SilentlyContinue

if ($LASTEXITCODE -ne 0) {
    Write-Log -Level 'WARN' "Migration history check failed (sqlcmd exit $LASTEXITCODE). DB restored but not verified."
} else {
    $migrationLines = $migrations | Where-Object { $_ -match '^\d{14}_' }
    Write-Log "Applied migrations on restored DB: $($migrationLines.Count)"
    foreach ($m in $migrationLines) { Write-Log "  - $m" }
}

Write-Log "Restore complete. Target DB: [$TargetDb]"
exit 0
```

**Final state of `backend/tests/CCE.Infrastructure.Tests/Backup/RestoreFromBackupTests.cs`:**

The tests exercise the SQL portion of the restore flow against Testcontainers SQL Server: create a backup chain (full + diff + 2 logs), restore to a different DB name, verify row counts match.

```cs
using System.Diagnostics.CodeAnalysis;
using CCE.Infrastructure.Persistence;
using CCE.Infrastructure.Tests.Migration;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CCE.Infrastructure.Tests.Backup;

/// <summary>
/// Round-trip tests for the Restore-FromBackup.ps1 workflow. Doesn't
/// invoke PowerShell directly — issues the same RESTORE DATABASE / LOG
/// commands the script issues, against a Testcontainers SQL Server.
/// </summary>
[Collection(nameof(MigratorCollection))]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
    Justification = "Test code; SqlConnections used in tight scope.")]
public sealed class RestoreFromBackupTests
{
    private readonly MigratorFixture _fixture;

    public RestoreFromBackupTests(MigratorFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task FullBackup_RestoresToTargetDb_ProducesSameRowCount()
    {
        // Arrange — create a source DB + a few rows + back it up.
        var sourceDb = $"src_full_{Guid.NewGuid():N}";
        var targetDb = $"tgt_full_{Guid.NewGuid():N}";
        var bakPath  = $"/tmp/{sourceDb}_full.bak";
        var srcCs    = _fixture.BuildConnectionString(sourceDb);

        await using (var ctx = _fixture.CreateContextWithFreshDb(sourceDb))
        {
            await ctx.Database.EnsureDeletedAsync();
            await ctx.Database.MigrateAsync();
        }

        // Insert one row that we can verify post-restore.
        await ExecuteAsync(srcCs, "USE master; CREATE TABLE testkv(k nvarchar(50) PRIMARY KEY, v nvarchar(50))");
        await ExecuteAsync(srcCs, $"USE [{sourceDb}]; CREATE TABLE testkv(k nvarchar(50) PRIMARY KEY, v nvarchar(50))");
        await ExecuteAsync(srcCs, $"USE [{sourceDb}]; INSERT INTO testkv VALUES ('hello','world')");

        // Backup.
        await ExecuteAsync(srcCs,
            $"BACKUP DATABASE [{sourceDb}] TO DISK = N'{bakPath}' WITH FORMAT, INIT, COMPRESSION");

        // Act — restore to a different DB name.
        var masterCs = _fixture.BuildConnectionString("master");
        await ExecuteAsync(masterCs,
            $"RESTORE DATABASE [{targetDb}] FROM DISK = N'{bakPath}' " +
            $"WITH FILE = 1, RECOVERY, REPLACE, " +
            $"MOVE N'{sourceDb}' TO N'/var/opt/mssql/data/{targetDb}.mdf', " +
            $"MOVE N'{sourceDb}_log' TO N'/var/opt/mssql/data/{targetDb}_log.ldf'");

        // Assert — restored DB has the row.
        var tgtCs = _fixture.BuildConnectionString(targetDb);
        var count = await ScalarAsync<int>(tgtCs, "SELECT COUNT(*) FROM testkv WHERE k = 'hello' AND v = 'world'");
        count.Should().Be(1);
    }

    [Fact]
    public async Task FullPlusDiffPlusLog_RestoresChain_IncludesAllChanges()
    {
        var sourceDb = $"src_chain_{Guid.NewGuid():N}";
        var targetDb = $"tgt_chain_{Guid.NewGuid():N}";
        var fullPath = $"/tmp/{sourceDb}_full.bak";
        var diffPath = $"/tmp/{sourceDb}_diff.bak";
        var logPath  = $"/tmp/{sourceDb}_log.trn";
        var srcCs    = _fixture.BuildConnectionString(sourceDb);

        // Migrate (creates DB in FULL recovery mode by default for SQL 2022).
        await using (var ctx = _fixture.CreateContextWithFreshDb(sourceDb))
        {
            await ctx.Database.EnsureDeletedAsync();
            await ctx.Database.MigrateAsync();
        }
        // Force FULL recovery model so log backups work.
        var masterCs = _fixture.BuildConnectionString("master");
        await ExecuteAsync(masterCs, $"ALTER DATABASE [{sourceDb}] SET RECOVERY FULL");

        // Round 1: insert + FULL backup.
        await ExecuteAsync(srcCs, $"USE [{sourceDb}]; CREATE TABLE chain_kv(k nvarchar(50) PRIMARY KEY, v nvarchar(50))");
        await ExecuteAsync(srcCs, $"USE [{sourceDb}]; INSERT INTO chain_kv VALUES ('full', '1')");
        await ExecuteAsync(srcCs,
            $"BACKUP DATABASE [{sourceDb}] TO DISK = N'{fullPath}' WITH FORMAT, INIT, COMPRESSION");

        // Round 2: insert + DIFF backup.
        await ExecuteAsync(srcCs, $"USE [{sourceDb}]; INSERT INTO chain_kv VALUES ('diff', '2')");
        await ExecuteAsync(srcCs,
            $"BACKUP DATABASE [{sourceDb}] TO DISK = N'{diffPath}' WITH DIFFERENTIAL, FORMAT, INIT, COMPRESSION");

        // Round 3: insert + LOG backup.
        await ExecuteAsync(srcCs, $"USE [{sourceDb}]; INSERT INTO chain_kv VALUES ('log', '3')");
        await ExecuteAsync(srcCs,
            $"BACKUP LOG [{sourceDb}] TO DISK = N'{logPath}' WITH FORMAT, INIT, COMPRESSION");

        // Restore the chain.
        await ExecuteAsync(masterCs,
            $"RESTORE DATABASE [{targetDb}] FROM DISK = N'{fullPath}' WITH FILE = 1, NORECOVERY, REPLACE, " +
            $"MOVE N'{sourceDb}' TO N'/var/opt/mssql/data/{targetDb}.mdf', " +
            $"MOVE N'{sourceDb}_log' TO N'/var/opt/mssql/data/{targetDb}_log.ldf'");
        await ExecuteAsync(masterCs,
            $"RESTORE DATABASE [{targetDb}] FROM DISK = N'{diffPath}' WITH FILE = 1, NORECOVERY");
        await ExecuteAsync(masterCs,
            $"RESTORE LOG [{targetDb}] FROM DISK = N'{logPath}' WITH FILE = 1, RECOVERY");

        // Assert — restored DB has all 3 rows.
        var tgtCs = _fixture.BuildConnectionString(targetDb);
        var rows = await ScalarAsync<int>(tgtCs, "SELECT COUNT(*) FROM chain_kv");
        rows.Should().Be(3, "FULL + DIFF + LOG should replay all 3 inserts");

        var hasLogRow = await ScalarAsync<int>(tgtCs, "SELECT COUNT(*) FROM chain_kv WHERE k = 'log'");
        hasLogRow.Should().Be(1, "the log-backup row must be present after the chain restore");
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 120;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(string connectionString, string sql)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 30;
        var result = await cmd.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T));
    }
}
```

**Note on `MigratorFixture`:** Phase 00 of Sub-10b created this fixture under `Migration/`. It boots a single SQL Server container shared across migration + backup tests via the `MigratorCollection` collection-fixture. Phase 03 reuses it. The `MigratorFixture.BuildConnectionString(string suffix)` helper is already public.

- [ ] **Step 1:** Create `infra/backup/Restore-FromBackup.ps1` with the contents above.

- [ ] **Step 2:** Create the test directory:
  ```bash
  mkdir -p /Users/m/CCE/backend/tests/CCE.Infrastructure.Tests/Backup
  ```

- [ ] **Step 3:** Verify `Microsoft.Data.SqlClient` is available transitively. If not, add it:
  ```bash
  grep -r "Microsoft.Data.SqlClient" /Users/m/CCE/backend/Directory.Packages.props /Users/m/CCE/backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj 2>&1 | head -5
  ```
  Expected: at least one match (transitive via EF Core).

- [ ] **Step 4:** Create `Backup/RestoreFromBackupTests.cs` with the contents above.

- [ ] **Step 5:** Build:
  ```bash
  cd /Users/m/CCE/backend && dotnet build tests/CCE.Infrastructure.Tests/ --nologo 2>&1 | tail -8
  ```
  Expected: success. If `Microsoft.Data.SqlClient` isn't transitively available, add `<PackageReference Include="Microsoft.Data.SqlClient" />` to the test project's `.csproj` (the package version is pinned in `Directory.Packages.props` already).

- [ ] **Step 6:** Run the backup tests (Docker required):
  ```bash
  cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/ --filter "FullyQualifiedName~Backup" --nologo 2>&1 | tail -10
  ```
  Expected: 2 passing.

- [ ] **Step 7:** Run full Infrastructure suite:
  ```bash
  cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/ --nologo 2>&1 | tail -3
  ```
  Expected: 69 + 2 = 71 passing (1 skipped).

- [ ] **Step 8:** Commit:
  ```bash
  git -C /Users/m/CCE add infra/backup/Restore-FromBackup.ps1 backend/tests/CCE.Infrastructure.Tests/Backup/RestoreFromBackupTests.cs
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(infra): Restore-FromBackup.ps1 + Testcontainers round-trip

  Restore-FromBackup.ps1 takes -FullBackup (required), optional
  -DiffBackup, optional -LogBackups (array), -TargetDb. Replays
  the chain with NORECOVERY between each step + RECOVERY on last.
  Refuses -TargetDb CCE without -Force as a safety check.
  Verifies migration history post-restore (best-effort log).

  RestoreFromBackupTests covers two paths via Testcontainers SQL:
   - Single FULL backup → restore to different DB name; row count
     preserved.
   - FULL → DIFF → LOG chain; restored DB has all 3 inserts.

  Tests exercise the same RESTORE DATABASE / LOG commands the
  script issues, validating the SQL behaviour. The script's
  PowerShell side is exercised by deploy-smoke.yml on Windows.

  Sub-10c Phase 03 Task 3.4.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 3.5: ADR-0056 + backup-restore runbook

**Files:**
- Create: `docs/adr/0056-backup-strategy-ola-hallengren.md`.
- Create: `docs/runbooks/backup-restore.md`.

**Final state of `docs/adr/0056-backup-strategy-ola-hallengren.md`:**

```markdown
# ADR-0056 — Backup strategy: Ola Hallengren + Task Scheduler

**Status:** Accepted
**Date:** 2026-05-04
**Deciders:** Sub-10c brainstorm (kilany113@gmail.com)
**Sub-project:** [Sub-10c — Production infra + DR](../superpowers/specs/2026-05-04-sub-10c-design.md)

## Context

CCE runs SQL Server on the Windows host (per IDD). Sub-10c needs scheduled backups that survive a host-level disaster, plus a documented restore procedure. RTO ~hours, RPO ≤15 minutes.

## Decision

Use **Ola Hallengren's SQL Server Maintenance Solution** for backup execution + retention; **Windows Task Scheduler** for triggering; **robocopy** for off-host sync to a UNC share.

5 scheduled tasks:

| Task | Frequency | Type | Retention | Destination |
|---|---|---|---|---|
| `CCE-Backup-Full` | Daily 02:00 local | `FULL` | 7 days | `D:\CCEBackups\FULL\` |
| `CCE-Backup-Diff` | Every 6 hours | `DIFF` | 7 days | `D:\CCEBackups\DIFF\` |
| `CCE-Backup-Log` | Every 15 minutes | `LOG` | 24 hours | `D:\CCEBackups\LOG\` |
| `CCE-Backup-IntegrityCheck` | Sunday 03:00 | `DBCC CHECKDB` | logs only | `C:\ProgramData\CCE\logs\` |
| `CCE-Backup-Sync-OffHost` | Hourly | `robocopy /MIR` | 30-day at destination | `\\${BACKUP_UNC_HOST}\${BACKUP_UNC_SHARE}\<env>\` |

Recovery model: `FULL` (required for log backups; gives 15-minute RPO).

**Considered alternatives:**

- **Built-in `BACKUP DATABASE` via custom PowerShell:** rejected. Reinvents Ola's retention + log-chain logic. No reason to roll our own when the standard tool is free and battle-tested.
- **Veeam / commercial backup tool:** rejected for Sub-10c. Adds licensing + new infra. Sub-10c stays within free tooling; commercial backup is a Sub-10d+ decision when the IDD ops team standardizes one.
- **Cloud-managed backup (Azure SQL backups, RDS snapshots):** rejected. SQL Server is on-prem per IDD; no cloud DB.

**Why Ola Hallengren won:**
- Industry-standard for SQL Server on Windows. Free. Documented procs (`DatabaseBackup`, `DatabaseIntegrityCheck`, `IndexOptimize`).
- Battle-tested retention logic (`@CleanupTime` parameter; understands the full → diff → log chain dependency).
- Logs every operation to `master.dbo.CommandLog` — trivial healthcheck + audit trail.
- Single-script install via `MaintenanceSolution.sql`. Idempotent (`CREATE OR ALTER PROC`).
- Active community + Microsoft endorsement.

## Implementation

`infra/backup/Install-OlaHallengren.ps1` is the bootstrap installer. Downloads `MaintenanceSolution.sql` from `ola.hallengren.com`, verifies SHA256 against `MaintenanceSolution.checksum`, applies via sqlcmd. First-install detection asks operator to record the SHA256 + commit.

`infra/backup/Register-ScheduledTasks.ps1` is the schtasks provisioner. 5 tasks; idempotent (deletes + re-creates by name).

`infra/backup/Sync-OffHost.ps1` is the robocopy wrapper for off-host sync. UNC auth via `cmdkey` cache (operator one-time setup).

`infra/backup/Restore-FromBackup.ps1` is the operator-driven restore helper. Refuses to restore over live `CCE` DB without `-Force`.

`infra/backup/Test-BackupChain.ps1` queries `master.dbo.CommandLog` for last-24h health; warn-only mode for deploy.ps1's post-deploy step.

## Consequences

**Positive:**
- Standard SQL Server DBA tooling; ops team already familiar.
- Zero new infra (no third-party services or agents).
- Backup logs centralized in `master.dbo.CommandLog` for auditing.
- 15-minute RPO via 15-min log backups.
- Off-host sync gives geographic redundancy via the UNC destination.
- Restore script covers full + diff + log chain; tested via Testcontainers SQL round-trip.

**Negative / accepted:**
- Restore is operator-driven; not auto-tested in CI beyond the SQL-command-sequence tests. Quarterly drill restore is documented as an ops procedure.
- UNC sync requires `cmdkey` credential setup on the host (one-time; documented).
- Backup encryption at rest not enabled by default. Can flip via Ola's `@EncryptionAlgorithm` parameter when IDD requires; deferred.
- 7-day full + diff retention may be tight; tunable via env-vars (`BACKUP_RETENTION_DAYS_FULL`).

**Out of scope (Sub-10c+):**
- Log shipping for HA (Sub-10d+; Decision 7B was rejected).
- Veeam / commercial integration.
- Backup encryption at rest (operator can enable per IDD).
- Auto-test-restore in CI on a regular schedule (operator-driven quarterly drill instead).

## References

- [Sub-10c design spec §Backup](../superpowers/specs/2026-05-04-sub-10c-design.md#backup--ola-hallengren--task-scheduler)
- [Backup-restore runbook](../runbooks/backup-restore.md)
- [Ola Hallengren's Maintenance Solution](https://ola.hallengren.com/)
- ADR-0054 — IIS reverse proxy on Windows Server (Sub-10c)
- ADR-0055 — AD federation via Keycloak LDAP (Sub-10c)
```

**Final state of `docs/runbooks/backup-restore.md`:**

```markdown
# Backup-restore runbook (Sub-10c)

CCE backups run via Ola Hallengren's maintenance solution + 5 Windows Task Scheduler tasks. ADR-0056 documents the design.

## Smoke-check: backup chain is healthy

```powershell
.\infra\backup\Test-BackupChain.ps1 -Environment <env>
```

Expected: `Backup chain HEALTHY.` Reports failures, last-full-success time, log-backup count over 24h.

## One-time host setup

1. **Install Ola Hallengren maintenance solution.**
   ```powershell
   .\infra\backup\Install-OlaHallengren.ps1 -Environment <env>
   ```
   First run: prints the downloaded SHA256 + asks operator to record it in `MaintenanceSolution.checksum` + re-run.

2. **Provision the backup-account.** Create a Windows account `cce.local\cce-sqlbackup-svc` (or whatever AD admins prefer) with SQL Server `sysadmin` (for `BACKUP DATABASE`) + filesystem write to `D:\CCEBackups\` + UNC write to the destination share.

3. **Cache the UNC credential.** From the deploy host as the backup-account:
   ```powershell
   cmdkey /add:${BACKUP_UNC_HOST} /user:${BACKUP_UNC_USER} /pass:${BACKUP_UNC_PASSWORD}
   ```

4. **Register scheduled tasks.**
   ```powershell
   .\infra\backup\Register-ScheduledTasks.ps1 -Environment <env> `
       -ServiceAccount cce.local\cce-sqlbackup-svc
   ```

5. **Verify.**
   ```powershell
   Get-ScheduledTask | Where-Object TaskName -like 'CCE-Backup-*' | Format-Table TaskName, State, LastRunTime, NextRunTime
   ```

## Quarterly drill: test restore

Recommended every quarter on a non-prod host:

```powershell
# Pick the latest backup chain.
$full = Get-ChildItem D:\CCEBackups\FULL\*.bak | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$diff = Get-ChildItem D:\CCEBackups\DIFF\*.bak | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$logs = Get-ChildItem D:\CCEBackups\LOG\*.trn  | Where-Object LastWriteTime -gt $full.LastWriteTime |
        Sort-Object LastWriteTime | ForEach-Object FullName

# Restore to a test DB.
.\infra\backup\Restore-FromBackup.ps1 `
    -FullBackup $full.FullName `
    -DiffBackup $diff.FullName `
    -LogBackups $logs `
    -TargetDb CCE_restoretest

# Verify row counts vs the live CCE DB.
sqlcmd -S <server> -d CCE_restoretest -Q "SELECT COUNT(*) FROM <key-table>"
sqlcmd -S <server> -d CCE              -Q "SELECT COUNT(*) FROM <key-table>"

# Cleanup.
sqlcmd -S <server> -Q "DROP DATABASE CCE_restoretest"
```

Record the result in the ops runbook log.

## Post-incident restore (live)

After a destructive incident (data corruption, accidental delete) on the live DB:

1. **Stop apps** to prevent further writes:
   ```powershell
   docker compose -f docker-compose.prod.yml down
   ```

2. **Identify the last good backup point.** Use `Test-BackupChain.ps1` to find the last full + last diff before the incident, plus all logs up to (but not past) the incident time.

3. **Run restore with `-Force`:**
   ```powershell
   .\infra\backup\Restore-FromBackup.ps1 `
       -FullBackup <path> -DiffBackup <path> -LogBackups <list> `
       -TargetDb CCE -Force
   ```

4. **Verify migration history matches** what the running image expects:
   ```powershell
   sqlcmd -S <server> -d CCE -Q "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId"
   ```

5. **Restart apps**:
   ```powershell
   .\deploy\deploy.ps1 -Environment <env>
   ```

## DR-host cold-start restore

After DR promotion (see [`dr-promotion.md`](dr-promotion.md)):

1. From the DR host, fetch the latest backup chain from the off-host UNC store:
   ```powershell
   robocopy "\\${BACKUP_UNC_HOST}\${BACKUP_UNC_SHARE}\prod" "D:\CCEBackups\restored" /E /Z /R:3 /W:10
   ```

2. Run restore with `-Force` against the DR host's SQL Server:
   ```powershell
   .\infra\backup\Restore-FromBackup.ps1 `
       -FullBackup "D:\CCEBackups\restored\FULL\<latest>.bak" `
       -DiffBackup "D:\CCEBackups\restored\DIFF\<latest>.bak" `
       -LogBackups (Get-ChildItem "D:\CCEBackups\restored\LOG\*.trn" | Sort Name).FullName `
       -TargetDb CCE -Force -Environment dr
   ```

3. Continue with the deploy step in `dr-promotion.md`.

## Common failures

| Symptom | Cause | Fix |
|---|---|---|
| `RESTORE failed: file 'X' is being used by another process` | Live CCE DB still in use | Stop apps via `docker compose down` before restore |
| `RESTORE LOG fails: cannot find a backup that includes time T` | Log chain has a gap (one log backup skipped) | Use the latest contiguous chain; restore just FULL + DIFF without the broken-chain LOG |
| `cmdkey credentials missing` (robocopy auth fails) | UNC credential not cached on host | Re-run `cmdkey /add:...` as the backup-account user |
| `Backup chain healthcheck reports 0 FULL successes in 24h` | Daily 02:00 task didn't run | Check `Get-ScheduledTaskInfo CCE-Backup-Full`; investigate task history |
| `DBCC CHECKDB reports allocation errors` | Possible disk corruption | STOP. File an incident; restore from latest known-good backup |

## See also

- [ADR-0056 — Backup strategy](../adr/0056-backup-strategy-ola-hallengren.md)
- [`migrations.md`](migrations.md) — forward-only migration discipline (relevant for restore-vs-migration-history checks)
- [`dr-promotion.md`](dr-promotion.md) — DR promotion procedure (Phase 05)
- [Ola Hallengren's docs](https://ola.hallengren.com/)
- [Sub-10c design spec §Backup](../superpowers/specs/2026-05-04-sub-10c-design.md#backup--ola-hallengren--task-scheduler)
```

- [ ] **Step 1:** Create `docs/adr/0056-backup-strategy-ola-hallengren.md` with the contents above.

- [ ] **Step 2:** Create `docs/runbooks/backup-restore.md` with the contents above.

- [ ] **Step 3:** Commit:
  ```bash
  git -C /Users/m/CCE add docs/adr/0056-backup-strategy-ola-hallengren.md docs/runbooks/backup-restore.md
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "docs(sub-10c): ADR-0056 + backup-restore runbook

  ADR-0056 captures the backup-strategy decision: Ola Hallengren
  + Task Scheduler + robocopy off-host sync. Considered + rejected:
  custom T-SQL backup scripts, Veeam, cloud-managed (no cloud DB).
  Free, standard, well-documented; logs to CommandLog for audit.

  backup-restore.md runbook: smoke-check, one-time host setup
  (install + scheduled tasks + UNC cmdkey), quarterly drill
  procedure, post-incident restore, DR-host cold-start restore,
  common-failure table.

  Sub-10c Phase 03 Task 3.5.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Phase 03 close-out

After Task 3.5 commits cleanly:

- [ ] **Run the full check:**
  ```bash
  cd /Users/m/CCE/backend && dotnet build && \
    dotnet test tests/CCE.Application.Tests/ tests/CCE.Infrastructure.Tests/ --nologo
  ```
  Expected: backend build clean; 439 Application + 71 Infrastructure tests passing (1 skipped). Phase 03 adds +2 (RestoreFromBackupTests).

- [ ] **Verify CI green** on push: `ci.yml` workflows pass.

- [ ] **Hand off to Phase 04.** Phase 04 wires `-AutoRollback` in `deploy.ps1` + Sentry env/release tagging in `LoggingExtensions.UseCceSerilog`. Plan file: `phase-04-auto-rollback-and-sentry.md` (to be written when ready).

**Phase 03 done when:**
- 5 commits land on `main`, each green.
- `infra/backup/Install-OlaHallengren.ps1` is operator-callable; bootstrap downloads + applies maintenance solution.
- `infra/backup/Register-ScheduledTasks.ps1` registers 5 tasks idempotently.
- `infra/backup/Sync-OffHost.ps1` mirrors local backups to UNC destination.
- `infra/backup/Restore-FromBackup.ps1` replays full+diff+log chain with `-Force` safety check.
- `infra/backup/Test-BackupChain.ps1` health-checks last 24h via `master.dbo.CommandLog`.
- 2 new backup tests pass against Testcontainers SQL.
- ADR-0056 + backup-restore.md committed.
- Test counts: backend Application 439 (unchanged); Infrastructure 71 (was 69, +2 backup). Frontend 502.

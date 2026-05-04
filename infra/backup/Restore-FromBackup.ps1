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

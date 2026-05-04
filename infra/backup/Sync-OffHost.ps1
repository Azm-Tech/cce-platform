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

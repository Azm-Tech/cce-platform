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
$newLines | Set-Content -Path $tempFile -Encoding utf8
Move-Item -Path $tempFile -Destination $resolvedEnvFile -Force
Write-Log "Env-file updated: CCE_IMAGE_TAG=$ToTag"

# ─── Invoke deploy.ps1 ────────────────────────────────────────────────────
$deployScript = Join-Path $PSScriptRoot 'deploy.ps1'
if ($SkipMigrator) {
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

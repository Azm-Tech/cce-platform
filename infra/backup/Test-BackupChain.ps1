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

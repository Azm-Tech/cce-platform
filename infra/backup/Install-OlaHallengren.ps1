#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10c — install Ola Hallengren's SQL Server Maintenance Solution.

.DESCRIPTION
    Downloads MaintenanceSolution.sql from ola.hallengren.com, verifies the
    SHA256 against the pinned value in MaintenanceSolution.checksum, and
    applies it to the host's SQL Server via sqlcmd.

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

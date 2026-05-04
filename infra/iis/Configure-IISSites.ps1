#requires -Version 7.0
#requires -RunAsAdministrator
<#
.SYNOPSIS
    CCE Sub-10c — provision the 4 IIS reverse-proxy sites.

.DESCRIPTION
    Reads IIS_CERT_* + IIS_HOSTNAMES + KEYCLOAK_REQUIRE_HTTPS from the
    env-file. Ensures IIS + URL Rewrite + ARR are installed (calls
    Install-ARRPrereqs.ps1 if not). Creates/updates the 4 sites with
    HTTPS bindings, named cert, and the ARR rewrite web.config. Adds
    the required server variables to ARR's allowedServerVariables list.

    Idempotent — re-running converges. Per-site rollback on failure.

.PARAMETER Environment
    Environment name. One of test, preprod, prod, dr. Default: prod.

.PARAMETER EnvFile
    Override env-file path. Default: C:\ProgramData\CCE\.env.<Environment>.

.EXAMPLE
    .\infra\iis\Configure-IISSites.ps1 -Environment prod
#>
[CmdletBinding()]
param(
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile
)

$ErrorActionPreference = 'Stop'

if (-not $EnvFile -or $EnvFile -eq '') {
    $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
}

# Logs
$logDir = 'C:\ProgramData\CCE\logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir ("iis-configure-{0}-{1:yyyyMMddTHHmmssZ}.log" -f $Environment, (Get-Date).ToUniversalTime())

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $line = "[$ts] [$Level] [$Environment] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

# ─── Parse env-file ───────────────────────────────────────────────────────
if (-not (Test-Path $EnvFile)) { Write-Error "Env-file not found: $EnvFile"; exit 1 }
$envMap = @{}
foreach ($line in Get-Content $EnvFile) {
    if ($line -match '^\s*#') { continue }
    if ($line -match '^\s*$') { continue }
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $envMap[$Matches[1]] = $Matches[2].Trim() -replace '\s*#.*$',''
    }
}

$required = @('IIS_HOSTNAMES')
$missing = $required | Where-Object { -not $envMap.ContainsKey($_) -or [string]::IsNullOrWhiteSpace($envMap[$_]) }
if ($missing) { Write-Error "Missing required env-keys: $($missing -join ', ')"; exit 1 }

# Cert: either thumbprint or PFX path+password.
$thumbprint = $envMap['IIS_CERT_THUMBPRINT']
$pfxPath    = $envMap['IIS_CERT_PFX_PATH']
$pfxPwd     = $envMap['IIS_CERT_PFX_PASSWORD']
if ([string]::IsNullOrWhiteSpace($thumbprint) -and [string]::IsNullOrWhiteSpace($pfxPath)) {
    Write-Error "Either IIS_CERT_THUMBPRINT or IIS_CERT_PFX_PATH must be set in $EnvFile."
    exit 1
}

# ─── Ensure prereqs ───────────────────────────────────────────────────────
$prereqsScript = Join-Path $PSScriptRoot 'Install-ARRPrereqs.ps1'
$rewriteInstalled = Test-Path 'HKLM:\SOFTWARE\Microsoft\IIS Extensions\URL Rewrite'
$arrInstalled     = Test-Path 'HKLM:\SOFTWARE\Microsoft\IIS Extensions\Application Request Routing'
if (-not $rewriteInstalled -or -not $arrInstalled) {
    Write-Log "Prereqs missing; running Install-ARRPrereqs.ps1..."
    & $prereqsScript
    if ($LASTEXITCODE -ne 0) { Write-Error "Install-ARRPrereqs.ps1 failed (exit $LASTEXITCODE)."; exit 1 }
}

Import-Module WebAdministration

# ─── Import PFX cert if needed ────────────────────────────────────────────
if (-not [string]::IsNullOrWhiteSpace($pfxPath)) {
    if (-not (Test-Path $pfxPath)) { Write-Error "PFX file not found: $pfxPath"; exit 1 }
    $existing = Get-ChildItem -Path Cert:\LocalMachine\My |
                Where-Object { $_.Subject -like "*$($envMap['IIS_HOSTNAMES'].Split(',')[0])*" } |
                Select-Object -First 1
    if ($existing) {
        $thumbprint = $existing.Thumbprint
        Write-Log "Cert already in store (thumbprint=$thumbprint); using it."
    } else {
        Write-Log "Importing PFX from $pfxPath..."
        $securePwd = ConvertTo-SecureString -String $pfxPwd -AsPlainText -Force
        $imported = Import-PfxCertificate -FilePath $pfxPath -CertStoreLocation Cert:\LocalMachine\My -Password $securePwd
        $thumbprint = $imported.Thumbprint
        Write-Log "Cert imported (thumbprint=$thumbprint)."
    }
}

# Verify thumbprint exists in cert store.
$certInStore = Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object { $_.Thumbprint -eq $thumbprint }
if (-not $certInStore) {
    Write-Error "Cert with thumbprint $thumbprint not found in Cert:\LocalMachine\My."
    Write-Error "Available thumbprints:"
    Get-ChildItem -Path Cert:\LocalMachine\My | Select-Object Subject, Thumbprint | Format-Table | Out-String | Write-Host
    exit 1
}

# ─── Site mapping ─────────────────────────────────────────────────────────
$hostnames = $envMap['IIS_HOSTNAMES'].Split(',') | ForEach-Object { $_.Trim() }
if ($hostnames.Count -ne 4) {
    Write-Error "IIS_HOSTNAMES must list exactly 4 hostnames (got $($hostnames.Count))."
    exit 1
}
$siteMap = @(
    @{ Name = 'CCE-ext';             Host = $hostnames[0]; BackendPort = 4200 }
    @{ Name = 'CCE-admin-Panel';     Host = $hostnames[1]; BackendPort = 4201 }
    @{ Name = 'api.CCE';             Host = $hostnames[2]; BackendPort = 5001 }
    @{ Name = 'Api.CCE-admin-Panel'; Host = $hostnames[3]; BackendPort = 5002 }
)

# ─── Add allowed server variables to ARR ──────────────────────────────────
$allowedVars = @('HTTP_X_FORWARDED_HOST','HTTP_X_FORWARDED_PROTO','HTTP_X_FORWARDED_FOR')
foreach ($v in $allowedVars) {
    $existing = Get-WebConfiguration -PSPath 'MACHINE/WEBROOT/APPHOST' `
                -Filter "system.webServer/rewrite/allowedServerVariables/add[@name='$v']" -ErrorAction SilentlyContinue
    if (-not $existing) {
        Add-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' `
            -Filter 'system.webServer/rewrite/allowedServerVariables' `
            -Name '.' -Value @{ name = $v }
        Write-Log "Added allowed server variable: $v"
    }
}

# ─── Provision each site ──────────────────────────────────────────────────
$template = Get-Content (Join-Path $PSScriptRoot 'web.config.template') -Raw
$siteRootBase = 'C:\inetpub\cce'
if (-not (Test-Path $siteRootBase)) { New-Item -ItemType Directory -Path $siteRootBase -Force | Out-Null }

foreach ($s in $siteMap) {
    $siteName = $s.Name
    $hostname = $s.Host
    $port     = $s.BackendPort
    $siteRoot = Join-Path $siteRootBase $siteName

    try {
        Write-Log "Provisioning site '$siteName' (host=$hostname, backend=localhost:$port)..."

        # Site directory + web.config from template.
        if (-not (Test-Path $siteRoot)) { New-Item -ItemType Directory -Path $siteRoot -Force | Out-Null }
        $rendered = $template.Replace('{BACKEND_PORT}', $port.ToString())
        Set-Content -Path (Join-Path $siteRoot 'web.config') -Value $rendered -Encoding utf8

        # Site exists? Update bindings; else create.
        $existingSite = Get-Website -Name $siteName -ErrorAction SilentlyContinue
        if ($existingSite) {
            Write-Log "Site '$siteName' exists; refreshing bindings."
            Remove-WebBinding -Name $siteName -Protocol https -ErrorAction SilentlyContinue
        } else {
            New-Website -Name $siteName -PhysicalPath $siteRoot -Port 443 -HostHeader $hostname `
                -Ssl -Force | Out-Null
            Write-Log "Site '$siteName' created."
        }

        # HTTPS binding with SNI.
        New-WebBinding -Name $siteName -Protocol https -Port 443 -HostHeader $hostname -SslFlags 1
        $binding = Get-WebBinding -Name $siteName -Protocol https
        $binding.AddSslCertificate($thumbprint, 'My')
        Write-Log "HTTPS binding for '$siteName' attached (thumbprint=$thumbprint)."
    } catch {
        Write-Log -Level 'ERROR' "Failed to provision '$siteName': $($_.Exception.Message). Rolling back."
        try { Remove-Website -Name $siteName -ErrorAction SilentlyContinue } catch {}
        throw
    }
}

# ─── Restart IIS ──────────────────────────────────────────────────────────
Write-Log "Restarting IIS to pick up changes..."
iisreset /restart | Out-Null
Write-Log "IIS restarted."

Write-Log "Configure-IISSites.ps1 done."
exit 0

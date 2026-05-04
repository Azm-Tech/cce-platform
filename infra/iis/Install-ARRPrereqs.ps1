#requires -Version 7.0
#requires -RunAsAdministrator
<#
.SYNOPSIS
    CCE Sub-10c — install IIS + URL Rewrite + ARR prerequisites.

.DESCRIPTION
    Idempotent. Installs the Web-Server Windows feature with the sub-
    features needed for ARR. Downloads + silently installs the URL
    Rewrite 2.1 + ARR 3.0 MSI packages from Microsoft.

    Skips already-installed components. Designed to run during
    one-time host setup; safe to re-run.

.EXAMPLE
    .\infra\iis\Install-ARRPrereqs.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# Logs
$logDir = 'C:\ProgramData\CCE\logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir ("iis-prereqs-{0:yyyyMMddTHHmmssZ}.log" -f (Get-Date).ToUniversalTime())

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $line = "[$ts] [$Level] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

# ─── Install IIS Windows feature ──────────────────────────────────────────
$iisFeatures = @(
    'Web-Server',
    'Web-Common-Http',
    'Web-Default-Doc',
    'Web-Dir-Browsing',
    'Web-Http-Errors',
    'Web-Static-Content',
    'Web-Http-Redirect',
    'Web-Performance',
    'Web-Stat-Compression',
    'Web-Dyn-Compression',
    'Web-Health',
    'Web-Http-Logging',
    'Web-Log-Libraries',
    'Web-Request-Monitor',
    'Web-Security',
    'Web-Filtering',
    'Web-App-Dev',
    'Web-Net-Ext45',
    'Web-Asp-Net45',
    'Web-ISAPI-Ext',
    'Web-ISAPI-Filter',
    'Web-Mgmt-Tools',
    'Web-Mgmt-Console'
)

foreach ($f in $iisFeatures) {
    $state = (Get-WindowsFeature -Name $f -ErrorAction SilentlyContinue).InstallState
    if ($state -eq 'Installed') {
        Write-Log "IIS feature already installed: $f"
    } else {
        Write-Log "Installing IIS feature: $f"
        Install-WindowsFeature -Name $f -IncludeManagementTools | Out-Null
    }
}

# ─── Install URL Rewrite 2.1 ──────────────────────────────────────────────
$urlRewriteVersion = '2.1'
$urlRewriteRegPath = 'HKLM:\SOFTWARE\Microsoft\IIS Extensions\URL Rewrite'
if (Test-Path $urlRewriteRegPath) {
    $installedVersion = (Get-ItemProperty $urlRewriteRegPath -Name 'Version' -ErrorAction SilentlyContinue).Version
    if ($installedVersion) {
        Write-Log "URL Rewrite already installed: version $installedVersion (skipping)"
    } else {
        Write-Log "URL Rewrite registry key exists but no version detected; will reinstall."
        $installedVersion = $null
    }
} else {
    $installedVersion = $null
}

if (-not $installedVersion) {
    $msi = Join-Path $env:TEMP 'rewrite_amd64_en-US.msi'
    Write-Log "Downloading URL Rewrite $urlRewriteVersion MSI..."
    Invoke-WebRequest -Uri 'https://download.microsoft.com/download/1/2/8/128E2E22-C1B9-44A4-BE2A-5859ED1D4592/rewrite_amd64_en-US.msi' `
                      -OutFile $msi -UseBasicParsing
    Write-Log "Installing URL Rewrite (silent)..."
    $proc = Start-Process msiexec.exe -ArgumentList "/i `"$msi`" /qn /norestart" -Wait -PassThru -NoNewWindow
    if ($proc.ExitCode -ne 0) {
        Write-Log -Level 'ERROR' "URL Rewrite installer exited with code $($proc.ExitCode)."
        exit $proc.ExitCode
    }
    Write-Log "URL Rewrite installed."
}

# ─── Install ARR 3.0 ───────────────────────────────────────────────────────
$arrRegPath = 'HKLM:\SOFTWARE\Microsoft\IIS Extensions\Application Request Routing'
if (Test-Path $arrRegPath) {
    $installedArrVersion = (Get-ItemProperty $arrRegPath -Name 'Version' -ErrorAction SilentlyContinue).Version
    if ($installedArrVersion) {
        Write-Log "ARR already installed: version $installedArrVersion (skipping)"
    } else {
        Write-Log "ARR registry key exists but no version detected; will reinstall."
        $installedArrVersion = $null
    }
} else {
    $installedArrVersion = $null
}

if (-not $installedArrVersion) {
    $msi = Join-Path $env:TEMP 'requestRouter_amd64.msi'
    Write-Log "Downloading ARR 3.0 MSI..."
    Invoke-WebRequest -Uri 'https://download.microsoft.com/download/E/9/8/E9849D6A-020E-47E4-9FD0-A023E99B54EB/requestRouter_amd64.msi' `
                      -OutFile $msi -UseBasicParsing
    Write-Log "Installing ARR (silent)..."
    $proc = Start-Process msiexec.exe -ArgumentList "/i `"$msi`" /qn /norestart" -Wait -PassThru -NoNewWindow
    if ($proc.ExitCode -ne 0) {
        Write-Log -Level 'ERROR' "ARR installer exited with code $($proc.ExitCode)."
        exit $proc.ExitCode
    }
    Write-Log "ARR installed."
}

# ─── Enable global ARR proxy ───────────────────────────────────────────────
Import-Module WebAdministration
$proxyEnabled = (Get-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' `
    -Filter 'system.webServer/proxy' -Name 'enabled' -ErrorAction SilentlyContinue).Value
if ($proxyEnabled -eq 'True' -or $proxyEnabled -eq $true) {
    Write-Log "ARR global proxy already enabled."
} else {
    Write-Log "Enabling ARR global proxy..."
    Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' `
        -Filter 'system.webServer/proxy' -Name 'enabled' -Value 'True'
    Write-Log "ARR global proxy enabled."
}

Write-Log "Install-ARRPrereqs.ps1 done."
exit 0

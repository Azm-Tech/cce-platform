#requires -Version 7.0
#requires -Modules @{ ModuleName = 'Microsoft.Graph.Applications'; ModuleVersion = '2.0.0' }
<#
.SYNOPSIS
    CCE Sub-11 — provision Entra ID app registration for the CCE platform.

.DESCRIPTION
    Idempotently provisions the multi-tenant Entra ID app registration
    backing the entire CCE platform. Reads ENTRA_* keys from the env-file,
    expands {{HOSTNAME_*}} placeholders in the manifest JSON, then either
    PATCHes the existing app or POSTs a new one. Re-runs are safe.

    The 5 app roles (cce-admin/editor/reviewer/expert/user) are deterministic
    GUIDs in the manifest, so re-runs match existing roles by ID.

.PARAMETER Environment
    Environment name. One of test, preprod, prod, dr. Default: prod.

.PARAMETER EnvFile
    Override env-file path. Default: C:\ProgramData\CCE\.env.<Environment>.

.PARAMETER ManifestJson
    Path to the manifest JSON template. Default: ./app-registration-manifest.json
    (relative to this script).

.EXAMPLE
    .\infra\entra\apply-app-registration.ps1 -Environment prod
#>
[CmdletBinding()]
param(
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile,
    [string]$ManifestJson
)

$ErrorActionPreference = 'Stop'

if (-not $EnvFile -or $EnvFile -eq '') {
    $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
}
if (-not $ManifestJson -or $ManifestJson -eq '') {
    $ManifestJson = Join-Path $PSScriptRoot 'app-registration-manifest.json'
}

# Logs
$logDir = 'C:\ProgramData\CCE\logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir ("entra-app-{0}-{1:yyyyMMddTHHmmssZ}.log" -f $Environment, (Get-Date).ToUniversalTime())

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $line = "[$ts] [$Level] [$Environment] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

# ─── Parse env-file ─────────────────────────────────────────────────────────
if (-not (Test-Path $EnvFile))      { Write-Error "Env-file not found: $EnvFile";      exit 1 }
if (-not (Test-Path $ManifestJson)) { Write-Error "Manifest not found: $ManifestJson"; exit 1 }

$envMap = @{}
foreach ($line in Get-Content $EnvFile) {
    if ($line -match '^\s*#') { continue }
    if ($line -match '^\s*$') { continue }
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $envMap[$Matches[1]] = $Matches[2].Trim() -replace '\s*#.*$',''
    }
}

$required = @(
    'ENTRA_TENANT_ID','ENTRA_PROVISIONER_CLIENT_ID','ENTRA_PROVISIONER_CLIENT_SECRET',
    'HOSTNAME_PORTAL_TEST','HOSTNAME_PORTAL_PREPROD','HOSTNAME_PORTAL_PROD','HOSTNAME_PORTAL_DR',
    'HOSTNAME_CMS_TEST','HOSTNAME_CMS_PREPROD','HOSTNAME_CMS_PROD','HOSTNAME_CMS_DR'
)
$missing = $required | Where-Object { -not $envMap.ContainsKey($_) -or [string]::IsNullOrWhiteSpace($envMap[$_]) }
if ($missing) { Write-Error "Missing required env-keys: $($missing -join ', ')"; exit 1 }

# ─── Substitute hostname placeholders in manifest ───────────────────────────
$manifestContent = Get-Content $ManifestJson -Raw
foreach ($key in $required) {
    if ($key -like 'HOSTNAME_*') {
        $manifestContent = $manifestContent.Replace("{{$key}}", $envMap[$key])
    }
}
$manifest = $manifestContent | ConvertFrom-Json -Depth 10 -AsHashtable

# ─── Connect to Graph (app-only — used to provision the app itself) ────────
Write-Log "Connecting to Microsoft Graph as provisioner app $($envMap['ENTRA_PROVISIONER_CLIENT_ID']) in tenant $($envMap['ENTRA_TENANT_ID'])"
$secureSecret = ConvertTo-SecureString $envMap['ENTRA_PROVISIONER_CLIENT_SECRET'] -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential($envMap['ENTRA_PROVISIONER_CLIENT_ID'], $secureSecret)
Connect-MgGraph -TenantId $envMap['ENTRA_TENANT_ID'] -ClientSecretCredential $cred -NoWelcome

# ─── Look up the existing app by displayName ───────────────────────────────
Write-Log "Looking up existing application: $($manifest.displayName)"
$existing = Get-MgApplication -Filter "displayName eq '$($manifest.displayName)'" -Top 1

if ($null -eq $existing) {
    Write-Log "App not found; creating new application"
    $created = New-MgApplication -BodyParameter $manifest
    Write-Log "Created application id=$($created.Id) appId=$($created.AppId)"
} else {
    Write-Log "Found existing application id=$($existing.Id) appId=$($existing.AppId); PATCHing"
    Update-MgApplication -ApplicationId $existing.Id -BodyParameter $manifest
    Write-Log "Updated application id=$($existing.Id)"
}

Write-Log "Done."
Disconnect-MgGraph | Out-Null

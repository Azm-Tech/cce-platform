#requires -Version 7.0
#requires -Modules @{ ModuleName = 'Microsoft.Graph.Identity.DirectoryManagement'; ModuleVersion = '2.0.0' }
<#
.SYNOPSIS
    CCE Sub-11 — apply CCE branding to the Entra ID sign-in page.

.DESCRIPTION
    Idempotently uploads CCE banner / square / background images plus a
    custom.css to Entra ID's organizationalBranding endpoint. Only renders
    for users signing in to the CCE tenant; multi-tenant partner users see
    their own home-tenant branding (Entra ID security boundary).

    Requires Entra ID P1 or P2 SKU. If the tenant doesn't have it, the
    script logs a warning and exits 0 (does not fail the deploy pipeline).

.PARAMETER Environment
    Environment name. One of test, preprod, prod, dr. Default: prod.

.PARAMETER EnvFile
    Override env-file path. Default: C:\ProgramData\CCE\.env.<Environment>.

.PARAMETER BrandingDir
    Path to the branding asset directory. Default: ./branding (relative to script).

.EXAMPLE
    .\infra\entra\Configure-Branding.ps1 -Environment prod
#>
[CmdletBinding()]
param(
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile,
    [string]$BrandingDir
)

$ErrorActionPreference = 'Stop'

if (-not $EnvFile -or $EnvFile -eq '') {
    $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
}
if (-not $BrandingDir -or $BrandingDir -eq '') {
    $BrandingDir = Join-Path $PSScriptRoot 'branding'
}

# Logs
$logDir = 'C:\ProgramData\CCE\logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir ("entra-branding-{0}-{1:yyyyMMddTHHmmssZ}.log" -f $Environment, (Get-Date).ToUniversalTime())

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $line = "[$ts] [$Level] [$Environment] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

# ─── Parse env-file ─────────────────────────────────────────────────────────
if (-not (Test-Path $EnvFile))     { Write-Error "Env-file not found: $EnvFile";      exit 1 }
if (-not (Test-Path $BrandingDir)) { Write-Error "Branding dir not found: $BrandingDir"; exit 1 }

$envMap = @{}
foreach ($line in Get-Content $EnvFile) {
    if ($line -match '^\s*#') { continue }
    if ($line -match '^\s*$') { continue }
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $envMap[$Matches[1]] = $Matches[2].Trim() -replace '\s*#.*$',''
    }
}

$required = @('ENTRA_TENANT_ID','ENTRA_PROVISIONER_CLIENT_ID','ENTRA_PROVISIONER_CLIENT_SECRET')
$missing = $required | Where-Object { -not $envMap.ContainsKey($_) -or [string]::IsNullOrWhiteSpace($envMap[$_]) }
if ($missing) { Write-Error "Missing required env-keys: $($missing -join ', ')"; exit 1 }

# ─── Connect ───────────────────────────────────────────────────────────────
Write-Log "Connecting to Microsoft Graph as provisioner $($envMap['ENTRA_PROVISIONER_CLIENT_ID'])"
$secureSecret = ConvertTo-SecureString $envMap['ENTRA_PROVISIONER_CLIENT_SECRET'] -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential($envMap['ENTRA_PROVISIONER_CLIENT_ID'], $secureSecret)
Connect-MgGraph -TenantId $envMap['ENTRA_TENANT_ID'] -ClientSecretCredential $cred -NoWelcome

# ─── Detect P1/P2 licensing (skip + warn if not present) ───────────────────
$skus = Get-MgSubscribedSku -ErrorAction SilentlyContinue
$hasP1OrP2 = $skus | Where-Object {
    $_.SkuPartNumber -in @('AAD_PREMIUM','AAD_PREMIUM_P2','ENTERPRISEPACKPLUS_FACULTY','EMS','ENTERPRISEPREMIUM','EMSPREMIUM','SPE_E5','SPE_E3')
}
if (-not $hasP1OrP2) {
    Write-Log "WARNING: tenant does not have Entra ID P1 or P2 SKU; organizationalBranding API requires P1/P2. Skipping." 'WARN'
    Disconnect-MgGraph | Out-Null
    exit 0
}

# ─── Upload default-localization branding ──────────────────────────────────
$bannerPath     = Join-Path $BrandingDir 'banner.png'
$squarePath     = Join-Path $BrandingDir 'square.png'
$backgroundPath = Join-Path $BrandingDir 'background.png'
$customCssPath  = Join-Path $BrandingDir 'custom.css'

# The default localization is PUT /organization/{id}/branding (no localizationId).
$orgId = (Get-MgOrganization -Top 1).Id
$baseUri = "https://graph.microsoft.com/v1.0/organization/$orgId/branding"

# Helper: PATCH with the binary in the body for an asset slot.
function Set-BrandingAsset {
    param([string]$Slot, [string]$FilePath, [string]$ContentType)
    if (-not (Test-Path $FilePath)) {
        Write-Log "Asset $Slot not present at $FilePath; skipping"
        return
    }
    Write-Log "Uploading branding asset $Slot from $FilePath"
    Invoke-MgGraphRequest -Method PATCH -Uri $baseUri `
        -Headers @{ 'Content-Type' = $ContentType } `
        -InputFilePath $FilePath
}

Set-BrandingAsset -Slot 'bannerLogo'      -FilePath $bannerPath     -ContentType 'image/png'
Set-BrandingAsset -Slot 'squareLogo'      -FilePath $squarePath     -ContentType 'image/png'
Set-BrandingAsset -Slot 'backgroundImage' -FilePath $backgroundPath -ContentType 'image/png'
Set-BrandingAsset -Slot 'customCSS'       -FilePath $customCssPath  -ContentType 'text/css'

Write-Log "Branding applied to tenant $($envMap['ENTRA_TENANT_ID'])"
Disconnect-MgGraph | Out-Null

# Sub-11 Phase 02 — App registration + branding provisioning

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the operator-facing surface for provisioning a CCE Entra ID **app registration** (multi-tenant, with 5 app roles + redirect URIs covering both BFFs across 4 envs + localhost) and applying CCE-branded **company branding** to the sign-in page. Plus the 4 ADRs that document the decisions locked in during brainstorming.

**Architecture:** Two PowerShell 7 scripts under `infra/entra/`, each idempotent (reads existing object, PATCHes if present, POSTs if absent). Both consume CCE's standard env-file pattern (`C:\ProgramData\CCE\.env.<env>`) and authenticate to Microsoft Graph via the `Microsoft.Graph.Authentication` module's `Connect-MgGraph -ClientSecretCredential` for unattended operation. The app-registration manifest JSON is templated with `{{HOSTNAME_*}}` placeholders that the script substitutes from the env-file before submitting.

**Tech Stack:** PowerShell 7+ · `Microsoft.Graph` PS module 2.x · Microsoft Graph v1.0 application + organizationalBranding APIs · existing CCE env-file pattern (`apply-realm.ps1` from Sub-10c is the template)

**No backend code changes** — Phase 02 is pure infra + ADRs. Backend tests stay at 87 (Domain 290 / Application 439 / Architecture 12 / Infrastructure 87).

---

## Phase 02 deliverables (5 tasks)

| # | Artifact | Purpose |
|---|---|---|
| 2.1 | `app-registration-manifest.json` + `apply-app-registration.ps1` | Idempotently provisions the CCE app registration with 5 app roles + 10 redirect URIs |
| 2.2 | `Configure-Branding.ps1` + `infra/entra/branding/` placeholders | Applies CCE branding to the sign-in page (CCE tenant only — partner tenants render their own branding) |
| 2.3 | `infra/entra/README.md` + `.env.{local,test,preprod,prod,dr}.example` updates | Operator runbook + env-key catalogue alignment |
| 2.4 | ADR-0058 (Entra ID multi-tenant + Graph writes) + ADR-0059 (app roles vs groups) | Architectural rationale for decisions 1, 2, 3, 5 |
| 2.5 | ADR-0060 (Conditional Access for MFA) + ADR-0055 supersede | Architectural rationale for decision 4; legacy ADR retired |

---

## Global conventions (Phase 02)

- All scripts target **PowerShell 7+** (`#requires -Version 7.0`).
- All scripts use the existing log pattern: append timestamped lines to `C:\ProgramData\CCE\logs\<script>-<env>-<utc-iso8601>.log` and `Write-Host` the same.
- Idempotency: scripts must be safe to re-run. Re-runs PATCH existing resources rather than duplicating.
- All commits use Conventional Commits (`feat`, `docs`, `chore`).
- All commits include the `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` trailer.
- After Phase 02: backend test counts unchanged (Phase 02 is infra-only).

---

## Task 2.1: App-registration manifest + `apply-app-registration.ps1`

**Files:**
- Create: `infra/entra/app-registration-manifest.json`
- Create: `infra/entra/apply-app-registration.ps1`

The manifest defines the 5 app roles (locked in spec §"App roles") + 10 redirect URIs (2 BFFs × 4 envs + 2 localhost). The script reads it, substitutes hostname placeholders from the env-file, and PATCHes/POSTs against the Graph `/applications` endpoint.

- [ ] **Step 1: Create `infra/entra/app-registration-manifest.json`**

The 5 app roles:
- `cce-admin` — full CMS access
- `cce-editor` — content authoring
- `cce-reviewer` — review queue access
- `cce-expert` — expert-only sections
- `cce-user` — base end-user role

The 10 redirect URIs (placeholders substituted by the PS1 script):
- `http://localhost:5101/signin-oidc` (External API local)
- `http://localhost:5102/signin-oidc` (Internal API local)
- `https://{{HOSTNAME_PORTAL_TEST}}/signin-oidc`
- `https://{{HOSTNAME_PORTAL_PREPROD}}/signin-oidc`
- `https://{{HOSTNAME_PORTAL_PROD}}/signin-oidc`
- `https://{{HOSTNAME_PORTAL_DR}}/signin-oidc`
- `https://{{HOSTNAME_CMS_TEST}}/signin-oidc`
- `https://{{HOSTNAME_CMS_PREPROD}}/signin-oidc`
- `https://{{HOSTNAME_CMS_PROD}}/signin-oidc`
- `https://{{HOSTNAME_CMS_DR}}/signin-oidc`

```json
{
  "displayName": "CCE Knowledge Center",
  "signInAudience": "AzureADMultipleOrgs",
  "description": "Multi-tenant Entra ID app registration for the CCE Knowledge Center platform. Provisioned by infra/entra/apply-app-registration.ps1. Source of truth: this manifest.",
  "web": {
    "redirectUris": [
      "http://localhost:5101/signin-oidc",
      "http://localhost:5102/signin-oidc",
      "https://{{HOSTNAME_PORTAL_TEST}}/signin-oidc",
      "https://{{HOSTNAME_PORTAL_PREPROD}}/signin-oidc",
      "https://{{HOSTNAME_PORTAL_PROD}}/signin-oidc",
      "https://{{HOSTNAME_PORTAL_DR}}/signin-oidc",
      "https://{{HOSTNAME_CMS_TEST}}/signin-oidc",
      "https://{{HOSTNAME_CMS_PREPROD}}/signin-oidc",
      "https://{{HOSTNAME_CMS_PROD}}/signin-oidc",
      "https://{{HOSTNAME_CMS_DR}}/signin-oidc"
    ],
    "implicitGrantSettings": {
      "enableAccessTokenIssuance": false,
      "enableIdTokenIssuance": false
    },
    "logoutUrl": "https://{{HOSTNAME_PORTAL_PROD}}/signout-oidc"
  },
  "appRoles": [
    {
      "id": "11111111-aaaa-1111-aaaa-111111111111",
      "displayName": "CCE Admin",
      "value": "cce-admin",
      "description": "Full administrative access to the CCE CMS and platform settings.",
      "allowedMemberTypes": ["User"],
      "isEnabled": true
    },
    {
      "id": "22222222-aaaa-2222-aaaa-222222222222",
      "displayName": "CCE Editor",
      "value": "cce-editor",
      "description": "Content-authoring access (resources, news, events, pages).",
      "allowedMemberTypes": ["User"],
      "isEnabled": true
    },
    {
      "id": "33333333-aaaa-3333-aaaa-333333333333",
      "displayName": "CCE Reviewer",
      "value": "cce-reviewer",
      "description": "Read access plus expert-request-review queue.",
      "allowedMemberTypes": ["User"],
      "isEnabled": true
    },
    {
      "id": "44444444-aaaa-4444-aaaa-444444444444",
      "displayName": "CCE Expert",
      "value": "cce-expert",
      "description": "Expert-only authoring (community posts, expert profiles).",
      "allowedMemberTypes": ["User"],
      "isEnabled": true
    },
    {
      "id": "55555555-aaaa-5555-aaaa-555555555555",
      "displayName": "CCE User",
      "value": "cce-user",
      "description": "Base end-user role; required for any signed-in access.",
      "allowedMemberTypes": ["User"],
      "isEnabled": true
    }
  ],
  "requiredResourceAccess": [
    {
      "resourceAppId": "00000003-0000-0000-c000-000000000000",
      "comment": "Microsoft Graph",
      "resourceAccess": [
        {
          "id": "741f803b-c850-494e-b5df-cde7c675a1ca",
          "comment": "User.ReadWrite.All — application permission; required for user-create",
          "type": "Role"
        },
        {
          "id": "df021288-bdef-4463-88db-98f22de89214",
          "comment": "User.Read.All — application permission; required for user lookup",
          "type": "Role"
        },
        {
          "id": "e1fe6dd8-ba31-4d61-89e7-88639da4683d",
          "comment": "User.Read — delegated; required for OIDC sign-in",
          "type": "Scope"
        }
      ]
    }
  ]
}
```

The `appRoles[].id` GUIDs are deterministic (not random) so re-runs match existing roles by ID. The Graph permission GUIDs are well-known — they map to `User.ReadWrite.All` (`741f803b-...`), `User.Read.All` (`df021288-...`), and `User.Read` (`e1fe6dd8-...`).

- [ ] **Step 2: Create `infra/entra/apply-app-registration.ps1`**

```powershell
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
    Path to the manifest JSON template. Default: ./app-registration-manifest.json.

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
$manifest = $manifestContent | ConvertFrom-Json -Depth 10

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
```

- [ ] **Step 3: Sanity-check the manifest parses as valid JSON**

```bash
python3 -c "import json; json.load(open('/Users/m/CCE/infra/entra/app-registration-manifest.json'))" && echo "manifest JSON valid"
```

Expected: `manifest JSON valid`.

- [ ] **Step 4: Sanity-check the PowerShell script parses cleanly**

If `pwsh` is available on the dev box:
```bash
pwsh -NoProfile -Command "Get-Content /Users/m/CCE/infra/entra/apply-app-registration.ps1 | Out-String | Invoke-Expression -ErrorAction SilentlyContinue; \$null"
```

If `pwsh` is not available locally, skip — operator-side runtime is Windows Server 2022 with PowerShell 7+.

- [ ] **Step 5: Commit**

```bash
git add infra/entra/app-registration-manifest.json infra/entra/apply-app-registration.ps1
git commit -m "$(cat <<'EOF'
feat(infra/entra): app-registration manifest + apply-app-registration.ps1

Idempotently provisions the multi-tenant CCE Entra ID app registration
with 5 app roles (cce-admin/editor/reviewer/expert/user) and 10 redirect
URIs (2 BFFs × 4 envs + 2 localhost). Manifest holds {{HOSTNAME_*}}
placeholders that the script substitutes from the env-file at apply time.

Re-runs PATCH the existing app rather than duplicating. Connects to
Graph as the dedicated ENTRA_PROVISIONER_* service principal (separate
from the runtime CCE app — provisioner has Application.ReadWrite.All;
runtime has User.ReadWrite.All).

Mirrors the Sub-10c apply-realm.ps1 pattern for consistency: env-file
parsing, log-file conventions, idempotent PATCH-or-POST.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2.2: `Configure-Branding.ps1` + branding asset placeholders

**Files:**
- Create: `infra/entra/Configure-Branding.ps1`
- Create: `infra/entra/branding/README.md`
- Create: `infra/entra/branding/.gitkeep`
- Create: `infra/entra/branding/banner.placeholder.txt` (instructions where to drop the real PNG)
- Create: `infra/entra/branding/square.placeholder.txt`
- Create: `infra/entra/branding/background.placeholder.txt`
- Create: `infra/entra/branding/custom.css.example`

`Configure-Branding.ps1` calls Graph's `/organization/{id}/branding/localizations` endpoint to upload PNGs + CSS for the CCE tenant. **Important security note**: company branding only renders for users signing in **to the home tenant**. Multi-tenant partner users see their own home-tenant branding. This is a hard Entra ID guarantee, not a configuration choice.

**Licensing note**: organizationalBranding API requires Entra ID P1 or P2 SKU. Phase 02 ships the script regardless; if the operator's tenant doesn't have P1/P2, the script logs a warning and exits 0 instead of failing.

- [ ] **Step 1: Create `infra/entra/Configure-Branding.ps1`**

```powershell
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

# Helper: PATCH with multipart for an asset slot.
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

Set-BrandingAsset -Slot 'bannerLogo'         -FilePath $bannerPath     -ContentType 'image/png'
Set-BrandingAsset -Slot 'squareLogo'         -FilePath $squarePath     -ContentType 'image/png'
Set-BrandingAsset -Slot 'backgroundImage'    -FilePath $backgroundPath -ContentType 'image/png'
Set-BrandingAsset -Slot 'customCSS'          -FilePath $customCssPath  -ContentType 'text/css'

Write-Log "Branding applied to tenant $($envMap['ENTRA_TENANT_ID'])"
Disconnect-MgGraph | Out-Null
```

The Graph organizationalBranding API uses `PATCH` with `Content-Type: image/png` (or `text/css`) and the binary in the body — no JSON wrapper for asset uploads. Each slot is a separate PATCH call.

- [ ] **Step 2: Create branding asset placeholders**

`infra/entra/branding/README.md`:

```markdown
# CCE Entra ID branding assets

Drop the following files here before running `Configure-Branding.ps1`:

- `banner.png` — 280×60 px max, < 50 KB. Shown above the username field on the sign-in page.
- `square.png` — 240×240 px, < 50 KB. Shown when "stay signed in" page renders.
- `background.png` — 1920×1080 recommended, < 300 KB. Background of the sign-in page.
- `custom.css` — < 25 KB. Optional; overrides default sign-in CSS.

These files are gitignored to keep brand assets out of the repo. Source-of-truth lives in
the design system (see `frontend/libs/ui-kit/`). Operators copy the rendered assets here
before running the script.

Sizing + format guidance: <https://learn.microsoft.com/entra/fundamentals/how-to-customize-branding>
```

`infra/entra/branding/.gitkeep` — empty file to track the directory.

`infra/entra/branding/custom.css.example`:

```css
/* CCE custom Entra ID sign-in CSS — example.
 * Copy to custom.css and customize. Maximum 25 KB.
 * Documented selectors: https://learn.microsoft.com/entra/fundamentals/how-to-customize-branding
 */

/* Brand-color overrides — match CCE design tokens. */
:root {
    --cce-primary: #2D5A87;
    --cce-accent: #F4A300;
}

/* Sign-in container. */
.ext-sign-in-box {
    background: var(--cce-primary);
}

/* Submit button. */
.ext-button.ext-primary {
    background-color: var(--cce-accent);
    border-color: var(--cce-accent);
}
```

Also add `infra/entra/branding/*.png` to `.gitignore` to prevent committing the actual brand assets:

- [ ] **Step 3: Update `.gitignore`**

Append to `/Users/m/CCE/.gitignore`:

```
# Entra ID branding assets — copied from design system at deploy time, not committed.
infra/entra/branding/*.png
infra/entra/branding/custom.css
```

- [ ] **Step 4: Sanity-check the script + assets land cleanly**

```bash
ls -la /Users/m/CCE/infra/entra/branding/
```

Expected: `.gitkeep`, `README.md`, `custom.css.example` (no PNGs — those are operator-supplied).

- [ ] **Step 5: Commit**

```bash
git add infra/entra/Configure-Branding.ps1 infra/entra/branding/ .gitignore
git commit -m "$(cat <<'EOF'
feat(infra/entra): Configure-Branding.ps1 + branding asset placeholders

Idempotently uploads CCE banner / square / background / custom.css to
the Entra ID organizationalBranding endpoint for the CCE tenant. Only
renders for users signing in to the CCE tenant; partner-tenant users
see their own home-tenant branding (hard Entra ID security boundary,
not configurable).

Requires Entra ID P1 or P2 SKU. If the tenant doesn't have it, the
script logs a warning and exits 0 instead of failing — keeps the
deploy pipeline green for tenants without P1.

PNG assets (banner.png, square.png, background.png) and custom.css
are gitignored — operators copy them from the design system at deploy
time. README + custom.css.example committed for operator guidance.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2.3: `infra/entra/README.md` + env-file updates

**Files:**
- Create: `infra/entra/README.md`
- Modify: `.env.example`, `.env.local.example`, `.env.test.example`, `.env.preprod.example`, `.env.prod.example`, `.env.dr.example`

The README is the operator-facing runbook for both scripts. The env-file updates add the new `ENTRA_*` and `HOSTNAME_*` keys; legacy `KEYCLOAK_*` keys remain (commented as "kept until Phase 04 cutover") so existing deploy automation doesn't break mid-phase.

- [ ] **Step 1: Create `infra/entra/README.md`**

```markdown
# CCE Entra ID provisioning

Sub-11 Phase 02 ships two PowerShell 7 scripts that provision CCE's
multi-tenant Entra ID app registration plus optional company branding
on the sign-in page.

## Scripts

### `apply-app-registration.ps1`

Idempotently creates or updates the **CCE Knowledge Center** app
registration with:
- 5 app roles (`cce-admin`, `cce-editor`, `cce-reviewer`,
  `cce-expert`, `cce-user`)
- 10 OIDC redirect URIs (2 BFFs × 4 envs + 2 localhost)
- 3 Graph permissions (`User.ReadWrite.All` app, `User.Read.All` app,
  `User.Read` delegated)
- Multi-tenant signInAudience (`AzureADMultipleOrgs`)

Re-runs PATCH the existing app — safe to run on every deploy.

### `Configure-Branding.ps1`

Uploads CCE-branded `banner.png`, `square.png`, `background.png`, and
`custom.css` to Entra ID's organizationalBranding endpoint. Renders
**only for CCE-tenant users**; partner-tenant users see their own
home-tenant branding.

Requires Entra ID P1 or P2 SKU. Without it, the script logs a warning
and exits 0.

## Prerequisites

Both scripts require:
- PowerShell 7+
- `Microsoft.Graph` PS module 2.x (`Install-Module Microsoft.Graph`)
- A **provisioner** Entra ID app registration with `Application.ReadWrite.All`
  + `Organization.ReadWrite.All` admin-consented Graph application
  permissions. **Do not** reuse the runtime CCE app for this — split
  privilege.
- Env-file at `C:\ProgramData\CCE\.env.<env>` containing the
  `ENTRA_*` and `HOSTNAME_*` keys (see `.env.<env>.example`).

## One-time provisioner setup (per Entra ID tenant)

1. In the Entra ID portal → **App registrations** → **New registration**:
   - Name: `CCE Provisioner`
   - Supported account types: **Single tenant** (this tenant only).
2. **Certificates & secrets** → **New client secret** → save the value
   into `ENTRA_PROVISIONER_CLIENT_SECRET` in the env-file.
3. **API permissions** → **Microsoft Graph** → **Application permissions**:
   - `Application.ReadWrite.All`
   - `Organization.ReadWrite.All`
   - **Grant admin consent** for the tenant.
4. Copy the **Application (client) ID** → `ENTRA_PROVISIONER_CLIENT_ID`.
5. Copy the **Directory (tenant) ID** → `ENTRA_TENANT_ID`.

## Running

From a Windows host with the env-file in place:

```powershell
cd C:\path\to\CCE\infra\entra
.\apply-app-registration.ps1 -Environment prod
.\Configure-Branding.ps1     -Environment prod    # optional; P1/P2 only
```

Both write logs to `C:\ProgramData\CCE\logs\entra-*-prod-<utc-iso8601>.log`.

## Phase 02 → 04 sequence

| Phase | What happens |
|---|---|
| 02 | These scripts ship. Operator runs them once per tenant per env. |
| 03 | Frontend swaps OIDC config to point at the registered app. |
| 04 | Cutover runbook deletes Keycloak, flips `MIGRATE_*` keys, deletes infra/keycloak/. |

## Troubleshooting

- **`AAD_TenantThrottleLimit_<n>`** — Graph rate-limit on app PATCH. Wait 5 min and retry; the script is idempotent.
- **`Authorization_RequestDenied` on app PATCH** — provisioner missing `Application.ReadWrite.All` admin consent.
- **`Authorization_RequestDenied` on branding** — provisioner missing `Organization.ReadWrite.All` admin consent.
- **Branding doesn't render after script success** — Entra ID caches branding for ~1 hour; users may need to clear cookies.
```

- [ ] **Step 2: Update env-file examples**

For each of `.env.example`, `.env.local.example`, `.env.test.example`, `.env.preprod.example`, `.env.prod.example`, `.env.dr.example`, add the following block (adapt hostnames per env):

```bash
# ─── Identity (Entra ID — Sub-11) ───────────────────────────────────────────
# Tenant + provisioner app for infra/entra/apply-app-registration.ps1.
# (Provisioner app is separate from the runtime CCE app — split privilege.)
ENTRA_TENANT_ID=<set-me>                            # e.g. 11111111-2222-3333-4444-555555555555
ENTRA_PROVISIONER_CLIENT_ID=<set-me>
ENTRA_PROVISIONER_CLIENT_SECRET=<set-me>

# Runtime CCE app — consumed by the CCE backend at runtime via
# CCE.Infrastructure.Identity.EntraIdOptions.
ENTRA_CLIENT_ID=<set-me>                            # populated AFTER apply-app-registration.ps1 runs
ENTRA_CLIENT_SECRET=<set-me>
ENTRA_AUDIENCE=api://<runtime-app-client-id>
ENTRA_GRAPH_TENANT_ID=<set-me>                      # CCE's own tenant ID for user-create
ENTRA_GRAPH_TENANT_DOMAIN=cce.onmicrosoft.com       # or verified custom domain

# Hostname-to-redirect-URI mapping (consumed by apply-app-registration.ps1
# manifest-substitution step). One per env across both BFFs.
HOSTNAME_PORTAL_TEST=taqah-portal-test.example.com
HOSTNAME_PORTAL_PREPROD=taqah-portal-preprod.example.com
HOSTNAME_PORTAL_PROD=taqah-portal.example.com
HOSTNAME_PORTAL_DR=taqah-portal-dr.example.com
HOSTNAME_CMS_TEST=taqah-cms-test.example.com
HOSTNAME_CMS_PREPROD=taqah-cms-preprod.example.com
HOSTNAME_CMS_PROD=taqah-cms.example.com
HOSTNAME_CMS_DR=taqah-cms-dr.example.com
```

(For `.env.local.example`, the hostnames can all be `localhost:5101` / `localhost:5102` since redirect URIs are env-scoped — the manifest already has localhost entries hard-coded.)

The legacy `KEYCLOAK_*` block stays in each file for now with a comment:

```bash
# ─── Identity (Keycloak — DEPRECATED, removed in Phase 04 cutover) ──────────
# These keys are still consumed through Phase 03 by the custom BFF middleware.
# Phase 04 deletes the BFF cluster + infra/keycloak/ + these env-keys.
```

- [ ] **Step 3: Sanity-check env-file syntax**

```bash
for f in /Users/m/CCE/.env*.example; do
  echo "=== $f ==="
  bash -n <(grep -v '^#' "$f" | grep -v '^$' | sed 's/^/export /; s/=\(.*\)$/="\1"/' )
done
```

Expected: no syntax errors.

- [ ] **Step 4: Commit**

```bash
git add infra/entra/README.md .env.example .env.local.example .env.test.example .env.preprod.example .env.prod.example .env.dr.example
git commit -m "$(cat <<'EOF'
docs(infra/entra): operator README + env-file ENTRA_* + HOSTNAME_* keys

Adds the operator-facing runbook for both PowerShell scripts. Documents
the prerequisite split-privilege provisioner app (separate from the
runtime CCE app), the per-tenant one-time setup, and Phase 02→04
sequencing.

Updates all 6 env-file templates with:
- ENTRA_TENANT_ID / ENTRA_PROVISIONER_CLIENT_ID/SECRET (provisioner app)
- ENTRA_CLIENT_ID/SECRET / ENTRA_AUDIENCE (runtime CCE app — populated
  AFTER apply-app-registration.ps1 first run)
- ENTRA_GRAPH_TENANT_ID / ENTRA_GRAPH_TENANT_DOMAIN (Graph user-create)
- HOSTNAME_PORTAL_*/CMS_* per env (consumed by manifest substitution)

Legacy KEYCLOAK_* keys retained with DEPRECATED comment — Phase 03
custom-BFF middleware still reads them; Phase 04 cutover deletes the
BFF cluster + the keys.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2.4: ADR-0058 (Entra ID multi-tenant + Graph writes) + ADR-0059 (app roles)

**Files:**
- Create: `docs/adr/0058-entra-id-multi-tenant-graph-writes.md`
- Create: `docs/adr/0059-app-roles-vs-security-groups.md`

ADRs follow the existing repo style (look at `0054-iis-reverse-proxy-on-windows-server.md` for shape — 5 sections: Context, Decision, Rationale, Consequences, Status).

- [ ] **Step 1: Create ADR-0058**

```markdown
# ADR-0058 — Entra ID multi-tenant with Graph writes

**Date:** 2026-05-04
**Status:** Accepted (supersedes ADR-0055)
**Decision-makers:** CCE Architecture, Sub-11 brainstorm 2026-05-04

## Context

Sub-1 through Sub-9 ran the entire CCE platform on Keycloak as the IdP,
synced from on-prem AD via Keycloak's LDAP user-federation provider
(see ADR-0055). Sub-11 retires Keycloak and adopts Microsoft Entra ID
in its place.

The choice of *which* Entra ID surface — single-tenant, multi-tenant
(`AzureADMultipleOrgs`), or B2C (`PersonalMicrosoftAccount`) — drives
how partner organizations sign in and whether CCE has authority to
create users.

## Decision

CCE uses **multi-tenant Entra ID** (`signInAudience: AzureADMultipleOrgs`)
with the CCE backend writing to its own home tenant via **Microsoft
Graph SDK** (app-only `User.ReadWrite.All` permission).

- **Tenant model:** multi-tenant. CCE's home tenant is `cce.onmicrosoft.com`
  (or verified custom domain). Partner organization users sign in with
  their own Entra ID tenant accounts; their tokens carry an `iss` claim
  matching `https://login.microsoftonline.com/<their-tenant>/v2.0`.
- **Issuer validation:** custom `EntraIdIssuerValidator` accepts any
  issuer matching `https://login.microsoftonline.com/<tenant>/v2.0` —
  no per-tenant allow-list.
- **User write path:** registration calls Graph `POST /v1.0/users` from
  the runtime CCE app (which has `User.ReadWrite.All` admin-consented
  application permission). CCE only ever writes users into its own
  home tenant — partner-tenant users are created by their own admins,
  not by CCE.

## Rationale

- **Multi-tenant unblocks partner orgs.** Single-tenant would force
  every partner user to be a guest invitation in CCE's tenant — a
  manual per-user gate that doesn't scale.
- **B2C was ruled out.** B2C is for consumer accounts; CCE serves
  organizations (employees, government partners). B2C also can't sync
  from on-prem AD via Entra ID Connect, which is decisive here
  because cce.local is the existing source of truth.
- **Graph writes (option b) chosen over Graph reads-only (option a).**
  CCE has self-service registration. Without write access, every new
  user requires an out-of-band IT ticket. The trade-off — CCE backend
  holds a Graph client secret with `User.ReadWrite.All` — is mitigated
  by storing the secret only on prod hosts in env-files locked down via
  ICACLS.
- **Entra ID Connect handles cce.local sync.** Existing on-prem AD
  identities flow into CCE's home tenant automatically; no Keycloak
  LDAP-federation surface needed (ADR-0055 is now superseded).

## Consequences

- **Phase 04 cutover deletes infra/keycloak/** and the
  `KeycloakLdapFederationTests` (3 tests) and the Testcontainers.Keycloak
  reference.
- **Outbound internet access required** from prod hosts to
  `login.microsoftonline.com` and `graph.microsoft.com`. This is a
  network-policy change documented in `docs/runbooks/entra-id-cutover.md`.
- **Multi-tenant means CCE can't enforce per-tenant policies.** A
  partner tenant could disable an account via their own admin; CCE's
  `EntraIdUserResolver` keeps stale objectIds linked but the next
  Graph call returns 404 / 401, surfacing the cleanup naturally.
- **Custom branding only renders for CCE-tenant users.** Partner-tenant
  users see their own home-tenant sign-in page. ADR documents this
  is by Microsoft design, not a configuration choice.

## Status

Accepted. **Supersedes ADR-0055** (`ad-federation-via-keycloak-ldap`).
```

- [ ] **Step 2: Create ADR-0059**

```markdown
# ADR-0059 — App roles vs security groups for permission mapping

**Date:** 2026-05-04
**Status:** Accepted
**Decision-makers:** CCE Architecture, Sub-11 brainstorm 2026-05-04

## Context

CCE permissions historically mapped from Keycloak realm roles
(`SuperAdmin`, `ContentEditor`, `ExpertReviewer`, etc.) into the
`permissions.yaml` matrix consumed by `RoleToPermissionClaimsTransformer`.
Sub-11 swaps Keycloak for Entra ID, and Entra ID offers two competing
mechanisms for the same shape: **app roles** (declared in the app
registration's `appRoles[]`) and **security groups** (membership-based,
emitted in the `groups` claim).

## Decision

CCE uses **app roles** (`appRoles[]` in the app registration) to drive
permissions. The token's `roles` claim is the authoritative input to
`RoleToPermissionClaimsTransformer`. Security groups are NOT consumed
by the platform.

The 5 app roles are: `cce-admin`, `cce-editor`, `cce-reviewer`,
`cce-expert`, `cce-user`.

## Rationale

- **App roles are app-scoped, groups are tenant-scoped.** The
  `cce-editor` role only means anything inside CCE; a tenant-scoped
  `Marketing` group might mean unrelated things in other apps. App
  roles keep authorization semantics local to CCE.
- **Multi-tenant compatibility.** Group membership in a partner tenant
  doesn't propagate to CCE's app — but app-role assignments do (admin
  consent + admin assignment in the partner's tenant). Groups would
  require per-tenant claim-mapping policies.
- **Token size.** Groups emit ALL group memberships into the `groups`
  claim. For a user in a typical Microsoft Entra tenant, that's
  20–200 GUIDs. The token bloats past Entra ID's 6 KB soft cap and
  spills into a separate `_claim_names` reference, which the BFF then
  has to dereference via Graph. App roles emit only the 1–5 assigned
  values into `roles` — no spillover.
- **Existing transformer adapts cleanly.** Phase 03 updates
  `RoleToPermissionClaimsTransformer` to consume `roles` (was `groups`)
  with role names matching `appRoles[].value` (was `SuperAdmin`-style
  names). The mapping table in `permissions.yaml` rewrites accordingly.

## Consequences

- **Operators must assign app roles in the Azure portal** (or via
  PowerShell / Microsoft Graph Explorer) per user. Group-based
  assignment via dynamic membership rules is NOT supported.
- **Phase 03 rewrites `permissions.yaml`** with `cce-admin`-style role
  names instead of `SuperAdmin`-style. The matrix shape is unchanged.
- **Phase 00's `RoleClaimMappingTests` (2 tests, deferred from Phase
  00 to Phase 03)** land in Phase 03 alongside the transformer
  rewrite.

## Status

Accepted.
```

- [ ] **Step 3: Sanity-check ADR markdown renders**

```bash
ls /Users/m/CCE/docs/adr/0058* /Users/m/CCE/docs/adr/0059*
```

Expected: both files exist.

- [ ] **Step 4: Commit**

```bash
git add docs/adr/0058-entra-id-multi-tenant-graph-writes.md docs/adr/0059-app-roles-vs-security-groups.md
git commit -m "$(cat <<'EOF'
docs(adr): 0058 (Entra ID multi-tenant + Graph writes) + 0059 (app roles)

ADR-0058 documents the architectural pivot from Keycloak LDAP
federation (ADR-0055, now superseded) to multi-tenant Entra ID with
the CCE backend writing to its own home tenant via Microsoft Graph SDK
(app-only User.ReadWrite.All). Covers tenant-model trade-offs (vs
single-tenant, vs B2C), the issuer-validation strategy, and the
Phase 04 cutover sequence.

ADR-0059 records the choice of app roles over security groups for
permission mapping. Rationale: app-scoped vs tenant-scoped semantics,
multi-tenant compatibility (groups don't propagate from partner
tenants but app-role assignments do), and token-size headroom (groups
emit all 20–200 memberships per user, app roles emit only the 1–5
assigned).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2.5: ADR-0060 (Conditional Access for MFA) + supersede ADR-0055

**Files:**
- Create: `docs/adr/0060-conditional-access-for-mfa.md`
- Modify: `docs/adr/0055-ad-federation-via-keycloak-ldap.md` (status → Superseded)

- [ ] **Step 1: Create ADR-0060**

```markdown
# ADR-0060 — Conditional Access for MFA enforcement

**Date:** 2026-05-04
**Status:** Accepted
**Decision-makers:** CCE Architecture, Sub-11 brainstorm 2026-05-04

## Context

Pre-Sub-11, MFA was an aspirational policy — Keycloak supported
TOTP/WebAuthn flows but CCE never wired them up. Sub-11 brings MFA
into scope, and Entra ID offers two enforcement points: **app-side**
(CCE checks for an `amr` claim and rejects tokens without `mfa`) and
**Entra ID-side** (Conditional Access policy refuses to issue a token
unless MFA was satisfied during sign-in).

## Decision

MFA is enforced via **Entra ID Conditional Access** policies, not by
the CCE app. The CCE backend stays MFA-agnostic — it does not inspect
the `amr` claim and does not refuse tokens that lack `mfa`.

The Conditional Access policy targets the CCE app registration with
the rule: "All users → require multi-factor authentication".

## Rationale

- **Conditional Access is the canonical Entra ID surface for MFA.**
  Microsoft's documentation, support, and tooling all assume CA. App-
  side MFA enforcement is a niche pattern reserved for legacy IdPs
  that don't support CA.
- **CA covers all tokens uniformly.** A user signing into CCE's web
  portal, admin CMS, or any other CCE-app-scoped surface gets the
  same MFA requirement. App-side enforcement would require wiring
  the same check into every entry point.
- **CCE stays simple.** No `RequireMfaPolicy`, no `amr` claim
  inspection, no fallback flow for users on devices that can't do
  MFA. The CA policy is the single source of truth — operators
  modify it without redeploying CCE.
- **Operationally proven.** Sub-10c (production infra) already
  assumes CA for MFA in the IIS/443 layer.

## Consequences

- **Operators MUST configure a CA policy** scoped to the CCE app
  before users can sign in. Without one, MFA is effectively disabled.
  `docs/runbooks/entra-id-cutover.md` (Phase 04) includes the
  step-by-step.
- **The CCE backend has no MFA-related test surface** — testing MFA
  enforcement happens against a real Entra ID tenant in a manual
  security review, not in CI.
- **Partner-tenant users** are subject to **their own tenant's** CA
  policies, not CCE's. CCE has no authority to enforce MFA on partner
  tenants. This is a multi-tenant trade-off accepted at brainstorm
  (decision 4).
- **Service accounts** (CCE-internal apps that call the CCE API
  app-to-app) bypass MFA via the client-credentials flow. The CA
  policy targets users only, not service principals. This is correct
  Entra ID semantics.

## Status

Accepted.
```

- [ ] **Step 2: Modify ADR-0055**

Open `docs/adr/0055-ad-federation-via-keycloak-ldap.md` and add a banner at the top of the file (after the title, before the original Date line):

```markdown
> **STATUS: Superseded by [ADR-0058](./0058-entra-id-multi-tenant-graph-writes.md) on 2026-05-04.**
> Sub-11 retires Keycloak and replaces it with Entra ID multi-tenant + Entra ID Connect from on-prem AD. The decisions in this ADR no longer apply; see ADR-0058 for the current architecture. The Phase 04 cutover (Sub-11) deletes the Keycloak surface (`infra/keycloak/`, `KeycloakLdapFederationTests`, `Testcontainers.Keycloak`).
```

Also update the **Status** line in the body from `Accepted` (or whatever it was) to `Superseded`.

- [ ] **Step 3: Sanity-check ADR cross-links**

```bash
grep -l "0058\|0055" /Users/m/CCE/docs/adr/0055-* /Users/m/CCE/docs/adr/0058-*
```

Expected: both files contain cross-references.

- [ ] **Step 4: Commit**

```bash
git add docs/adr/0060-conditional-access-for-mfa.md docs/adr/0055-ad-federation-via-keycloak-ldap.md
git commit -m "$(cat <<'EOF'
docs(adr): 0060 (Conditional Access for MFA) + 0055 marked Superseded

ADR-0060 records the choice to enforce MFA at the Entra ID Conditional
Access layer (operator-configured policy targeting the CCE app)
instead of in the CCE backend. Rationale: CA is canonical Entra ID
MFA surface; covers all tokens uniformly; CCE stays simple (no amr
claim inspection); proven in Sub-10c production deploys.

Documents the multi-tenant trade-off: partner-tenant users are bound
by their own tenant's CA policies, not CCE's — CCE has no authority
to enforce MFA on partner tenants. Service accounts bypass via
client-credentials flow (correct Entra ID semantics).

ADR-0055 (Keycloak LDAP federation) gets a banner at the top marking
it as Superseded by ADR-0058 on 2026-05-04. Phase 04 cutover deletes
the underlying surface (infra/keycloak/, KeycloakLdapFederationTests,
Testcontainers.Keycloak).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 02 close-out

After Task 2.5 commits cleanly:

- [ ] **Sanity-check the artifact tree:**
  ```bash
  ls /Users/m/CCE/infra/entra/
  ls /Users/m/CCE/docs/adr/0055* /Users/m/CCE/docs/adr/0058* /Users/m/CCE/docs/adr/0059* /Users/m/CCE/docs/adr/0060*
  ```
  Expected: `app-registration-manifest.json` + `apply-app-registration.ps1` + `Configure-Branding.ps1` + `branding/` + `README.md` under `infra/entra/`. 4 ADR files (0055 modified, 0058+0059+0060 created).

- [ ] **Backend test suites unchanged** — no source changes in Phase 02:
  ```bash
  cd /Users/m/CCE/backend && dotnet build --nologo --verbosity minimal | tail -5
  ```
  Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. Test counts unchanged from Phase 01: Domain 290 / Application 439 / Architecture 12 / Infrastructure 87.

- [ ] **Update master plan + Phase 02 doc** to mark Phase 02 DONE with actual deliverables.

- [ ] **Hand off to Phase 03.** Phase 03 is the frontend-side wiring: 6 frontend files (`auth.guard`, `register.page`, `sign-in-cta`, 2× `auth.interceptor`, `correlation-id.interceptor`), 3 e2e spec files, runtime config files per env. Plus the backend-side `RoleToPermissionClaimsTransformer` rewrite (the 2 deferred `RoleClaimMappingTests` from Phase 00 land here). Plan file: `phase-03-frontend-changes.md` (to be written just-in-time before execution).

**Phase 02 done when:**
- 5 commits land on `main`, each green.
- `infra/entra/app-registration-manifest.json` ships with 5 app roles + 10 redirect URIs (templated).
- `infra/entra/apply-app-registration.ps1` + `infra/entra/Configure-Branding.ps1` ship with PSv7 + Microsoft.Graph PS module dependencies.
- `infra/entra/branding/` placeholders ship; PNGs gitignored.
- `infra/entra/README.md` operator runbook ships.
- All 6 env-file examples updated with `ENTRA_*` + `HOSTNAME_*` blocks; legacy `KEYCLOAK_*` keys retained with DEPRECATED comment.
- ADR-0058 (multi-tenant + Graph writes) + ADR-0059 (app roles) + ADR-0060 (Conditional Access for MFA) committed.
- ADR-0055 status flipped to Superseded with cross-link to ADR-0058.
- Backend test counts unchanged: Domain 290 / Application 439 / Architecture 12 / Infrastructure 87.
- **No production cutover.** Cutover happens in Phase 04.

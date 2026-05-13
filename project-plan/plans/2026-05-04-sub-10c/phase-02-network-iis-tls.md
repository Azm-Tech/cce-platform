# Phase 02 — Network: IIS reverse proxy + TLS + DNS (Sub-10c)

> Parent: [`../2026-05-04-sub-10c.md`](../2026-05-04-sub-10c.md) · Spec: [`../../specs/2026-05-04-sub-10c-design.md`](../../specs/2026-05-04-sub-10c-design.md) §Network — IIS reverse proxy + TLS.

**Phase goal:** Make the 4 backend services reachable at IDD's production hostnames over HTTPS via IIS sites that terminate TLS and ARR-proxy to the localhost container ports. Operator-callable provisioning script + cert/DNS checklist + env-aware smoke probes against IDD hostnames + ADR-0054.

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 01 closed (4 commits land on `main`; HEAD at `5932cd1` or later).
- `.env.<env>.example` files include `IIS_CERT_*` + `IIS_HOSTNAMES` keys (added in Phase 00 Task 0.3).
- 4 backend containers from Sub-10b bind to localhost ports 4200/4201/5001/5002.
- Backend baseline: 439 Application + 69 Infrastructure tests passing (1 skipped).

---

## Task 2.1: `Install-ARRPrereqs.ps1` — idempotent feature install

**Files:**
- Create: `infra/iis/Install-ARRPrereqs.ps1` — installs Web-Server (IIS) + URL Rewrite Module + Application Request Routing (ARR) features. Idempotent.

**Why first:** every later task in Phase 02 depends on these features being installed. Operators may run this manually as a one-time host-setup step; `Configure-IISSites.ps1` calls it as a fall-through (skips if already installed).

**The 3 prerequisites:**
1. **IIS** — installed via Windows feature `Web-Server` + sub-features (`Web-Common-Http`, `Web-Performance`, `Web-Health`, `Web-Security`, `Web-App-Dev`, `Web-Mgmt-Tools`).
2. **URL Rewrite 2.1** — Microsoft's free module; downloaded as MSI from `https://download.microsoft.com/download/1/2/8/128E2E22-C1B9-44A4-BE2A-5859ED1D4592/rewrite_amd64_en-US.msi`.
3. **Application Request Routing 3.0** — Microsoft's free reverse-proxy module; depends on URL Rewrite. MSI from `https://download.microsoft.com/download/E/9/8/E9849D6A-020E-47E4-9FD0-A023E99B54EB/requestRouter_amd64.msi`.

**Final state of `infra/iis/Install-ARRPrereqs.ps1`:**

```powershell
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
```

- [ ] **Step 1:** Create `infra/iis/` directory:
  ```bash
  mkdir -p /Users/m/CCE/infra/iis
  ```

- [ ] **Step 2:** Create `infra/iis/Install-ARRPrereqs.ps1` with the contents above.

- [ ] **Step 3:** Commit:
  ```bash
  git -C /Users/m/CCE add infra/iis/Install-ARRPrereqs.ps1
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(infra): Install-ARRPrereqs.ps1 — IIS + URL Rewrite + ARR

  Idempotent installer for the 3 IIS reverse-proxy prereqs:
   - IIS Web-Server feature + sub-features (Common-Http, Health,
     Security, App-Dev, Mgmt-Tools, ASP.NET 4.5).
   - URL Rewrite 2.1 module (MSI download + silent install).
   - ARR 3.0 module (MSI download + silent install).

  Detects already-installed via registry version keys; skips
  re-install. Enables the global ARR proxy at the end. Run as
  Administrator on the Windows Server host during one-time setup.

  Sub-10c Phase 02 Task 2.1.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 2.2: `web.config.template` — ARR rewrite rules

**Files:**
- Create: `infra/iis/web.config.template` — parameterized site-level web.config with the rewrite rule + security headers.

**Why a template, not 4 hand-coded files:** all 4 sites have identical reverse-proxy semantics; only the backend port differs. A single template file with `{BACKEND_PORT}` substitution keeps them in sync.

**Final state of `infra/iis/web.config.template`:**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="ProxyToBackend" stopProcessing="true">
          <match url="^(.*)$" />
          <action type="Rewrite" url="http://localhost:{BACKEND_PORT}/{R:1}" />
          <serverVariables>
            <set name="HTTP_X_FORWARDED_HOST"  value="{HTTP_HOST}" />
            <set name="HTTP_X_FORWARDED_PROTO" value="https" />
            <set name="HTTP_X_FORWARDED_FOR"   value="{REMOTE_ADDR}" />
          </serverVariables>
        </rule>
      </rules>
    </rewrite>
    <httpProtocol>
      <customHeaders>
        <remove name="X-Powered-By" />
        <add name="Strict-Transport-Security" value="max-age=31536000; includeSubDomains" />
        <add name="X-Content-Type-Options" value="nosniff" />
        <add name="X-Frame-Options" value="SAMEORIGIN" />
        <add name="Referrer-Policy" value="strict-origin-when-cross-origin" />
      </customHeaders>
    </httpProtocol>
    <!-- SSE-friendly: long concurrent request limit; no response buffering. -->
    <serverRuntime appConcurrentRequestLimit="5000" />
    <security>
      <requestFiltering>
        <requestLimits maxAllowedContentLength="52428800" />
      </requestFiltering>
    </security>
  </system.webServer>
</configuration>
```

**Note on `{BACKEND_PORT}`:** literal placeholder. `Configure-IISSites.ps1` (Task 2.3) does `[string]::Replace('{BACKEND_PORT}', '5001')` per site. The 4 mappings:

| Site | `{BACKEND_PORT}` |
|---|---|
| `CCE-ext` | `4200` |
| `CCE-admin-Panel` | `4201` |
| `api.CCE` | `5001` |
| `Api.CCE-admin-Panel` | `5002` |

**Note on `serverVariables`:** ARR requires the Allowed Server Variables list to include `HTTP_X_FORWARDED_*` for the `<set>` rules to work. `Configure-IISSites.ps1` adds them via `Add-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter 'system.webServer/rewrite/allowedServerVariables' -Name '.' -Value @{ name = 'HTTP_X_FORWARDED_HOST' }` (etc.) at host-setup time.

- [ ] **Step 1:** Create `infra/iis/web.config.template` with the contents above.

- [ ] **Step 2:** Verify XML parses (use `xmllint` if available; otherwise visual inspection):
  ```bash
  python3 -c "import xml.etree.ElementTree as ET; ET.parse('/Users/m/CCE/infra/iis/web.config.template')" && echo "XML OK"
  ```
  Expected: `XML OK`.

- [ ] **Step 3:** Commit:
  ```bash
  git -C /Users/m/CCE add infra/iis/web.config.template
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(infra): web.config.template for IIS reverse-proxy sites

  Single template parameterized by {BACKEND_PORT}; Configure-IISSites.ps1
  substitutes per site. Rewrites every URL to http://localhost:{port}/...
  with X-Forwarded-Host/Proto/For server variables. Adds standard
  security headers (HSTS, X-Content-Type-Options, X-Frame-Options,
  Referrer-Policy) and removes X-Powered-By. SSE-friendly:
  appConcurrentRequestLimit=5000 + max content 50MB.

  Sub-10c Phase 02 Task 2.2.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 2.3: `Configure-IISSites.ps1` — idempotent site provisioning

**Files:**
- Create: `infra/iis/Configure-IISSites.ps1` — provisions the 4 IIS sites with hostname bindings, port 443, named cert, ARR `web.config` (rendered from template), allowed server variables. Idempotent.

**Final state of `infra/iis/Configure-IISSites.ps1`:**

```powershell
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
```

- [ ] **Step 1:** Create `infra/iis/Configure-IISSites.ps1` with the contents above.

- [ ] **Step 2:** Commit:
  ```bash
  git -C /Users/m/CCE add infra/iis/Configure-IISSites.ps1
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(infra): Configure-IISSites.ps1 — 4 IIS reverse-proxy sites

  Idempotent provisioner. Reads IIS_CERT_* + IIS_HOSTNAMES from the
  env-file; ensures IIS + URL Rewrite + ARR are installed (calls
  Install-ARRPrereqs.ps1 if missing); imports PFX cert if path
  given (or uses thumbprint from env-file); adds the 3 X-Forwarded-*
  server variables to ARR's allowedServerVariables; creates/updates
  the 4 sites with HTTPS bindings + SNI + cert + ARR web.config
  rendered from web.config.template.

  Per-site rollback on failure (Remove-Website). Site dirs at
  C:\\inetpub\\cce\\<site>. iisreset at end.

  Sub-10c Phase 02 Task 2.3.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 2.4: `infra/dns-tls/README.md` — operator checklist

**Files:**
- Create: `infra/dns-tls/README.md` — operator-facing checklist for cert procurement (3 paths) + DNS provisioning + validation.

**Why a checklist not automation:** cert procurement and DNS provisioning are operator + ops-team tasks; the IDD environment may dictate AD CS auto-enrollment, manual cert import, or `win-acme`. Sub-10c documents the three paths and the validation steps; doesn't pick one.

**Final state of `infra/dns-tls/README.md`:**

```markdown
# CCE infra/dns-tls/

Operator checklist for cert + DNS provisioning. Sub-10c uses operator-procured certs (not auto-provisioned); pick one of three paths per IDD/site requirements.

## Hostnames (per IDD v1.2)

| Environment | External | Admin Panel | API External | API Admin |
|---|---|---|---|---|
| `prod` | `CCE-ext` | `CCE-admin-Panel` | `api.CCE` | `Api.CCE-admin-Panel` |
| `preprod` | `cce-ext-preprod` | `cce-admin-panel-preprod` | `api.cce-preprod` | `api.cce-admin-panel-preprod` |
| `test` | `cce-ext-test` | `cce-admin-panel-test` | `api.cce-test` | `api.cce-admin-panel-test` |
| `dr` | `cce-ext-dr` | `cce-admin-panel-dr` | `api.cce-dr` | `api.cce-admin-panel-dr` |

(IDD's "port 433" treated as 443 per session memory.)

## Cert procurement — pick one path

### Path A: AD CS auto-enrollment (recommended for AD-joined hosts)

If the host is AD-domain-joined and the AD environment has Active Directory Certificate Services with a Web Server template:

1. Verify auto-enrollment policy applied: `gpresult /h gpreport.html` → review.
2. Trigger immediate enrollment:
   ```powershell
   certutil -pulse
   ```
3. Verify cert appeared in the personal store:
   ```powershell
   Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -match 'CN=' + $env:COMPUTERNAME }
   ```
4. Copy the thumbprint into `.env.<env>` as `IIS_CERT_THUMBPRINT=<thumbprint>`.

### Path B: `win-acme` (Let's Encrypt for internet-facing hosts)

If the host is reachable from the public internet (or has a DNS provider supporting DNS-01 challenge):

1. Download `win-acme` from <https://www.win-acme.com/>; extract to `C:\Tools\win-acme\`.
2. Run interactively to add a cert:
   ```powershell
   cd C:\Tools\win-acme
   .\wacs.exe
   ```
   Pick "Create new certificate" → "Manual input" → enter the 4 hostnames comma-separated → choose IIS as the validation method (HTTP-01) or DNS-01 if non-public.
3. `win-acme` installs the cert + creates a scheduled task for auto-renewal every 60 days.
4. Find the thumbprint:
   ```powershell
   Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -match 'CCE' }
   ```
5. Copy into `.env.<env>` as `IIS_CERT_THUMBPRINT=<thumbprint>`.

### Path C: Manual cert import (purchased cert or internal CA)

For purchased commercial certs or internal CA-issued certs delivered as PFX:

1. Place the PFX file at `C:\ProgramData\CCE\certs\cce-<env>.pfx`.
2. Lock down ACLs:
   ```powershell
   icacls C:\ProgramData\CCE\certs\cce-<env>.pfx /inheritance:r `
       /grant:r "Administrators:F" "<deploy-user>:R"
   ```
3. In `.env.<env>` set:
   ```
   IIS_CERT_PFX_PATH=C:\ProgramData\CCE\certs\cce-<env>.pfx
   IIS_CERT_PFX_PASSWORD=<the-pfx-password>
   ```
4. `Configure-IISSites.ps1` imports the PFX into the cert store on the next run.

## DNS provisioning

Sub-10c does NOT automate DNS. The operator + DNS-admin team provision A records (or AAAA for IPv6) pointing the 4 hostnames at the host IP (or the load balancer's VIP if one fronts the host).

Recommended TTL during steady-state: 300 sec (5 min). Lower TTLs (e.g., 60 sec) before a planned DR failover so propagation is fast.

## Validation

After cert + DNS are in place + `Configure-IISSites.ps1` has run:

```powershell
# 1. From the host: cert is bound to the IIS site.
Get-WebBinding | Format-Table Name, Protocol, BindingInformation, CertificateHash

# 2. From the host: TLS handshake works against each hostname.
foreach ($h in @('CCE-ext','CCE-admin-Panel','api.CCE','Api.CCE-admin-Panel')) {
    Test-NetConnection -ComputerName $h -Port 443
}

# 3. From a CLIENT (outside the host): DNS resolves + TLS terminates.
Resolve-DnsName CCE-ext
Invoke-WebRequest https://CCE-ext/ -UseBasicParsing | Select-Object StatusCode, Headers
```

## Cert renewal

| Cert source | Renewal mechanism |
|---|---|
| AD CS auto-enrollment | Automatic via group policy; no operator action |
| `win-acme` (Let's Encrypt) | Scheduled task auto-renews 60 days before expiry |
| Manual import | Operator must replace the PFX + re-run `Configure-IISSites.ps1` |

For manual renewal, see [`secret-rotation.md`](../../docs/runbooks/secret-rotation.md) — the `IIS_CERT_PFX_PASSWORD` rotation procedure also covers cert renewal mechanics.

## See also

- [`Configure-IISSites.ps1`](../iis/Configure-IISSites.ps1) — provisions the IIS sites with these certs.
- [ADR-0054 — IIS reverse proxy](../../docs/adr/0054-iis-reverse-proxy-on-windows-server.md)
- [Sub-10c design spec §Network](../../specs/2026-05-04-sub-10c-design.md#network--iis-reverse-proxy--tls)
```

- [ ] **Step 1:** Create `infra/dns-tls/` directory:
  ```bash
  mkdir -p /Users/m/CCE/infra/dns-tls
  ```

- [ ] **Step 2:** Create `infra/dns-tls/README.md` with the contents above.

- [ ] **Step 3:** Commit:
  ```bash
  git -C /Users/m/CCE add infra/dns-tls/README.md
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "docs(infra): cert + DNS operator checklist

  Three documented cert-procurement paths: AD CS auto-enrollment
  (AD-joined hosts), win-acme + Let's Encrypt (internet-facing
  hosts), manual PFX import (purchased certs / internal CA).
  Sub-10c doesn't automate cert provisioning — operator picks per
  IDD environment.

  Hostname table per env (test/preprod/prod/dr). DNS provisioning
  guidance + low-TTL recommendation for DR failover. Validation
  procedure (Get-WebBinding, Test-NetConnection, Resolve-DnsName,
  Invoke-WebRequest). Renewal mechanism per cert source.

  Sub-10c Phase 02 Task 2.4.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 2.5: `smoke.ps1` env-aware HTTPS probes + ADR-0054

**Files:**
- Modify: `deploy/smoke.ps1` — add `-Environment <env>` parameter; when passed, probes the env's IDD hostnames over HTTPS instead of localhost. `-AllowSelfSignedCert` switch for test/preprod with internal CAs.
- Create: `docs/adr/0054-iis-reverse-proxy-on-windows-server.md`.

**Final state of the modified `deploy/smoke.ps1`:**

```powershell
#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10b/10c smoke-probe script.

.DESCRIPTION
    Polls the 4 endpoints. Two modes:
     - localhost mode (Sub-10b default): probes http://localhost:5001..4201
       directly against the backend containers. Used during local
       smoke testing + Sub-10b deploy-smoke.yml.
     - env-aware mode (Sub-10c, when -Environment passed): probes the
       env-specific IDD hostnames over HTTPS via IIS reverse proxy.

    Each endpoint: 30 attempts × 2 sec backoff = 60-sec window.

.PARAMETER Timeout
    Per-endpoint timeout in seconds. Default 60.

.PARAMETER Quiet
    Suppress per-attempt output; only print the final result.

.PARAMETER Environment
    When passed (test|preprod|prod|dr), probes that env's IDD hostnames
    over HTTPS instead of localhost. Reads IIS_HOSTNAMES from the
    corresponding env-file.

.PARAMETER EnvFile
    Override env-file path. Default: C:\ProgramData\CCE\.env.<Environment>.

.PARAMETER AllowSelfSignedCert
    Skip TLS cert validation. For test/preprod with internal CAs not
    in the dev machine's trust store. Never use in prod.

.EXAMPLE
    .\deploy\smoke.ps1                                    # localhost mode
    .\deploy\smoke.ps1 -Environment prod                  # IDD hostnames over HTTPS
    .\deploy\smoke.ps1 -Environment test -AllowSelfSignedCert
#>
[CmdletBinding()]
param(
    [int]$Timeout = 60,
    [switch]$Quiet,
    [ValidateSet('','test','preprod','prod','dr')]
    [string]$Environment = '',
    [string]$EnvFile,
    [switch]$AllowSelfSignedCert
)

$ErrorActionPreference = 'Stop'

# Build probe set.
if ([string]::IsNullOrWhiteSpace($Environment)) {
    # Sub-10b localhost mode.
    $probes = @(
        @{ Name = 'api-external/health'; Url = 'http://localhost:5001/health'; Type = 'health' },
        @{ Name = 'api-internal/health'; Url = 'http://localhost:5002/health'; Type = 'health' },
        @{ Name = 'web-portal/';         Url = 'http://localhost:4200/';        Type = 'html'   },
        @{ Name = 'admin-cms/';          Url = 'http://localhost:4201/';        Type = 'html'   }
    )
} else {
    # Sub-10c env-aware mode.
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
    if (-not $envMap.ContainsKey('IIS_HOSTNAMES') -or [string]::IsNullOrWhiteSpace($envMap['IIS_HOSTNAMES'])) {
        Write-Error "IIS_HOSTNAMES not set in $EnvFile."
        exit 1
    }
    $hosts = $envMap['IIS_HOSTNAMES'].Split(',') | ForEach-Object { $_.Trim() }
    if ($hosts.Count -ne 4) {
        Write-Error "IIS_HOSTNAMES must list exactly 4 hostnames (got $($hosts.Count))."
        exit 1
    }
    $probes = @(
        @{ Name = 'web-portal';   Url = "https://$($hosts[0])/";       Type = 'html'   },
        @{ Name = 'admin-cms';    Url = "https://$($hosts[1])/";       Type = 'html'   },
        @{ Name = 'api-external'; Url = "https://$($hosts[2])/health"; Type = 'health' },
        @{ Name = 'api-internal'; Url = "https://$($hosts[3])/health"; Type = 'health' }
    )
}

$attempts = [Math]::Max(1, [int]($Timeout / 2))
$failed = @()

foreach ($probe in $probes) {
    if (-not $Quiet) { Write-Host "Probing $($probe.Name)..." -NoNewline }
    $ok = $false
    for ($i = 1; $i -le $attempts; $i++) {
        try {
            $params = @{
                Uri = $probe.Url
                UseBasicParsing = $true
                TimeoutSec = 5
                ErrorAction = 'Stop'
            }
            if ($AllowSelfSignedCert) { $params.SkipCertificateCheck = $true }
            $response = Invoke-WebRequest @params
            if ($probe.Type -eq 'health') {
                $body = $response.Content | ConvertFrom-Json
                if ($body.status -eq 'Healthy') { $ok = $true; break }
            } else {
                if ($response.Content -match '<html') { $ok = $true; break }
            }
        } catch {
            # swallow — keep retrying
        }
        Start-Sleep -Seconds 2
    }
    if ($ok) {
        if (-not $Quiet) { Write-Host " OK" }
    } else {
        if (-not $Quiet) { Write-Host " FAIL" }
        $failed += $probe.Name
    }
}

if ($failed.Count -gt 0) {
    Write-Error "Smoke probe FAILED: $($failed -join ', ')"
    exit 1
}
Write-Host "All 4 probes PASSED."
exit 0
```

**Final state of `docs/adr/0054-iis-reverse-proxy-on-windows-server.md`:**

```markdown
# ADR-0054 — IIS reverse proxy on Windows Server

**Status:** Accepted
**Date:** 2026-05-04
**Deciders:** Sub-10c brainstorm (kilany113@gmail.com)
**Sub-project:** [Sub-10c — Production infra + DR](../../specs/2026-05-04-sub-10c-design.md)

## Context

Sub-10b ships 4 backend containers bound to localhost ports (4200/4201/5001/5002). Sub-10c needs them reachable at IDD's production hostnames (`CCE-ext`, `CCE-admin-Panel`, `api.CCE`, `Api.CCE-admin-Panel`) over HTTPS. The host is Windows Server 2022 per IDD; AD-domain-joined.

## Decision

Use IIS as a reverse proxy on the host. Each IDD hostname maps to an IIS site that terminates TLS (port 443) and reverse-proxies via Application Request Routing (ARR) + URL Rewrite to the corresponding localhost backend port.

**Considered alternatives:**

- **Reverse-proxy container (Caddy / Traefik)**: rejected. Requires a new container in the stack with auto-cert lifecycle (ACME). On a Windows Server target with AD CS already present, IIS's native cert-store integration is simpler. Caddy/Traefik shine on Linux; on Windows they fight against the platform.
- **nginx as a host-level Windows service**: rejected. nginx-on-Windows is second-class (no graceful reload, bespoke service-management); cert-renewal scripting is bespoke vs IIS's native cert-binding APIs.

**Why IIS won:**

- Native to Windows Server 2022; the host already has IIS or can install it via standard `Install-WindowsFeature`.
- AD CS auto-enrollment delivers certs into the LocalMachine cert store; IIS binds them by thumbprint with zero copy operations.
- ARR is Microsoft's documented reverse-proxy module; battle-tested in enterprise environments; preserves `X-Forwarded-*` headers via `serverVariables` rules.
- Ops/network admins on a Windows shop already know IIS; no new tooling to learn.
- Single config surface — no Docker reverse-proxy layer in addition to the app containers.

## Implementation

`infra/iis/Install-ARRPrereqs.ps1` (one-time host setup) installs IIS + URL Rewrite 2.1 + ARR 3.0; enables the global ARR proxy.

`infra/iis/Configure-IISSites.ps1` (per-deploy or one-time) reads `IIS_CERT_*` + `IIS_HOSTNAMES` from `.env.<env>`; provisions the 4 sites with HTTPS bindings + SNI + named cert + ARR rewrite rules from `web.config.template`.

`infra/iis/web.config.template` is the parameterized rewrite config — `{BACKEND_PORT}` substituted per site. Adds standard security headers (HSTS, X-Content-Type-Options, X-Frame-Options, Referrer-Policy). SSE-friendly: `appConcurrentRequestLimit=5000`, no response buffering.

## Consequences

**Positive:**
- Reuses existing IIS infrastructure on the AD-joined Windows host.
- Cert procurement uses one of three documented paths (AD CS, win-acme, manual import) — operator chooses per IDD.
- ARR's `X-Forwarded-*` headers preserve the real client IP for backend logs.
- Per-site rollback in `Configure-IISSites.ps1` prevents partial-config breakage.
- Per-host TLS termination simplifies the Sub-10b containers — they stay HTTP-only on localhost.

**Negative / accepted:**
- Adds a configuration surface (IIS + ARR) on top of Docker. Two layers to debug when things break.
- ARR has known SSE quirks; mitigated by `appConcurrentRequestLimit=5000` and by avoiding `precondition` rules that buffer.
- Operators not familiar with IIS need to learn `Get-Website`, `New-WebBinding`, `Set-WebConfigurationProperty`. The runbook + scripts cover the common operations.
- IIS sites + cert bindings are mutable host state outside of compose; `Configure-IISSites.ps1` is the only supported way to manage them.

**Out of scope (Sub-10c+):**
- Cert auto-provisioning (operator-driven; three paths documented).
- DNS auto-provisioning.
- WAF / IP allowlisting (can layer ARR's IP/domain restrictions if IDD requires).
- Multi-host LB (Sub-10c targets one host per env; an LB in front would terminate TLS at the LB).

## References

- [Sub-10c design spec §Network](../../specs/2026-05-04-sub-10c-design.md#network--iis-reverse-proxy--tls)
- [`infra/dns-tls/README.md`](../../infra/dns-tls/README.md) — cert + DNS operator checklist.
- [Microsoft URL Rewrite docs](https://learn.microsoft.com/en-us/iis/extensions/url-rewrite-module/using-url-rewrite-module)
- [Microsoft ARR docs](https://learn.microsoft.com/en-us/iis/extensions/installing-application-request-routing-arr/)
- ADR-0055 — AD federation via Keycloak LDAP (Sub-10c)
```

- [ ] **Step 1:** Modify `deploy/smoke.ps1` per the diffs above (full replacement is cleanest given the param block expansion).

- [ ] **Step 2:** Create `docs/adr/0054-iis-reverse-proxy-on-windows-server.md` with the contents above.

- [ ] **Step 3:** Commit:
  ```bash
  git -C /Users/m/CCE add deploy/smoke.ps1 docs/adr/0054-iis-reverse-proxy-on-windows-server.md
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(deploy): smoke.ps1 env-aware HTTPS mode + ADR-0054

  smoke.ps1 gains -Environment <test|preprod|prod|dr> + -AllowSelfSignedCert.
  When -Environment passed, reads IIS_HOSTNAMES from the env-file
  and probes the 4 IDD hostnames over HTTPS instead of localhost.
  Default behaviour (no -Environment) preserves Sub-10b localhost
  probing for backward compat.

  ADR-0054 documents the IIS reverse-proxy decision: native to
  Windows Server, AD CS-friendly cert lifecycle, ARR battle-
  tested. Considered + rejected: Caddy/Traefik containers, nginx
  on Windows. Implementation overview + scope boundaries.

  Sub-10c Phase 02 Task 2.5.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Phase 02 close-out

After Task 2.5 commits cleanly:

- [ ] **Run the full check:**
  ```bash
  cd /Users/m/CCE/backend && dotnet build && \
    dotnet test tests/CCE.Application.Tests/ tests/CCE.Infrastructure.Tests/ --nologo
  ```
  Expected: backend build clean; 439 Application + 69 Infrastructure tests passing (1 skipped). Phase 02 adds zero C# tests — IIS provisioning is verified by deploy-smoke.yml on Windows runners.

- [ ] **Verify CI green** on push: `ci.yml` workflows pass; `deploy-smoke.yml` is unchanged in Phase 02.

- [ ] **Hand off to Phase 03.** Phase 03 writes the Ola Hallengren install + scheduled-tasks XML + `Restore-FromBackup.ps1` + ADR-0056 + backup-restore runbook. Plan file: `phase-03-backup-and-restore.md` (to be written when ready).

**Phase 02 done when:**
- 5 commits land on `main`, each green.
- `infra/iis/Install-ARRPrereqs.ps1` is operator-callable; idempotent.
- `infra/iis/Configure-IISSites.ps1` provisions 4 IIS sites + bindings + cert.
- `infra/iis/web.config.template` is the parameterized rewrite config.
- `infra/dns-tls/README.md` documents cert + DNS procurement.
- `deploy/smoke.ps1` supports env-aware HTTPS probes against IDD hostnames.
- ADR-0054 committed.
- Test counts unchanged: 439 Application + 69 Infrastructure (1 skipped). Frontend 502.

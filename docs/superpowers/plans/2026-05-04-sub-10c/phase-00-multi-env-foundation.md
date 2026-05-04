# Phase 00 — Multi-env foundation (Sub-10c)

> Parent: [`../2026-05-04-sub-10c.md`](../2026-05-04-sub-10c.md) · Spec: [`../../specs/2026-05-04-sub-10c-design.md`](../../specs/2026-05-04-sub-10c-design.md) §Multi-env config — per-env files, §Secret rotation runbook, §Canary integrity check.

**Phase goal:** Generalize Sub-10b's `.env.prod`-singleton model to N environments. After Phase 00, `deploy.ps1 -Environment <env>` resolves to `.env.<env>` on the host; per-env `deploy-history-${env}.tsv` audit trails are separate; `validate-env.ps1` rejects placeholder values + known-leaked patterns; operators have a documented secret-rotation procedure and an env-promotion helper. **No identity / network / backup / IIS work in Phase 00** — those phases all consume the env-file abstraction this phase builds.

**Tasks:** 6
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Sub-10b closed (tag `deploy-v1.0.0` exists; HEAD at `1fec122` (Sub-10c spec commit) or later).
- `deploy/deploy.ps1`, `deploy/rollback.ps1`, `deploy/smoke.ps1` exist from Sub-10b.
- `.env.prod.example` exists at repo root from Sub-10b Phase 01.
- Existing `.gitignore` has `.env*` deny + `!.env.{example,local.example,prod.example}` allow.
- Backend baseline: 439 Application + 66 Infrastructure tests passing (1 skipped).

---

## Task 0.1: Add `-Environment` parameter to `deploy.ps1`

**Files:**
- Modify: `deploy/deploy.ps1` — add `-Environment <env>` parameter (default `prod` for backward compat); resolve `$EnvFile` from environment when caller doesn't override; prefix log messages with `[<env>]`.

**Why first:** every later task in Phase 00 + every later phase consumes the env name. Locking the parameter shape before adding `validate-env.ps1` etc. keeps the surface stable.

**Backward compat contract:**
- Existing call sites (`.\deploy\deploy.ps1` no args) continue to work — `-Environment` defaults to `prod`, which resolves to `C:\ProgramData\CCE\.env.prod` (the existing default). Zero behavior change for Sub-10b operators.
- Existing call sites with `-EnvFile <path>` continue to work — when `-EnvFile` is explicitly passed, it overrides the env-derived default.
- New call site `-Environment test` resolves to `C:\ProgramData\CCE\.env.test`.

**Final state of the param block + env resolution:**

```powershell
[CmdletBinding()]
param(
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile,
    [switch]$Recursive
)

$ErrorActionPreference = 'Stop'

# Default env-file derived from -Environment when -EnvFile not explicitly passed.
if (-not $EnvFile -or $EnvFile -eq '') {
    $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$composeBase   = Join-Path $repoRoot 'docker-compose.prod.yml'
$composeStrict = Join-Path $repoRoot 'docker-compose.prod.deploy.yml'

# Logs directory + timestamped log file
$logDir = 'C:\ProgramData\CCE\logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir ("deploy-{0}-{1:yyyyMMddTHHmmssZ}.log" -f $Environment, (Get-Date).ToUniversalTime())

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $line = "[$ts] [$Level] [$Environment] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}
```

The `-Recursive` switch is added now (default `false`) so Phase 04 can wire it in without touching the param block again. It's a no-op in Phase 00.

**Per-env history file:**

```powershell
# Replace any reference to:
#   $historyFile = 'C:\ProgramData\CCE\deploy-history.tsv'
# With:
$historyFile = "C:\ProgramData\CCE\deploy-history-$Environment.tsv"
```

(Step 9 already references `$historyFile`; this is one line.)

**Update Abort's rollback hint** to point at the right history file:

```powershell
function Abort {
    param([string]$Message, [int]$ExitCode = 1, [switch]$ShowRollback)
    Write-Log -Level 'ERROR' -Message $Message
    if ($ShowRollback) {
        Write-Log -Level 'ERROR' -Message "Rollback command: .\deploy\rollback.ps1 -ToTag <previous-tag> -Environment $Environment"
        Write-Log -Level 'ERROR' -Message "Find previous tag in: C:\ProgramData\CCE\deploy-history-$Environment.tsv"
    }
    exit $ExitCode
}
```

**Headline-step update** (Step 1 message): keep "Step 1/10: Resolving env-file path." — the `[<env>]` prefix from `Write-Log` already conveys env.

- [ ] **Step 1:** Read `deploy/deploy.ps1` to confirm the current param block + history file references match what we expect to modify.
  ```bash
  grep -n "param(\|EnvFile\|historyFile\|deploy-history" /Users/m/CCE/deploy/deploy.ps1
  ```
  Expected: `param(` at ~22, `EnvFile` at ~23 + ~56-58, `historyFile` at ~125, `deploy-history` at ~49.

- [ ] **Step 2:** Apply the param block + log file update + history file update + Abort message update per the diffs above. Three localized edits, all under 10 lines each.

- [ ] **Step 3:** Verify backward compat — running with no args still defaults to prod:
  ```bash
  pwsh -NoProfile -Command "& { \$null = [System.Management.Automation.Language.Parser]::ParseFile('/Users/m/CCE/deploy/deploy.ps1', [ref]\$null, [ref]\$err); if (\$err) { \$err | Out-Host; exit 1 } else { Write-Host 'parses OK' } }"
  ```
  Expected: `parses OK`. Skip if pwsh not installed locally — CI's deploy-smoke.yml is the syntax check.

- [ ] **Step 4:** Commit:
  ```bash
  git -C /Users/m/CCE add deploy/deploy.ps1
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(deploy): -Environment switch + per-env history file

  deploy.ps1 gains -Environment <test|preprod|prod|dr> (default prod
  for Sub-10b backward compat) + -Recursive (no-op stub for Phase 04
  auto-rollback recursion guard). Default env-file resolves to
  C:\\ProgramData\\CCE\\.env.<env>; existing -EnvFile <path> overrides.
  Log file + deploy-history.tsv become per-env: deploy-<env>-<UTC>.log
  and deploy-history-<env>.tsv. Log messages prefixed with [<env>].

  Sub-10c Phase 00 Task 0.1.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.2: Mirror `-Environment` switch in `rollback.ps1`

**Files:**
- Modify: `deploy/rollback.ps1` — add `-Environment <env>` parameter; pass through to nested `deploy.ps1` invocation; resolve env-file from env when not overridden; per-env history file write.

**Why this task:** `rollback.ps1` invokes `deploy.ps1` recursively. Both must agree on the env name; otherwise the rollback log lands in the wrong history file.

**Final state of the param block:**

```powershell
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$ToTag,
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile,
    [switch]$SkipMigrator
)

$ErrorActionPreference = 'Stop'

if (-not $EnvFile -or $EnvFile -eq '') {
    $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
}
```

**Per-env rollback log file:**

```powershell
$logFile = Join-Path $logDir ("rollback-{0}-{1:yyyyMMddTHHmmssZ}.log" -f $Environment, (Get-Date).ToUniversalTime())
```

**Per-env rollback history file** (Step "Append rollback row to deploy-history.tsv"):

```powershell
$historyFile = "C:\ProgramData\CCE\deploy-history-$Environment.tsv"
```

**Pass `-Environment` to nested `deploy.ps1`:**

```powershell
& pwsh -NoProfile -File $deployScript -EnvFile $resolvedEnvFile -Environment $Environment -Recursive
```

The `-Recursive` flag is the Phase 04 hook; passing it now makes Phase 04 a no-change in this script.

- [ ] **Step 1:** Read `deploy/rollback.ps1`:
  ```bash
  grep -n "param(\|EnvFile\|historyFile\|deploy-history\|deployScript" /Users/m/CCE/deploy/rollback.ps1
  ```

- [ ] **Step 2:** Apply the four updates above (param block, log file, history file, deploy.ps1 invocation). Each is localized.

- [ ] **Step 3:** Quick parse check (skip if no pwsh):
  ```bash
  pwsh -NoProfile -Command "& { \$null = [System.Management.Automation.Language.Parser]::ParseFile('/Users/m/CCE/deploy/rollback.ps1', [ref]\$null, [ref]\$err); if (\$err) { \$err | Out-Host; exit 1 } else { Write-Host 'parses OK' } }"
  ```

- [ ] **Step 4:** Commit:
  ```bash
  git -C /Users/m/CCE add deploy/rollback.ps1
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(deploy): -Environment switch in rollback.ps1

  Mirrors deploy.ps1 changes from Task 0.1. Default 'prod' preserves
  backward compat. Per-env rollback log + history file. Passes
  -Environment + -Recursive to the nested deploy.ps1 invocation.

  Sub-10c Phase 00 Task 0.2.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.3: `.env.{test,preprod,prod,dr}.example` files + `.gitignore` allow-list

**Files:**
- Modify: `.env.prod.example` — add new keys (`AUTO_ROLLBACK`, `LDAP_*`, `IIS_CERT_*`, `BACKUP_UNC_*`, `SENTRY_RELEASE`, `SENTRY_ENVIRONMENT`).
- Create: `.env.test.example`.
- Create: `.env.preprod.example`.
- Create: `.env.dr.example`.
- Modify: `.gitignore` — extend `!.env.*.example` allow-list.

**`.env.prod.example` final state** (full replacement; keys are a superset of Sub-10b's):

```bash
# CCE production environment file (Sub-10c)
# ===========================================================================
# Copy this file to C:\ProgramData\CCE\.env.prod on the deployment host,
# fill in real values, then lock it down:
#   icacls C:\ProgramData\CCE\.env.prod /inheritance:r /grant:r "Administrators:R" "<deploy-user>:R"
#
# deploy.ps1 -Environment prod reads this file. validate-env.ps1
# rejects placeholder values (anything matching <set-me>, <github-org-or-user>,
# Strong!Passw0rd, etc.) at deploy time.

# ─── Image refs (drives rollback via image-tag pinning) ─────────────────────
CCE_REGISTRY_OWNER=<github-org-or-user>     # e.g. moenergy-cce
CCE_IMAGE_TAG=app-v1.0.0                    # release tag, full SHA, or "latest"

# ─── Database ───────────────────────────────────────────────────────────────
INFRA_SQL=Server=host.docker.internal,1433;Database=CCE;User Id=cce_app;Password=<set-me>;TrustServerCertificate=True;Encrypt=True

# ─── Cache / queue ──────────────────────────────────────────────────────────
INFRA_REDIS=host.docker.internal:6379

# ─── Identity (Keycloak) ────────────────────────────────────────────────────
KEYCLOAK_AUTHORITY=https://api.CCE/auth/realms/cce
KEYCLOAK_AUDIENCE=cce-api
KEYCLOAK_REQUIRE_HTTPS=true                 # 10c flips to true behind LB

# ─── Identity (Keycloak admin API; for apply-realm.ps1) ─────────────────────
KEYCLOAK_ADMIN_USER=<set-me>
KEYCLOAK_ADMIN_PASSWORD=<set-me>

# ─── Identity (Active Directory federation; consumed by Keycloak) ──────────
LDAP_HOST=ad.cce.local
LDAP_PORT=636                               # 389 = LDAP, 636 = LDAPS
LDAP_BIND_DN=CN=cce-keycloak-svc,CN=Users,DC=cce,DC=local
LDAP_BIND_PASSWORD=<set-me>
LDAP_USERS_DN=OU=Users,DC=cce,DC=local
LDAP_GROUPS_DN=OU=Groups,DC=cce,DC=local

# ─── IIS reverse proxy (Phase 02 consumes) ──────────────────────────────────
# Either thumbprint of imported cert OR PFX path + password, not both.
IIS_CERT_THUMBPRINT=                        # e.g. A1B2C3D4...
IIS_CERT_PFX_PATH=                          # e.g. C:\ProgramData\CCE\certs\cce-prod.pfx
IIS_CERT_PFX_PASSWORD=                      # required if IIS_CERT_PFX_PATH set
IIS_HOSTNAMES=CCE-ext,CCE-admin-Panel,api.CCE,Api.CCE-admin-Panel

# ─── Assistant (Anthropic LLM) ──────────────────────────────────────────────
ASSISTANT_PROVIDER=anthropic                # or "stub" to disable
ANTHROPIC_API_KEY=<set-me>                  # required when provider=anthropic

# ─── Observability ──────────────────────────────────────────────────────────
LOG_LEVEL=Information
SENTRY_DSN=                                 # leave blank to disable
SENTRY_ENVIRONMENT=production               # MUST match -Environment
SENTRY_RELEASE=app-v1.0.0                   # SHOULD match CCE_IMAGE_TAG

# ─── Migration behaviour ────────────────────────────────────────────────────
MIGRATE_ON_DEPLOY=true                      # set false to skip migrator service
MIGRATE_SEED_REFERENCE=true                 # seed reference data alongside migrate

# ─── Auto-rollback (Phase 04 consumes) ──────────────────────────────────────
AUTO_ROLLBACK=false                         # prod: false; test/preprod: true

# ─── Backup automation (Phase 03 consumes) ─────────────────────────────────
BACKUP_UNC_HOST=backup-server.cce.local
BACKUP_UNC_SHARE=cce-backups
BACKUP_UNC_USER=<set-me>
BACKUP_UNC_PASSWORD=<set-me>
BACKUP_RETENTION_DAYS_FULL=7
BACKUP_RETENTION_DAYS_DIFF=7
BACKUP_RETENTION_HOURS_LOG=24

# ─── Optional: ghcr.io auth (otherwise rely on existing docker login session) ─
CCE_GHCR_TOKEN=                             # PAT with read:packages

# ─── Required-key catalogue (deploy.ps1 + validate-env.ps1 enforce) ────────
#   CCE_REGISTRY_OWNER, CCE_IMAGE_TAG, INFRA_SQL, INFRA_REDIS,
#   KEYCLOAK_AUTHORITY, KEYCLOAK_AUDIENCE, KEYCLOAK_ADMIN_USER,
#   KEYCLOAK_ADMIN_PASSWORD, LDAP_HOST, LDAP_BIND_DN, LDAP_BIND_PASSWORD,
#   LDAP_USERS_DN, LDAP_GROUPS_DN, SENTRY_ENVIRONMENT, BACKUP_UNC_HOST,
#   BACKUP_UNC_SHARE, BACKUP_UNC_USER, BACKUP_UNC_PASSWORD.
#   ANTHROPIC_API_KEY required only when ASSISTANT_PROVIDER=anthropic.
#   IIS_CERT_THUMBPRINT or IIS_CERT_PFX_PATH+PASSWORD must be set (one of).
```

**`.env.test.example`** — same shape; per-env knobs:

```bash
# CCE test environment file (Sub-10c)
# Copy to C:\ProgramData\CCE\.env.test on the test host.

CCE_REGISTRY_OWNER=<github-org-or-user>
CCE_IMAGE_TAG=latest

INFRA_SQL=Server=host.docker.internal,1433;Database=CCE_test;User Id=cce_app;Password=<set-me>;TrustServerCertificate=True;Encrypt=True
INFRA_REDIS=host.docker.internal:6379

KEYCLOAK_AUTHORITY=https://api.CCE-test/auth/realms/cce
KEYCLOAK_AUDIENCE=cce-api
KEYCLOAK_REQUIRE_HTTPS=false                # test allows http for fast iteration

KEYCLOAK_ADMIN_USER=<set-me>
KEYCLOAK_ADMIN_PASSWORD=<set-me>

LDAP_HOST=ad.cce.local
LDAP_PORT=636
LDAP_BIND_DN=CN=cce-keycloak-svc,CN=Users,DC=cce,DC=local
LDAP_BIND_PASSWORD=<set-me>
LDAP_USERS_DN=OU=Users-Test,DC=cce,DC=local      # separate test OU if available; else prod OU
LDAP_GROUPS_DN=OU=Groups,DC=cce,DC=local

IIS_CERT_THUMBPRINT=
IIS_CERT_PFX_PATH=
IIS_CERT_PFX_PASSWORD=
IIS_HOSTNAMES=cce-ext-test,cce-admin-panel-test,api.cce-test,api.cce-admin-panel-test

ASSISTANT_PROVIDER=stub                     # test uses stub by default
ANTHROPIC_API_KEY=

LOG_LEVEL=Debug                             # verbose for test
SENTRY_DSN=
SENTRY_ENVIRONMENT=test
SENTRY_RELEASE=latest

MIGRATE_ON_DEPLOY=true
MIGRATE_SEED_REFERENCE=true

AUTO_ROLLBACK=true                          # test gets fast-feedback rollback

BACKUP_UNC_HOST=backup-server.cce.local
BACKUP_UNC_SHARE=cce-backups
BACKUP_UNC_USER=<set-me>
BACKUP_UNC_PASSWORD=<set-me>
BACKUP_RETENTION_DAYS_FULL=3                # shorter for test
BACKUP_RETENTION_DAYS_DIFF=3
BACKUP_RETENTION_HOURS_LOG=12

CCE_GHCR_TOKEN=
```

**`.env.preprod.example`** — between test and prod:

```bash
# CCE pre-production (release-candidate) environment file (Sub-10c)
# Copy to C:\ProgramData\CCE\.env.preprod on the preprod host.

CCE_REGISTRY_OWNER=<github-org-or-user>
CCE_IMAGE_TAG=app-v1.0.0-rc.1               # release-candidate tag

INFRA_SQL=Server=host.docker.internal,1433;Database=CCE_preprod;User Id=cce_app;Password=<set-me>;TrustServerCertificate=True;Encrypt=True
INFRA_REDIS=host.docker.internal:6379

KEYCLOAK_AUTHORITY=https://api.CCE-preprod/auth/realms/cce
KEYCLOAK_AUDIENCE=cce-api
KEYCLOAK_REQUIRE_HTTPS=true

KEYCLOAK_ADMIN_USER=<set-me>
KEYCLOAK_ADMIN_PASSWORD=<set-me>

LDAP_HOST=ad.cce.local
LDAP_PORT=636
LDAP_BIND_DN=CN=cce-keycloak-svc,CN=Users,DC=cce,DC=local
LDAP_BIND_PASSWORD=<set-me>
LDAP_USERS_DN=OU=Users,DC=cce,DC=local
LDAP_GROUPS_DN=OU=Groups,DC=cce,DC=local

IIS_CERT_THUMBPRINT=
IIS_CERT_PFX_PATH=
IIS_CERT_PFX_PASSWORD=
IIS_HOSTNAMES=cce-ext-preprod,cce-admin-panel-preprod,api.cce-preprod,api.cce-admin-panel-preprod

ASSISTANT_PROVIDER=anthropic                # preprod tests with real LLM
ANTHROPIC_API_KEY=<set-me>

LOG_LEVEL=Information
SENTRY_DSN=                                 # operator pastes preprod DSN
SENTRY_ENVIRONMENT=preprod
SENTRY_RELEASE=app-v1.0.0-rc.1

MIGRATE_ON_DEPLOY=true
MIGRATE_SEED_REFERENCE=true

AUTO_ROLLBACK=true                          # preprod opts in to fast rollback

BACKUP_UNC_HOST=backup-server.cce.local
BACKUP_UNC_SHARE=cce-backups
BACKUP_UNC_USER=<set-me>
BACKUP_UNC_PASSWORD=<set-me>
BACKUP_RETENTION_DAYS_FULL=7
BACKUP_RETENTION_DAYS_DIFF=7
BACKUP_RETENTION_HOURS_LOG=24

CCE_GHCR_TOKEN=
```

**`.env.dr.example`** — mirrors prod, distinct hostnames + DB:

```bash
# CCE disaster-recovery environment file (Sub-10c)
# Copy to C:\ProgramData\CCE\.env.dr on the DR host. DR host stays cold
# until promoted; see docs/runbooks/dr-promotion.md (Phase 05).

CCE_REGISTRY_OWNER=<github-org-or-user>
CCE_IMAGE_TAG=app-v1.0.0                    # mirrors prod's tag

INFRA_SQL=Server=host.docker.internal,1433;Database=CCE;User Id=cce_app;Password=<set-me>;TrustServerCertificate=True;Encrypt=True
INFRA_REDIS=host.docker.internal:6379

KEYCLOAK_AUTHORITY=https://api.CCE-dr/auth/realms/cce
KEYCLOAK_AUDIENCE=cce-api
KEYCLOAK_REQUIRE_HTTPS=true

KEYCLOAK_ADMIN_USER=<set-me>
KEYCLOAK_ADMIN_PASSWORD=<set-me>

LDAP_HOST=ad.cce.local
LDAP_PORT=636
LDAP_BIND_DN=CN=cce-keycloak-svc,CN=Users,DC=cce,DC=local
LDAP_BIND_PASSWORD=<set-me>
LDAP_USERS_DN=OU=Users,DC=cce,DC=local
LDAP_GROUPS_DN=OU=Groups,DC=cce,DC=local

IIS_CERT_THUMBPRINT=
IIS_CERT_PFX_PATH=
IIS_CERT_PFX_PASSWORD=
IIS_HOSTNAMES=cce-ext-dr,cce-admin-panel-dr,api.cce-dr,api.cce-admin-panel-dr

ASSISTANT_PROVIDER=anthropic
ANTHROPIC_API_KEY=<set-me>

LOG_LEVEL=Information
SENTRY_DSN=
SENTRY_ENVIRONMENT=dr
SENTRY_RELEASE=app-v1.0.0

MIGRATE_ON_DEPLOY=true
MIGRATE_SEED_REFERENCE=true

AUTO_ROLLBACK=false                         # DR is operator-driven only

BACKUP_UNC_HOST=backup-server.cce.local
BACKUP_UNC_SHARE=cce-backups
BACKUP_UNC_USER=<set-me>
BACKUP_UNC_PASSWORD=<set-me>
BACKUP_RETENTION_DAYS_FULL=7
BACKUP_RETENTION_DAYS_DIFF=7
BACKUP_RETENTION_HOURS_LOG=24

CCE_GHCR_TOKEN=
```

**`.gitignore` change** — extend the existing `!.env.prod.example` line:

Find the existing `.env*` block:
```
.env
.env.*
!.env.example
!.env.local.example
!.env.prod.example
```

Replace with:
```
.env
.env.*
!.env.example
!.env.local.example
!.env.test.example
!.env.preprod.example
!.env.prod.example
!.env.dr.example
```

- [ ] **Step 1:** Modify `.env.prod.example` to the contents above (full replacement; superset of Sub-10b's existing keys).

- [ ] **Step 2:** Create `.env.test.example` with the contents above.

- [ ] **Step 3:** Create `.env.preprod.example` with the contents above.

- [ ] **Step 4:** Create `.env.dr.example` with the contents above.

- [ ] **Step 5:** Update `.gitignore` per the diff above.

- [ ] **Step 6:** Verify all 4 files are tracked (none ignored):
  ```bash
  cd /Users/m/CCE && git add --dry-run .env.test.example .env.preprod.example .env.prod.example .env.dr.example
  ```
  Expected: 4 lines `add '.env.<env>.example'`.

- [ ] **Step 7:** Verify `.env.prod` (real file, not example) is still ignored:
  ```bash
  cd /Users/m/CCE && touch .env.prod && git check-ignore -v .env.prod ; rm .env.prod
  ```
  Expected: `.gitignore:7:.env.* .env.prod` (matched by `.env.*` deny). The negation rules don't apply because the filename doesn't match `*.example`.

- [ ] **Step 8:** Commit:
  ```bash
  git -C /Users/m/CCE add .env.prod.example .env.test.example .env.preprod.example .env.dr.example .gitignore
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "chore(env): 4 per-env example files + .gitignore allow-list

  Sub-10c per-env config foundation. .env.prod.example extended with
  AUTO_ROLLBACK, LDAP_*, IIS_CERT_*, BACKUP_UNC_*, SENTRY_RELEASE,
  SENTRY_ENVIRONMENT, KEYCLOAK_ADMIN_USER/PASSWORD. .env.test/preprod/dr
  examples committed as full siblings (different per-env knobs:
  database name, hostnames, AUTO_ROLLBACK default, log level, image
  tag stream). All 4 examples allowed via .gitignore !.env.<env>.example
  exceptions; real .env.<env> files stay ignored.

  Sub-10c Phase 00 Task 0.3.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.4: `deploy/validate-env.ps1` canary integrity check

**Files:**
- Create: `deploy/validate-env.ps1` — standalone-callable script that loads an env-file and checks for placeholder values + leaked-secret canaries + suspicious whitespace.
- Modify: `deploy/deploy.ps1` — Step 2 invokes `validate-env.ps1`; abort on non-zero.

**Why now:** Tasks 0.5 + 0.6 + future phases all depend on the validator existing. Canaries are the cheapest way to catch "operator forgot to fill in a placeholder" mistakes before running migrations.

**Final state of `deploy/validate-env.ps1`:**

```powershell
#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10c env-file canary integrity check.

.DESCRIPTION
    Loads an env-file and checks for:
      - Placeholder values still in place (<set-me>, etc.).
      - Known-leaked-secret canaries (AWS docs example keys, etc.).
      - Suspicious whitespace (trailing CR, BOM) that breaks env_file: parsing.
    Exits 0 on clean, non-zero with a precise message on any failure.
    Standalone-callable for ad-hoc verification.

.PARAMETER EnvFile
    Path to the env-file. Required.

.PARAMETER Environment
    Optional environment name; informational only (logged in messages).

.EXAMPLE
    .\deploy\validate-env.ps1 -EnvFile C:\ProgramData\CCE\.env.prod
    .\deploy\validate-env.ps1 -EnvFile .env.prod.example -Environment prod
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$EnvFile,
    [string]$Environment = ''
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $EnvFile)) {
    Write-Error "Env-file not found: $EnvFile"
    exit 1
}

# ─── Placeholder canaries (must NOT appear in real values) ────────────────
$placeholders = @(
    '<set-me>',
    '<github-org-or-user>',
    '<github-org>',
    'changeme',
    'CHANGEME',
    'Strong!Passw0rd',                # Sub-10b's smoke-test SQL password
    'example.com',
    'EXAMPLE_KEY',
    # Public canary credentials from cloud-vendor docs
    'AKIAIOSFODNN7EXAMPLE',           # AWS docs example access key
    'wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY',   # AWS docs secret key
    '00000000-0000-0000-0000-000000000000'        # Azure docs zero-GUID
)

$lines = Get-Content $EnvFile
$failures = New-Object System.Collections.Generic.List[string]

# Parse env-file into key→value map (skip comments + blank lines).
$envMap = @{}
$lineNumber = 0
foreach ($line in $lines) {
    $lineNumber++

    # Suspicious whitespace check.
    if ($line -match "`r$") {
        $failures.Add("Line ${lineNumber}: trailing CR detected (file uses CRLF; convert to LF)")
    }
    if ($lineNumber -eq 1 -and $line.StartsWith([char]0xFEFF)) {
        $failures.Add("Line 1: BOM detected at start of file (will break env_file: parsing)")
    }

    if ($line -match '^\s*#') { continue }
    if ($line -match '^\s*$') { continue }
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $key = $Matches[1]
        $value = $Matches[2].Trim() -replace '\s*#.*$',''   # strip inline comments
        $envMap[$key] = $value

        # Placeholder check (case-sensitive — we want to match doc placeholders exactly).
        foreach ($placeholder in $placeholders) {
            if ($value -match [regex]::Escape($placeholder)) {
                $failures.Add("Line ${lineNumber}: ${key}='${value}' contains placeholder '${placeholder}'")
                break
            }
        }
    }
}

# ─── Cross-key consistency checks ─────────────────────────────────────────
# SENTRY_ENVIRONMENT must match expected env name when -Environment passed.
if ($Environment -and $envMap.ContainsKey('SENTRY_ENVIRONMENT')) {
    $expected = if ($Environment -eq 'prod') { 'production' } else { $Environment }
    if ($envMap['SENTRY_ENVIRONMENT'] -ne $expected) {
        $failures.Add("SENTRY_ENVIRONMENT='$($envMap['SENTRY_ENVIRONMENT'])' but expected '$expected' (matches -Environment $Environment)")
    }
}

# AUTO_ROLLBACK must be 'true' or 'false' when set.
if ($envMap.ContainsKey('AUTO_ROLLBACK') -and -not [string]::IsNullOrWhiteSpace($envMap['AUTO_ROLLBACK'])) {
    if ($envMap['AUTO_ROLLBACK'] -notmatch '^(true|false)$') {
        $failures.Add("AUTO_ROLLBACK='$($envMap['AUTO_ROLLBACK'])' must be 'true' or 'false'")
    }
}

# ─── Report ───────────────────────────────────────────────────────────────
if ($failures.Count -gt 0) {
    Write-Host "validate-env.ps1: FAILED — $($failures.Count) issue(s)" -ForegroundColor Red
    foreach ($f in $failures) {
        Write-Host "  - $f" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host "validate-env.ps1: OK — $($envMap.Count) keys parsed, no canaries hit."
exit 0
```

**`deploy/deploy.ps1` Step 2 modification:**

Find the existing Step 2 (env-file validation):

```powershell
# ─── Step 2: Validate env-file ─────────────────────────────────────────────
Write-Log "Step 2/10: Validating required keys."
```

Insert immediately AFTER the `Write-Log "Step 2/10: ..."` line and BEFORE the existing `foreach ($line in Get-Content ...)`:

```powershell
# Sub-10c: canary integrity check (placeholder values, leaked-secret canaries, whitespace).
$validateScript = Join-Path $PSScriptRoot 'validate-env.ps1'
& pwsh -NoProfile -File $validateScript -EnvFile $resolvedEnvFile -Environment $Environment
if ($LASTEXITCODE -ne 0) { Abort "Env-file validation failed (canary check). See messages above." }
```

**Test surface:** Phase 00 doesn't add a unit test for `validate-env.ps1` — it's a PowerShell script and covered by:
- The existing `.env.prod.example` (committed) MUST trigger validation failures (placeholder values present); we verify this with a manual smoke step below.
- Phase 04's `deploy-smoke.yml` exercises the validator end-to-end with a synthetic env-file that has all canaries cleared.

- [ ] **Step 1:** Create `deploy/validate-env.ps1` with the contents above.

- [ ] **Step 2:** Modify `deploy/deploy.ps1` Step 2 to invoke the validator.

- [ ] **Step 3:** Smoke test the validator: it MUST reject the example file (placeholders present):
  ```bash
  pwsh -NoProfile -File /Users/m/CCE/deploy/validate-env.ps1 -EnvFile /Users/m/CCE/.env.prod.example -Environment prod 2>&1 | head -20
  echo "exit: $?"
  ```
  Expected: `validate-env.ps1: FAILED` + multiple `<set-me>` / `<github-org-or-user>` placeholder hits, exit code 1. Skip if no pwsh — it's also exercised in deploy-smoke.yml.

- [ ] **Step 4:** Smoke test the validator on a clean synthetic file:
  ```bash
  cat > /tmp/test.env <<'EOF'
  CCE_REGISTRY_OWNER=acme-cce
  CCE_IMAGE_TAG=app-v1.0.0
  INFRA_SQL=Server=db;Database=CCE;User Id=cce;Password=hunter2;
  AUTO_ROLLBACK=false
  SENTRY_ENVIRONMENT=production
  EOF
  pwsh -NoProfile -File /Users/m/CCE/deploy/validate-env.ps1 -EnvFile /tmp/test.env -Environment prod
  echo "exit: $?"
  rm /tmp/test.env
  ```
  Expected: `validate-env.ps1: OK — 5 keys parsed, no canaries hit.` + exit 0.

- [ ] **Step 5:** Smoke test cross-key consistency check:
  ```bash
  cat > /tmp/test.env <<'EOF'
  CCE_REGISTRY_OWNER=acme-cce
  CCE_IMAGE_TAG=app-v1.0.0
  INFRA_SQL=Server=db;Database=CCE;User Id=cce;Password=hunter2;
  AUTO_ROLLBACK=maybe
  SENTRY_ENVIRONMENT=test
  EOF
  pwsh -NoProfile -File /Users/m/CCE/deploy/validate-env.ps1 -EnvFile /tmp/test.env -Environment prod 2>&1 | head -10
  echo "exit: $?"
  rm /tmp/test.env
  ```
  Expected: failures: `AUTO_ROLLBACK='maybe' must be 'true' or 'false'` + `SENTRY_ENVIRONMENT='test' but expected 'production'`, exit 1.

- [ ] **Step 6:** Commit:
  ```bash
  git -C /Users/m/CCE add deploy/validate-env.ps1 deploy/deploy.ps1
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(deploy): validate-env.ps1 canary integrity check

  Standalone PowerShell script that loads an env-file and reports:
   - Placeholder values still in place (<set-me>, etc.).
   - Known-leaked-secret canaries (AWS docs example credentials,
     Azure zero-GUID, Sub-10b's Strong!Passw0rd smoke-test password).
   - Trailing CR / BOM that breaks docker compose env_file: parsing.
   - Cross-key consistency: AUTO_ROLLBACK is true/false; SENTRY_
     ENVIRONMENT matches -Environment.

  deploy.ps1 Step 2 invokes the validator and aborts on non-zero
  before any state-changing step.

  Sub-10c Phase 00 Task 0.4.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.5: `deploy/promote-env.ps1` env-promotion helper

**Files:**
- Create: `deploy/promote-env.ps1` — copies an env-file to a new env, replacing per-env values with the new env's expected values, leaving operator-required fields blank with `<set-me>` placeholders.

**Why this helper:** promoting test → preprod → prod is mostly mechanical (change DB name, change Sentry environment, change image tag, change log level), but operators forget steps. A script that does the mechanical edits and intentionally blanks out per-env secrets makes the procedure routine.

**Final state of `deploy/promote-env.ps1`:**

```powershell
#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10c env-promotion helper.

.DESCRIPTION
    Copies an env-file from one environment to another, rewriting per-env
    values (database name, Sentry environment, IIS hostnames, image tag
    stream, AUTO_ROLLBACK default, log level) to the destination env's
    conventions. Per-env SECRETS (passwords, DSNs, tokens) are intentionally
    blanked out — operator MUST fill them in for the destination env.

    Refuses to overwrite an existing destination file unless -Force is passed.

.PARAMETER FromEnv
    Source environment. One of test, preprod, prod, dr.

.PARAMETER ToEnv
    Destination environment. One of test, preprod, prod, dr.

.PARAMETER ImageTag
    New value for CCE_IMAGE_TAG in the destination. e.g. app-v1.0.0,
    sha-abc1234, latest.

.PARAMETER FromFile
    Source env-file path. Default: C:\ProgramData\CCE\.env.<FromEnv>.

.PARAMETER ToFile
    Destination env-file path. Default: C:\ProgramData\CCE\.env.<ToEnv>.

.PARAMETER Force
    Overwrite the destination if it exists.

.EXAMPLE
    # Promote a test deploy to preprod for QA
    .\deploy\promote-env.ps1 -FromEnv test -ToEnv preprod -ImageTag app-v1.0.0-rc.1

    # Promote a release candidate to prod
    .\deploy\promote-env.ps1 -FromEnv preprod -ToEnv prod -ImageTag app-v1.0.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidateSet('test','preprod','prod','dr')] [string]$FromEnv,
    [Parameter(Mandatory)] [ValidateSet('test','preprod','prod','dr')] [string]$ToEnv,
    [Parameter(Mandatory)] [string]$ImageTag,
    [string]$FromFile,
    [string]$ToFile,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

if ($FromEnv -eq $ToEnv) {
    Write-Error "FromEnv and ToEnv must differ."; exit 1
}

if (-not $FromFile) { $FromFile = "C:\ProgramData\CCE\.env.$FromEnv" }
if (-not $ToFile)   { $ToFile   = "C:\ProgramData\CCE\.env.$ToEnv" }

if (-not (Test-Path $FromFile)) {
    Write-Error "Source env-file not found: $FromFile"; exit 1
}
if ((Test-Path $ToFile) -and -not $Force) {
    Write-Error "Destination env-file exists: $ToFile (use -Force to overwrite)"; exit 1
}

# Per-env conventions (kept in lockstep with .env.<env>.example contents).
$envConfig = @{
    'test'    = @{
        DbSuffix       = '_test'
        SentryEnv      = 'test'
        Hostnames      = 'cce-ext-test,cce-admin-panel-test,api.cce-test,api.cce-admin-panel-test'
        AutoRollback   = 'true'
        LogLevel       = 'Debug'
        RequireHttps   = 'false'
        AssistantProv  = 'stub'
    }
    'preprod' = @{
        DbSuffix       = '_preprod'
        SentryEnv      = 'preprod'
        Hostnames      = 'cce-ext-preprod,cce-admin-panel-preprod,api.cce-preprod,api.cce-admin-panel-preprod'
        AutoRollback   = 'true'
        LogLevel       = 'Information'
        RequireHttps   = 'true'
        AssistantProv  = 'anthropic'
    }
    'prod'    = @{
        DbSuffix       = ''
        SentryEnv      = 'production'
        Hostnames      = 'CCE-ext,CCE-admin-Panel,api.CCE,Api.CCE-admin-Panel'
        AutoRollback   = 'false'
        LogLevel       = 'Information'
        RequireHttps   = 'true'
        AssistantProv  = 'anthropic'
    }
    'dr'      = @{
        DbSuffix       = ''
        SentryEnv      = 'dr'
        Hostnames      = 'cce-ext-dr,cce-admin-panel-dr,api.cce-dr,api.cce-admin-panel-dr'
        AutoRollback   = 'false'
        LogLevel       = 'Information'
        RequireHttps   = 'true'
        AssistantProv  = 'anthropic'
    }
}

# Per-env SECRETS that must always be re-filled by the operator on promotion.
# (Re-blanking these is the security feature — an operator who runs
#  `promote-env -ToEnv prod` should NOT accidentally inherit preprod's secrets.)
$secretKeys = @(
    'INFRA_SQL',                  # contains password
    'KEYCLOAK_ADMIN_PASSWORD',
    'LDAP_BIND_PASSWORD',
    'ANTHROPIC_API_KEY',
    'SENTRY_DSN',
    'BACKUP_UNC_PASSWORD',
    'IIS_CERT_PFX_PASSWORD',
    'CCE_GHCR_TOKEN'
)

$toCfg = $envConfig[$ToEnv]
$lines = Get-Content $FromFile

# Mutate.
$newLines = New-Object System.Collections.Generic.List[string]
$newLines.Add("# CCE $ToEnv environment file (Sub-10c)")
$newLines.Add("# Generated by promote-env.ps1 from .env.$FromEnv on $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')")
$newLines.Add("# Operator MUST fill in <set-me> values before deploy.")
$newLines.Add("")

foreach ($line in $lines) {
    if ($line -match '^\s*#') { $newLines.Add($line); continue }
    if ($line -match '^\s*$') { $newLines.Add($line); continue }

    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $key = $Matches[1]
        $rest = $Matches[2]

        # Re-blank secrets.
        if ($secretKeys -contains $key) {
            $newLines.Add("$key=<set-me>")
            continue
        }

        # Per-env knobs.
        switch ($key) {
            'CCE_IMAGE_TAG'                 { $newLines.Add("$key=$ImageTag");                continue }
            'INFRA_SQL'                     { $newLines.Add("$key=<set-me>");                 continue }   # contains password
            'IIS_HOSTNAMES'                 { $newLines.Add("$key=$($toCfg.Hostnames)");      continue }
            'KEYCLOAK_REQUIRE_HTTPS'        { $newLines.Add("$key=$($toCfg.RequireHttps)");   continue }
            'ASSISTANT_PROVIDER'            { $newLines.Add("$key=$($toCfg.AssistantProv)");  continue }
            'LOG_LEVEL'                     { $newLines.Add("$key=$($toCfg.LogLevel)");       continue }
            'SENTRY_ENVIRONMENT'            { $newLines.Add("$key=$($toCfg.SentryEnv)");      continue }
            'SENTRY_RELEASE'                { $newLines.Add("$key=$ImageTag");                continue }
            'AUTO_ROLLBACK'                 { $newLines.Add("$key=$($toCfg.AutoRollback)");   continue }
            'KEYCLOAK_AUTHORITY' {
                # https://api.CCE-<env>/auth/realms/cce
                $hostname = if ($ToEnv -eq 'prod') { 'api.CCE' } else { "api.cce-$ToEnv" }
                $newLines.Add("$key=https://$hostname/auth/realms/cce")
                continue
            }
        }

        # Default: keep as-is.
        $newLines.Add($line)
    } else {
        $newLines.Add($line)
    }
}

# Atomic write.
$tempFile = "$ToFile.tmp"
$newLines | Set-Content -Path $tempFile -Encoding utf8
Move-Item -Path $tempFile -Destination $ToFile -Force

Write-Host "Wrote $ToFile (promoted from .env.$FromEnv with CCE_IMAGE_TAG=$ImageTag)."
Write-Host ""
Write-Host "REMAINING OPERATOR STEPS:" -ForegroundColor Yellow
Write-Host "  1. Fill in <set-me> values in $ToFile (secrets, IIS_CERT_*)." -ForegroundColor Yellow
Write-Host "  2. Run validate-env.ps1 to confirm no placeholders remain:" -ForegroundColor Yellow
Write-Host "       .\deploy\validate-env.ps1 -EnvFile $ToFile -Environment $ToEnv" -ForegroundColor Yellow
Write-Host "  3. Lock down ACLs:" -ForegroundColor Yellow
Write-Host "       icacls $ToFile /inheritance:r /grant:r 'Administrators:R' '<deploy-user>:R'" -ForegroundColor Yellow
Write-Host "  4. Deploy:  .\deploy\deploy.ps1 -Environment $ToEnv" -ForegroundColor Yellow
exit 0
```

- [ ] **Step 1:** Create `deploy/promote-env.ps1` with the contents above.

- [ ] **Step 2:** Smoke test on a synthetic source file (skip if no pwsh):
  ```bash
  cp /Users/m/CCE/.env.preprod.example /tmp/.env.preprod
  pwsh -NoProfile -File /Users/m/CCE/deploy/promote-env.ps1 \
      -FromEnv preprod -ToEnv prod -ImageTag app-v1.0.0 \
      -FromFile /tmp/.env.preprod -ToFile /tmp/.env.prod
  echo "exit: $?"
  grep -E "SENTRY_ENVIRONMENT|AUTO_ROLLBACK|CCE_IMAGE_TAG|IIS_HOSTNAMES" /tmp/.env.prod
  rm -f /tmp/.env.preprod /tmp/.env.prod
  ```
  Expected: exit 0; grep shows `SENTRY_ENVIRONMENT=production`, `AUTO_ROLLBACK=false`, `CCE_IMAGE_TAG=app-v1.0.0`, `IIS_HOSTNAMES=CCE-ext,...`.

- [ ] **Step 3:** Commit:
  ```bash
  git -C /Users/m/CCE add deploy/promote-env.ps1
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(deploy): promote-env.ps1 helper

  Mechanical promotion between envs (test → preprod → prod, etc.).
  Rewrites per-env knobs (DB name, hostnames, Sentry environment,
  AUTO_ROLLBACK default, log level, image tag stream) to the
  destination env's conventions. Per-env SECRETS (passwords, DSNs,
  tokens) are intentionally re-blanked to <set-me> — promotion
  must NOT inherit secrets across env boundaries.

  Operator follow-up steps printed at end: fill <set-me>, run
  validate-env.ps1, lock down ACLs, deploy.

  Sub-10c Phase 00 Task 0.5.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.6: Secret-rotation + env-promotion runbooks

**Files:**
- Create: `docs/runbooks/secret-rotation.md`.
- Create: `docs/runbooks/env-promotion.md`.

**Final state of `docs/runbooks/secret-rotation.md`:**

```markdown
# Secret rotation runbook (Sub-10c)

CCE secrets live in `C:\ProgramData\CCE\.env.<env>` on each environment's host. NTFS-locked to Administrators + the deploy service account. Rotation is operator-driven. Vault graduation is deferred to Sub-10d+; this runbook is the procedure.

## Rotation cadence

| Secret | Frequency | Trigger |
|---|---|---|
| `INFRA_SQL` password | Quarterly | Routine; or on suspected compromise |
| `LDAP_BIND_PASSWORD` | Quarterly | Routine; or on suspected compromise |
| `ANTHROPIC_API_KEY` | Semi-annually OR on compromise | Routine ops drill |
| `KEYCLOAK_ADMIN_PASSWORD` | Annually OR on compromise | Routine ops drill |
| `SENTRY_DSN` | Only on compromise | Sentry side regenerates DSN if requested |
| `BACKUP_UNC_PASSWORD` | Annually OR on compromise | Routine ops drill |
| `IIS_CERT_PFX_PASSWORD` | At cert renewal | Cert lifecycle |
| `CCE_GHCR_TOKEN` | Annually | GitHub PAT lifecycle |

## General procedure

For any secret rotation:

1. **Generate the new secret** (in the issuing system — SQL Server, AD, Anthropic console, etc.).
2. **Update the env-file** on each environment that uses it:
   ```powershell
   notepad C:\ProgramData\CCE\.env.<env>
   ```
   Replace the secret's value. Save.
3. **Validate the env-file**:
   ```powershell
   .\deploy\validate-env.ps1 -EnvFile C:\ProgramData\CCE\.env.<env> -Environment <env>
   ```
   Expected: `OK`.
4. **Re-deploy** to apply the new secret:
   ```powershell
   .\deploy\deploy.ps1 -Environment <env>
   ```
5. **Verify** the new secret works (per-secret verify steps below).
6. **Revoke the old secret** at the issuing system. **Don't skip this step** — old credentials remain valid until explicitly revoked.

## Per-secret procedure

### `INFRA_SQL` (SQL Server password)

1. In SQL Server, create a new login OR alter existing login's password:
   ```sql
   ALTER LOGIN [cce_app] WITH PASSWORD = '<new-strong-password>'
   ```
2. Update `INFRA_SQL` in `.env.<env>` (the connection string contains `Password=...`).
3. `validate-env.ps1` → `deploy.ps1` → `smoke.ps1` (Step 8 of deploy verifies app can hit DB).
4. After successful deploy, drop the old login if you created a new one (vs. altering).

### `LDAP_BIND_PASSWORD` (AD service-account password)

1. AD admin resets `cce-keycloak-svc` (or whatever bind account) password.
2. Update `LDAP_BIND_PASSWORD` in `.env.<env>`.
3. `validate-env.ps1` → `deploy.ps1`.
4. **Re-apply the Keycloak realm config** (this is what tells Keycloak the new bind cred):
   ```powershell
   .\infra\keycloak\apply-realm.ps1 -Environment <env>
   ```
5. Verify federation: log in as a known AD user via the assistant-portal login. Expected: success.

### `ANTHROPIC_API_KEY`

1. Anthropic console → API keys → Create a new key.
2. Update `ANTHROPIC_API_KEY` in `.env.<env>`.
3. `validate-env.ps1` → `deploy.ps1`.
4. Verify: hit the assistant endpoint, confirm a real Claude reply (not the stub).
5. Anthropic console → Revoke the old key.

### `KEYCLOAK_ADMIN_PASSWORD`

1. Keycloak admin UI: change master admin password.
2. Update `KEYCLOAK_ADMIN_PASSWORD` in `.env.<env>`.
3. Verify by re-running `apply-realm.ps1` — exit 0 means new admin password authenticates.

### `SENTRY_DSN`

1. Sentry project → Settings → Client Keys (DSN) → "Generate New Key" → revoke old.
2. Update `SENTRY_DSN` in `.env.<env>`.
3. `deploy.ps1` → trigger a test error → verify it appears in the Sentry dashboard.

### `BACKUP_UNC_PASSWORD`

1. File-server admin resets the credential used by the deploy host's `cmdkey` entry.
2. Update `BACKUP_UNC_PASSWORD` in `.env.<env>`.
3. **Re-cache the credential on the deploy host**:
   ```powershell
   cmdkey /delete:${BACKUP_UNC_HOST}
   cmdkey /add:${BACKUP_UNC_HOST} /user:${BACKUP_UNC_USER} /pass:${BACKUP_UNC_PASSWORD}
   ```
4. Verify next backup-sync task succeeds: `Get-ScheduledTask -TaskName CCE-Backup-Sync-OffHost | Get-ScheduledTaskInfo`.

### `IIS_CERT_PFX_PASSWORD`

Per-cert; rotated when cert is renewed. See [`infra/dns-tls/README.md`](../../infra/dns-tls/README.md) for cert renewal procedure.

### `CCE_GHCR_TOKEN`

1. GitHub → Settings → Developer Settings → PATs → Generate new token (`read:packages` scope).
2. Update `CCE_GHCR_TOKEN` in `.env.<env>`.
3. `deploy.ps1` (next run does `docker login` with new token).
4. GitHub → Revoke old PAT.

## Audit trail

`deploy-history-${env}.tsv` records every deploy + rollback. Cross-reference rotation operations with the deploy log files in `C:\ProgramData\CCE\logs\deploy-<env>-<UTC>.log`.

## See also

- [`env-promotion.md`](env-promotion.md) — promoting deploys across environments.
- [Sub-10c design spec §Secret rotation](../superpowers/specs/2026-05-04-sub-10c-design.md#secret-rotation-runbook-docsrunbookssecret-rotationmd).
```

**Final state of `docs/runbooks/env-promotion.md`:**

```markdown
# Environment promotion runbook (Sub-10c)

CCE has 4 environments: `test` → `preprod` → `prod` → (`dr` mirrors prod). Promotion is operator-driven, supported by `deploy/promote-env.ps1` for the mechanical config edits.

## When to promote

| From → To | Trigger | Image tag pattern |
|---|---|---|
| `test` → `preprod` | After test passes; promoting a feature for QA | release-candidate (`app-v1.0.0-rc.1`) |
| `preprod` → `prod` | After QA + stakeholder sign-off | release tag (`app-v1.0.0`) |
| `prod` → `dr` | When DR host needs to mirror prod's tag (e.g. before a known-risky deploy) | exact prod tag |

## Procedure: test → preprod

```powershell
# On the preprod host:
cd C:\path\to\CCE

# 1. Generate the preprod env-file from test's, with the new image tag.
.\deploy\promote-env.ps1 -FromEnv test -ToEnv preprod -ImageTag app-v1.0.0-rc.1
# Output: written to C:\ProgramData\CCE\.env.preprod with <set-me> placeholders.

# 2. Fill in the <set-me> values (preprod-specific secrets).
notepad C:\ProgramData\CCE\.env.preprod

# 3. Validate.
.\deploy\validate-env.ps1 -EnvFile C:\ProgramData\CCE\.env.preprod -Environment preprod
# Expected: OK.

# 4. Lock down ACLs.
icacls C:\ProgramData\CCE\.env.preprod /inheritance:r `
    /grant:r "Administrators:R" "<deploy-user>:R"

# 5. Deploy.
.\deploy\deploy.ps1 -Environment preprod
```

## Procedure: preprod → prod

Identical to the above; substitute `-FromEnv preprod -ToEnv prod` and use the release tag (no `-rc.N` suffix).

The first time prod runs, the `<set-me>` placeholders include the prod-specific `SENTRY_DSN`, prod LDAP bind account, prod backup-share user, etc. — different from preprod's. **`promote-env.ps1` deliberately re-blanks all secrets** so an operator can't accidentally inherit preprod creds into prod.

## Procedure: prod → dr (mirror)

```powershell
# On the DR host:
.\deploy\promote-env.ps1 -FromEnv prod -ToEnv dr -ImageTag <prod's-current-tag>
# ... fill <set-me>, validate, deploy.ps1 -Environment dr
```

DR host stays cold until promoted. Use `prod → dr` to keep the DR env-file's tag aligned before a planned risky deploy, so failover finds the right images.

## Common mistakes

| Mistake | Fix |
|---|---|
| Forgot to fill `<set-me>` | `validate-env.ps1` catches this; re-edit, re-validate. |
| Inherited secrets from prior env | Re-run `promote-env.ps1` (it re-blanks); fill in destination-specific values. |
| Wrong `CCE_IMAGE_TAG` | Edit `.env.<env>` directly, or re-run `promote-env.ps1` with `-Force`. |
| `Sentry_Environment` doesn't match `-Environment` | `validate-env.ps1` catches this; fix the env-file. |
| Used prod's hostnames for preprod (or vice versa) | Fixed automatically by `promote-env.ps1`'s per-env hostname table; if you bypassed, edit `IIS_HOSTNAMES` to match the destination's convention. |

## See also

- [`secret-rotation.md`](secret-rotation.md) — per-secret rotation procedure.
- [`deploy.md`](deploy.md) — green-path deploy.
- [`rollback.md`](rollback.md) — rollback within an env.
- [Sub-10c design spec §Multi-env config](../superpowers/specs/2026-05-04-sub-10c-design.md#multi-env-config--per-env-files).
```

- [ ] **Step 1:** Create `docs/runbooks/secret-rotation.md` with the contents above.

- [ ] **Step 2:** Create `docs/runbooks/env-promotion.md` with the contents above.

- [ ] **Step 3:** Commit:
  ```bash
  git -C /Users/m/CCE add docs/runbooks/secret-rotation.md docs/runbooks/env-promotion.md
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "docs(runbook): secret-rotation + env-promotion procedures

  secret-rotation.md is the per-secret rotation procedure: cadence
  table, general 6-step procedure, per-secret detail (INFRA_SQL,
  LDAP_BIND_PASSWORD, ANTHROPIC_API_KEY, KEYCLOAK_ADMIN_PASSWORD,
  SENTRY_DSN, BACKUP_UNC_PASSWORD, IIS_CERT_PFX_PASSWORD,
  CCE_GHCR_TOKEN). Each section ends with a verify step.

  env-promotion.md documents test → preprod → prod → dr promotion
  using promote-env.ps1. Includes the prod-mirror-to-DR pattern
  for keeping DR's image tag aligned before risky deploys.

  Sub-10c Phase 00 Task 0.6.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Phase 00 close-out

After Task 0.6 commits cleanly:

- [ ] **Run the full check:**
  ```bash
  cd /Users/m/CCE/backend && dotnet build && \
    dotnet test tests/CCE.Application.Tests/ tests/CCE.Infrastructure.Tests/
  ```
  Expected: backend build clean; 439 Application + 66 Infrastructure tests passing (1 skipped). Phase 00 doesn't add C# tests; numbers unchanged from Sub-10b.

- [ ] **Verify all 4 example files commit cleanly + are not ignored:**
  ```bash
  cd /Users/m/CCE && git ls-files .env*.example
  ```
  Expected: `.env.dr.example`, `.env.preprod.example`, `.env.prod.example`, `.env.test.example` (4 entries).

- [ ] **Verify deploy.ps1 backward compat:** with no env-file present, `.\deploy\deploy.ps1` (no args) should fail at Step 1 with "Env-file not found: C:\ProgramData\CCE\.env.prod" — same behavior as Sub-10b. New-arg path: `.\deploy\deploy.ps1 -Environment test` should fail with `.env.test` not found, also as expected.

- [ ] **Verify CI green** on push: existing CI workflows pass; `deploy-smoke.yml` is unchanged in Phase 00 — Phase 04 extends it.

- [ ] **Hand off to Phase 01.** Phase 01 writes `infra/keycloak/realm-cce-ldap-federation.json` + `apply-realm.ps1` + Testcontainers-based federation tests + ADR-0055. Plan file: `phase-01-identity-ad-federation.md` (to be written when ready).

**Phase 00 done when:**
- 6 commits land on `main`, each green.
- `deploy.ps1` accepts `-Environment <env>` and resolves to per-env env-file.
- `rollback.ps1` mirrors the switch.
- 4 `.env.<env>.example` files committed.
- `validate-env.ps1` rejects placeholders + leaked-secret canaries + suspicious whitespace + cross-key inconsistencies.
- `promote-env.ps1` performs mechanical promotion + re-blanks secrets.
- `secret-rotation.md` + `env-promotion.md` runbooks committed.
- Test counts unchanged: backend Application 439; Infrastructure 66 (1 skipped). Frontend 502.
- Sub-10b's existing `deploy-v1.0.0` deploys still work via backward-compatible defaults (no env-file regression).

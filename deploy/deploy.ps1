#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10b production deploy script.

.DESCRIPTION
    Validates the env-file, pulls the requested image tags from ghcr.io,
    runs the migrator to completion, brings up the 4 app services,
    and runs smoke probes. Idempotent — safe to re-run.

    Returns 0 on success, non-zero on any failure. Prints the rollback
    command on any failure after the first state-changing step.

.PARAMETER EnvFile
    Path to the production env-file. Default: C:\ProgramData\CCE\.env.prod.

.EXAMPLE
    .\deploy\deploy.ps1
    .\deploy\deploy.ps1 -EnvFile C:\ProgramData\CCE\.env.prod
#>
[CmdletBinding()]
param(
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile,
    [switch]$Recursive,
    [switch]$AutoRollback,
    [switch]$NoAutoRollback
)

$ErrorActionPreference = 'Stop'

# Default env-file derived from -Environment when -EnvFile not explicitly passed.
if (-not $EnvFile -or $EnvFile -eq '') {
    $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$composeBase   = Join-Path $repoRoot 'docker-compose.prod.yml'
$composeStrict = Join-Path $repoRoot 'docker-compose.prod.deploy.yml'
$historyFile   = "C:\ProgramData\CCE\deploy-history-$Environment.tsv"

# Logs directory + timestamped log file (per-env)
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

function Abort {
    param([string]$Message, [int]$ExitCode = 1, [switch]$ShowRollback)
    Write-Log -Level 'ERROR' -Message $Message
    if ($ShowRollback) {
        Write-Log -Level 'ERROR' -Message "Rollback command: .\deploy\rollback.ps1 -ToTag <previous-tag> -Environment $Environment"
        Write-Log -Level 'ERROR' -Message "Find previous tag in: C:\ProgramData\CCE\deploy-history-$Environment.tsv"
    }
    exit $ExitCode
}

function Send-SentryBreadcrumb {
    param(
        [hashtable]$EnvMap,
        [string]$CurrentTag,
        [string]$PreviousTag,
        [string]$Reason
    )
    $dsn = $EnvMap['SENTRY_DSN']
    if ([string]::IsNullOrWhiteSpace($dsn)) {
        Write-Log "SENTRY_DSN not set; skipping auto-rollback Sentry event."
        return
    }
    # Sentry DSN format: https://<key>@<host>/<project_id>
    $match = [regex]::Match($dsn, '^https://([^@]+)@([^/]+)/(.+)$')
    if (-not $match.Success) {
        Write-Log -Level 'WARN' "SENTRY_DSN looks malformed; skipping breadcrumb."
        return
    }
    $key = $match.Groups[1].Value
    $sentryHost = $match.Groups[2].Value
    $projectId = $match.Groups[3].Value

    $payload = @{
        message = "deploy.auto_rollback: $CurrentTag -> $PreviousTag ($Reason)"
        level = 'error'
        environment = $EnvMap['SENTRY_ENVIRONMENT'] ?? $Environment
        release = $CurrentTag
        tags = @{
            'deploy.auto_rollback' = 'true'
            'deploy.from_tag' = $CurrentTag
            'deploy.to_tag' = $PreviousTag
        }
        extra = @{
            reason = $Reason
            cce_environment = $Environment
            host = $env:COMPUTERNAME
        }
    } | ConvertTo-Json -Depth 10

    $sentryUrl = "https://$sentryHost/api/$projectId/store/"
    $sentryAuth = "Sentry sentry_version=7,sentry_key=$key,sentry_client=cce-deploy/1.0"
    try {
        Invoke-RestMethod -Uri $sentryUrl -Method Post `
            -Headers @{ 'X-Sentry-Auth' = $sentryAuth; 'Content-Type' = 'application/json' } `
            -Body $payload -TimeoutSec 5 | Out-Null
        Write-Log "Sentry auto-rollback event sent."
    } catch {
        Write-Log -Level 'WARN' "Sentry breadcrumb POST failed (non-fatal): $($_.Exception.Message)"
    }
}

# ─── Step 1: Resolve env-file path ─────────────────────────────────────────
Write-Log "Step 1/10: Resolving env-file path."
if (-not (Test-Path $EnvFile)) { Abort "Env-file not found: $EnvFile" }
$resolvedEnvFile = (Resolve-Path $EnvFile).Path
Write-Log "Env-file: $resolvedEnvFile"

# ─── Step 2: Validate env-file ─────────────────────────────────────────────
Write-Log "Step 2/10: Validating required keys."

# Sub-10c: canary integrity check (placeholder values, leaked-secret canaries, whitespace).
$validateScript = Join-Path $PSScriptRoot 'validate-env.ps1'
& pwsh -NoProfile -File $validateScript -EnvFile $resolvedEnvFile -Environment $Environment
if ($LASTEXITCODE -ne 0) { Abort "Env-file validation failed (canary check). See messages above." }

$envMap = @{}
foreach ($line in Get-Content $resolvedEnvFile) {
    if ($line -match '^\s*#') { continue }
    if ($line -match '^\s*$') { continue }
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $envMap[$Matches[1]] = $Matches[2].Trim()
    }
}
$required = @('CCE_REGISTRY_OWNER','CCE_IMAGE_TAG','INFRA_SQL','INFRA_REDIS','KEYCLOAK_AUTHORITY','KEYCLOAK_AUDIENCE')
$missing = $required | Where-Object { -not $envMap.ContainsKey($_) -or [string]::IsNullOrWhiteSpace($envMap[$_]) }
if ($envMap['ASSISTANT_PROVIDER'] -eq 'anthropic' -and [string]::IsNullOrWhiteSpace($envMap['ANTHROPIC_API_KEY'])) {
    $missing += 'ANTHROPIC_API_KEY (required when ASSISTANT_PROVIDER=anthropic)'
}
if ($missing) { Abort "Missing required env-keys: $($missing -join ', ')" }
Write-Log "CCE_IMAGE_TAG = $($envMap['CCE_IMAGE_TAG'])"
Write-Log "CCE_REGISTRY_OWNER = $($envMap['CCE_REGISTRY_OWNER'])"

# Export CCE_ENV_FILE so compose's env_file: directive resolves.
$env:CCE_ENV_FILE = $resolvedEnvFile

# ─── Step 3: Docker reachable? ─────────────────────────────────────────────
Write-Log "Step 3/10: Checking docker daemon."
& docker info > $null 2>&1
if ($LASTEXITCODE -ne 0) { Abort "Docker daemon not reachable. Is Docker Desktop / CE running?" }

# ─── Step 4: Registry login (optional) ─────────────────────────────────────
Write-Log "Step 4/10: Registry login."
if (-not [string]::IsNullOrWhiteSpace($envMap['CCE_GHCR_TOKEN'])) {
    Write-Log "CCE_GHCR_TOKEN present; logging into ghcr.io."
    $envMap['CCE_GHCR_TOKEN'] | & docker login ghcr.io -u $envMap['CCE_REGISTRY_OWNER'] --password-stdin
    if ($LASTEXITCODE -ne 0) { Abort "ghcr.io login failed." }
} else {
    Write-Log "CCE_GHCR_TOKEN not set; relying on existing docker login session."
}

# ─── Step 5: Pull images ───────────────────────────────────────────────────
Write-Log "Step 5/10: Pulling images for tag $($envMap['CCE_IMAGE_TAG'])."
& docker compose -f $composeBase -f $composeStrict --env-file $resolvedEnvFile pull
if ($LASTEXITCODE -ne 0) { Abort "Image pull failed. Verify CCE_IMAGE_TAG is correct: $($envMap['CCE_IMAGE_TAG'])" }

# ─── Step 6: Migrator step ─────────────────────────────────────────────────
$migrateOnDeploy = $envMap['MIGRATE_ON_DEPLOY']
if ($null -eq $migrateOnDeploy -or $migrateOnDeploy -eq '') { $migrateOnDeploy = 'true' }
if ($migrateOnDeploy -ieq 'true') {
    Write-Log "Step 6/10: Running migrator."
    & docker compose -f $composeBase -f $composeStrict --env-file $resolvedEnvFile run --rm --no-deps migrator
    if ($LASTEXITCODE -ne 0) { Abort "Migrator failed (exit $LASTEXITCODE). Migrations NOT applied; APIs not started." -ShowRollback }
} else {
    Write-Log "Step 6/10: MIGRATE_ON_DEPLOY=false; skipping migrator. Operator must run migrations manually."
}

# ─── Step 7: Up the apps ───────────────────────────────────────────────────
Write-Log "Step 7/10: Bringing up apps."
& docker compose -f $composeBase -f $composeStrict --env-file $resolvedEnvFile up -d --no-deps --remove-orphans api-external api-internal web-portal admin-cms
if ($LASTEXITCODE -ne 0) { Abort "App startup failed." -ShowRollback }

# ─── Step 8: Smoke probe ──────────────────────────────────────────────────
Write-Log "Step 8/10: Running smoke probes."
$smokeScript = Join-Path $PSScriptRoot 'smoke.ps1'
& pwsh -NoProfile -File $smokeScript -Timeout 60
$smokeExitCode = $LASTEXITCODE

if ($smokeExitCode -ne 0) {
    # Resolve auto-rollback decision (precedence high → low):
    #  -NoAutoRollback wins; -Recursive (nested call from rollback.ps1) suppresses;
    #  -AutoRollback flag forces; env-file AUTO_ROLLBACK=true triggers; else don't.
    $autoRollbackEnabled = $false
    if ($NoAutoRollback) {
        Write-Log "Smoke failed; -NoAutoRollback override — leaving apps running for operator inspection."
    } elseif ($Recursive) {
        Write-Log "Smoke failed; -Recursive set (nested deploy from rollback.ps1) — recursion guard suppresses auto-rollback."
    } elseif ($AutoRollback) {
        Write-Log "Smoke failed; -AutoRollback flag — attempting auto-rollback."
        $autoRollbackEnabled = $true
    } elseif ($envMap['AUTO_ROLLBACK'] -ieq 'true') {
        Write-Log "Smoke failed; AUTO_ROLLBACK=true in env-file — attempting auto-rollback."
        $autoRollbackEnabled = $true
    } else {
        Write-Log "Smoke failed; auto-rollback NOT enabled."
    }

    if ($autoRollbackEnabled) {
        # Resolve previous OK tag from deploy-history-${env}.tsv.
        $currentTag = $envMap['CCE_IMAGE_TAG']
        $previousTag = $null
        if (Test-Path $historyFile) {
            $okRows = Get-Content $historyFile | Where-Object { $_ -match '\tOK(\t|$)' }
            # TSV columns: <UTC-iso8601> \t <sha> \t <tag> \t OK [\t ROLLBACK_FROM=...]
            # Walk newest → oldest, pick first with a tag != current.
            for ($i = $okRows.Count - 1; $i -ge 0; $i--) {
                $cols = $okRows[$i].Split("`t")
                if ($cols.Count -ge 3 -and $cols[2] -and $cols[2] -ne $currentTag) {
                    $previousTag = $cols[2]
                    break
                }
            }
        }

        if (-not $previousTag) {
            Abort "Auto-rollback enabled but no prior OK tag found in $historyFile. Operator-driven rollback only."
        }

        Write-Log "Auto-rolling back to '$previousTag' (current was '$currentTag')."
        Send-SentryBreadcrumb -EnvMap $envMap -CurrentTag $currentTag -PreviousTag $previousTag -Reason "smoke-probe failure"

        $rollbackScript = Join-Path $PSScriptRoot 'rollback.ps1'
        & pwsh -NoProfile -File $rollbackScript -ToTag $previousTag -Environment $Environment -EnvFile $resolvedEnvFile
        $rollbackExitCode = $LASTEXITCODE

        if ($rollbackExitCode -ne 0) {
            Abort "Auto-rollback FAILED (rollback.ps1 exit $rollbackExitCode). Manual intervention required. Both bad tag '$currentTag' and rollback target '$previousTag' may be unhealthy."
        }
        Write-Log "Auto-rollback complete; live tag is now '$previousTag'."
        exit 0
    }

    Abort "Smoke probe failed. Apps left running for inspection." -ShowRollback
}

# ─── Step 9: Append deploy-history.tsv ────────────────────────────────────
Write-Log "Step 9/10: Appending deploy-history.tsv."
# $historyFile defined at top of script.
# Capture git SHA from the env-file's tag if it looks like sha-* or a hex SHA;
# otherwise leave SHA blank (release tags don't carry SHA info here).
$tagValue = $envMap['CCE_IMAGE_TAG']
$sha = ''
if ($tagValue -match '^sha-([0-9a-f]{7,40})$') { $sha = $Matches[1] }
elseif ($tagValue -match '^[0-9a-f]{40}$')     { $sha = $tagValue }
$tsRow = "{0:yyyy-MM-ddTHH:mm:ssZ}`t{1}`t{2}`t{3}" -f `
    (Get-Date).ToUniversalTime(), `
    $sha, `
    $tagValue, `
    'OK'
Add-Content -Path $historyFile -Value $tsRow
Write-Log "deploy-history.tsv appended."

# ─── Step 10: Print summary ────────────────────────────────────────────────
Write-Log "Step 10/10: Done."
Write-Log "Image tag deployed: $($envMap['CCE_IMAGE_TAG'])"
Write-Log "Registry owner    : $($envMap['CCE_REGISTRY_OWNER'])"
Write-Log "Log file          : $logFile"
exit 0

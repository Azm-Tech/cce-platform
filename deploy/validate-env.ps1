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

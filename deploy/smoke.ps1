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

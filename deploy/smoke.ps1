#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10b smoke-probe script.

.DESCRIPTION
    Polls the 4 localhost endpoints exposed by the production compose
    stack. Each endpoint: 30 attempts × 2 sec backoff = 60-sec window.
    Returns 0 if all pass, 1 on first failure.

.PARAMETER Timeout
    Per-endpoint timeout in seconds. Default 60.

.PARAMETER Quiet
    Suppress per-attempt output; only print the final result.

.EXAMPLE
    .\deploy\smoke.ps1
    .\deploy\smoke.ps1 -Timeout 90
    .\deploy\smoke.ps1 -Quiet
#>
[CmdletBinding()]
param(
    [int]$Timeout = 60,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

$probes = @(
    @{ Name = 'api-external/health'; Url = 'http://localhost:5001/health'; Type = 'health' },
    @{ Name = 'api-internal/health'; Url = 'http://localhost:5002/health'; Type = 'health' },
    @{ Name = 'web-portal/';         Url = 'http://localhost:4200/';        Type = 'html'   },
    @{ Name = 'admin-cms/';          Url = 'http://localhost:4201/';        Type = 'html'   }
)

$attempts = [Math]::Max(1, [int]($Timeout / 2))
$failed = @()

foreach ($probe in $probes) {
    if (-not $Quiet) { Write-Host "Probing $($probe.Name)..." -NoNewline }
    $ok = $false
    for ($i = 1; $i -le $attempts; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $probe.Url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
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

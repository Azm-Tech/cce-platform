#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10c — provision Keycloak LDAP user-federation provider.

.DESCRIPTION
    Idempotently provisions Keycloak's "User Federation" against AD on the
    'cce' realm. Reads LDAP_* keys + KEYCLOAK_ADMIN_* keys from the env-file.
    Authenticates as master admin, looks up or creates the federation
    provider component, attaches the group-mapper, triggers an initial sync.

    Re-runs PATCH the existing components rather than duplicating.

.PARAMETER Environment
    Environment name. One of test, preprod, prod, dr. Default: prod.

.PARAMETER EnvFile
    Override env-file path. Default: C:\ProgramData\CCE\.env.<Environment>.

.PARAMETER RealmJson
    Path to the realm-import JSON template. Default: ./realm-cce-ldap-federation.json
    (relative to this script).

.EXAMPLE
    .\infra\keycloak\apply-realm.ps1 -Environment prod
#>
[CmdletBinding()]
param(
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile,
    [string]$RealmJson
)

$ErrorActionPreference = 'Stop'

if (-not $EnvFile -or $EnvFile -eq '') {
    $EnvFile = "C:\ProgramData\CCE\.env.$Environment"
}
if (-not $RealmJson -or $RealmJson -eq '') {
    $RealmJson = Join-Path $PSScriptRoot 'realm-cce-ldap-federation.json'
}

# Logs
$logDir = 'C:\ProgramData\CCE\logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir ("keycloak-apply-{0}-{1:yyyyMMddTHHmmssZ}.log" -f $Environment, (Get-Date).ToUniversalTime())

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $line = "[$ts] [$Level] [$Environment] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

# ─── Parse env-file ───────────────────────────────────────────────────────
if (-not (Test-Path $EnvFile))    { Write-Error "Env-file not found: $EnvFile";    exit 1 }
if (-not (Test-Path $RealmJson))  { Write-Error "Realm JSON not found: $RealmJson"; exit 1 }

$envMap = @{}
foreach ($line in Get-Content $EnvFile) {
    if ($line -match '^\s*#') { continue }
    if ($line -match '^\s*$') { continue }
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $envMap[$Matches[1]] = $Matches[2].Trim() -replace '\s*#.*$',''
    }
}

$required = @(
    'KEYCLOAK_AUTHORITY','KEYCLOAK_ADMIN_USER','KEYCLOAK_ADMIN_PASSWORD',
    'LDAP_HOST','LDAP_PORT','LDAP_BIND_DN','LDAP_BIND_PASSWORD',
    'LDAP_USERS_DN','LDAP_GROUPS_DN'
)
$missing = $required | Where-Object { -not $envMap.ContainsKey($_) -or [string]::IsNullOrWhiteSpace($envMap[$_]) }
if ($missing) { Write-Error "Missing required env-keys: $($missing -join ', ')"; exit 1 }

# Derive Keycloak base URL from KEYCLOAK_AUTHORITY (strip /realms/<realm> suffix).
$kcBase = $envMap['KEYCLOAK_AUTHORITY'] -replace '/realms/.*$',''
$realmName = 'cce'

Write-Log "Keycloak base: $kcBase"
Write-Log "Realm: $realmName"
Write-Log "LDAP host: $($envMap['LDAP_HOST']):$($envMap['LDAP_PORT'])"

# ─── Acquire master admin token ──────────────────────────────────────────
$tokenUrl = "$kcBase/realms/master/protocol/openid-connect/token"
$tokenBody = @{
    grant_type = 'password'
    client_id  = 'admin-cli'
    username   = $envMap['KEYCLOAK_ADMIN_USER']
    password   = $envMap['KEYCLOAK_ADMIN_PASSWORD']
}
try {
    $tokenResp = Invoke-RestMethod -Uri $tokenUrl -Method Post -Body $tokenBody -ContentType 'application/x-www-form-urlencoded'
} catch {
    Write-Log -Level 'ERROR' "Failed to acquire master-admin token from $tokenUrl. Verify KEYCLOAK_ADMIN_USER/PASSWORD."
    Write-Log -Level 'ERROR' $_.Exception.Message
    exit 1
}
$token = $tokenResp.access_token
$headers = @{ Authorization = "Bearer $token"; 'Content-Type' = 'application/json' }
Write-Log "Master-admin token acquired."

# ─── Substitute env-vars into realm JSON ─────────────────────────────────
$jsonText = (Get-Content $RealmJson -Raw)
$jsonText = $jsonText `
    -replace '\$\{LDAP_HOST\}',          $envMap['LDAP_HOST'] `
    -replace '\$\{LDAP_PORT\}',          $envMap['LDAP_PORT'] `
    -replace '\$\{LDAP_BIND_DN\}',       $envMap['LDAP_BIND_DN'] `
    -replace '\$\{LDAP_BIND_PASSWORD\}', $envMap['LDAP_BIND_PASSWORD'] `
    -replace '\$\{LDAP_USERS_DN\}',      $envMap['LDAP_USERS_DN'] `
    -replace '\$\{LDAP_GROUPS_DN\}',     $envMap['LDAP_GROUPS_DN']

$realmData = $jsonText | ConvertFrom-Json -Depth 10

# Split parent component + group-mapper child.
$groupMapper = $realmData._groupMapper
$realmData.PSObject.Properties.Remove('_groupMapper')

# ─── Idempotent: lookup existing federation provider by name ─────────────
$compsUrl = "$kcBase/admin/realms/$realmName/components?type=org.keycloak.storage.UserStorageProvider"
$existingComps = Invoke-RestMethod -Uri $compsUrl -Headers $headers
$existing = $existingComps | Where-Object { $_.name -eq $realmData.name }

$body = $realmData | ConvertTo-Json -Depth 10

if ($existing) {
    $compId = $existing.id
    Write-Log "Federation provider '$($realmData.name)' exists (id=$compId); PUT to update."
    # Keycloak requires id + parentId on PUT.
    $realmData | Add-Member -NotePropertyName id -NotePropertyValue $compId -Force
    $realmData | Add-Member -NotePropertyName parentId -NotePropertyValue $existing.parentId -Force
    $body = $realmData | ConvertTo-Json -Depth 10
    $putUrl = "$kcBase/admin/realms/$realmName/components/$compId"
    Invoke-RestMethod -Uri $putUrl -Method Put -Headers $headers -Body $body | Out-Null
    Write-Log "Federation provider updated."
} else {
    Write-Log "Federation provider '$($realmData.name)' not found; POST to create."
    $createUrl = "$kcBase/admin/realms/$realmName/components"
    Invoke-RestMethod -Uri $createUrl -Method Post -Headers $headers -Body $body | Out-Null
    # Re-fetch to get the new component's id.
    $existingComps = Invoke-RestMethod -Uri $compsUrl -Headers $headers
    $existing = $existingComps | Where-Object { $_.name -eq $realmData.name }
    $compId = $existing.id
    Write-Log "Federation provider created (id=$compId)."
}

# ─── Idempotent: lookup existing group mapper by name + parentId ─────────
$mappersUrl = "$kcBase/admin/realms/$realmName/components?type=org.keycloak.storage.ldap.mappers.LDAPStorageMapper&parent=$compId"
$existingMappers = Invoke-RestMethod -Uri $mappersUrl -Headers $headers
$existingMapper = $existingMappers | Where-Object { $_.name -eq $groupMapper.name }

# Attach parentId on the mapper.
$groupMapper | Add-Member -NotePropertyName parentId -NotePropertyValue $compId -Force
$mapperBody = $groupMapper | ConvertTo-Json -Depth 10

if ($existingMapper) {
    $mapperId = $existingMapper.id
    Write-Log "Group mapper '$($groupMapper.name)' exists (id=$mapperId); PUT to update."
    $groupMapper | Add-Member -NotePropertyName id -NotePropertyValue $mapperId -Force
    $mapperBody = $groupMapper | ConvertTo-Json -Depth 10
    $putUrl = "$kcBase/admin/realms/$realmName/components/$mapperId"
    Invoke-RestMethod -Uri $putUrl -Method Put -Headers $headers -Body $mapperBody | Out-Null
    Write-Log "Group mapper updated."
} else {
    Write-Log "Group mapper '$($groupMapper.name)' not found; POST to create."
    $createUrl = "$kcBase/admin/realms/$realmName/components"
    Invoke-RestMethod -Uri $createUrl -Method Post -Headers $headers -Body $mapperBody | Out-Null
    Write-Log "Group mapper created."
}

# ─── Trigger initial sync (best-effort; logs failure but doesn't abort) ──
$syncUrl = "$kcBase/admin/realms/$realmName/user-storage/$compId/sync?action=triggerFullSync"
try {
    Invoke-RestMethod -Uri $syncUrl -Method Post -Headers $headers | Out-Null
    Write-Log "Initial user sync triggered."
} catch {
    Write-Log -Level 'WARN' "Sync trigger failed (non-fatal): $($_.Exception.Message)"
}

Write-Log "apply-realm.ps1 done. Federation provider id: $compId"
exit 0

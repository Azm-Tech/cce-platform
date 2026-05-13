# Phase 01 — Identity (AD federation via Keycloak LDAP)

> Parent: [`../2026-05-04-sub-10c.md`](../2026-05-04-sub-10c.md) · Spec: [`../../specs/2026-05-04-sub-10c-design.md`](../../specs/2026-05-04-sub-10c-design.md) §Identity — Keycloak LDAP user federation.

**Phase goal:** Wire CCE's existing Keycloak realm `cce` to Active Directory via Keycloak's User Federation provider, read-only, with AD security groups → Keycloak roles mapping. Operator-callable provisioning script + idempotent re-runs + Testcontainers-backed unit tests + ADR-0055 + troubleshooting runbook.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 00 closed (6 commits land on `main`; HEAD at `2af6de3` or later).
- `.env.<env>.example` files include LDAP_* keys (added in Task 0.3).
- `Testcontainers.Keycloak` v4.0.0 + `Testcontainers` v4.0.0 already pinned in `Directory.Packages.props` and referenced by `CCE.Infrastructure.Tests` (added in Sub-10b Phase 00 Task 0.3).
- Backend baseline: 439 Application + 66 Infrastructure tests passing (1 skipped).

---

## Task 1.1: `apply-realm.ps1` — Keycloak admin REST API wrapper

**Files:**
- Create: `infra/keycloak/apply-realm.ps1` — idempotent provisioning script that authenticates as Keycloak master admin, looks up/creates the LDAP user-federation provider on the `cce` realm, and triggers initial sync.
- Create: `infra/keycloak/realm-cce-ldap-federation.json` — committed JSON template with `${LDAP_HOST}`-style placeholders that `apply-realm.ps1` substitutes before POST.

**Why this combination:** the JSON describes the federation provider's attribute matrix (vendor=AD, RDN=cn, UUID=objectGUID, etc.) so the script doesn't carry that detail in its source. Operators can review the JSON for what gets configured. `apply-realm.ps1` handles env-var substitution, master-admin token acquisition, idempotent PATCH-vs-create, and exit-code semantics.

**Final state of `infra/keycloak/realm-cce-ldap-federation.json`:**

```json
{
  "name": "cce-ldap",
  "providerId": "ldap",
  "providerType": "org.keycloak.storage.UserStorageProvider",
  "config": {
    "enabled": ["true"],
    "vendor": ["ad"],
    "connectionUrl": ["ldaps://${LDAP_HOST}:${LDAP_PORT}"],
    "useTruststoreSpi": ["ldapsOnly"],
    "connectionPooling": ["true"],
    "authType": ["simple"],
    "bindDn": ["${LDAP_BIND_DN}"],
    "bindCredential": ["${LDAP_BIND_PASSWORD}"],
    "editMode": ["READ_ONLY"],
    "syncRegistrations": ["false"],
    "importEnabled": ["true"],
    "usersDn": ["${LDAP_USERS_DN}"],
    "searchScope": ["2"],
    "usernameLDAPAttribute": ["sAMAccountName"],
    "rdnLDAPAttribute": ["cn"],
    "uuidLDAPAttribute": ["objectGUID"],
    "userObjectClasses": ["person, organizationalPerson, user"],
    "validatePasswordPolicy": ["true"],
    "trustEmail": ["true"],
    "useKerberosForPasswordAuthentication": ["false"],
    "fullSyncPeriod": ["-1"],
    "changedSyncPeriod": ["-1"],
    "batchSizeForSync": ["1000"],
    "pagination": ["true"],
    "allowKerberosAuthentication": ["false"],
    "debug": ["false"]
  },
  "_groupMapper": {
    "name": "cce-group-mapper",
    "providerId": "group-ldap-mapper",
    "providerType": "org.keycloak.storage.ldap.mappers.LDAPStorageMapper",
    "config": {
      "groups.dn": ["${LDAP_GROUPS_DN}"],
      "group.name.ldap.attribute": ["cn"],
      "group.object.classes": ["group"],
      "membership.ldap.attribute": ["member"],
      "membership.attribute.type": ["DN"],
      "membership.user.ldap.attribute": ["sAMAccountName"],
      "preserve.group.inheritance": ["false"],
      "ignore.missing.groups": ["true"],
      "user.roles.retrieve.strategy": ["LOAD_GROUPS_BY_MEMBER_ATTRIBUTE"],
      "mapped.group.attributes": [""],
      "mode": ["READ_ONLY"],
      "drop.non.existing.groups.during.sync": ["false"],
      "groups.path": ["/"]
    }
  }
}
```

**Note on `_groupMapper`:** the underscore prefix is non-standard JSON convention saying "pull this out and POST it as a separate request after the parent provider is created." `apply-realm.ps1` handles the split. Keycloak's REST API requires the group-mapper as a child of the user-federation provider component, so it's a second POST.

**Final state of `infra/keycloak/apply-realm.ps1`:**

```powershell
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
$jsonText = Get-Content $RealmJson -Raw
foreach ($k in $envMap.Keys) {
    $jsonText = $jsonText -replace [regex]::Escape("`${$k}"), [System.Text.RegularExpressions.Regex]::Escape($envMap[$k]).Replace('\','\\').Replace('"','\"')
    # Simpler form acceptable here: env values don't contain shell-special chars typically.
}
# Re-parse — easier than escaping every value.
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

# Convert PSCustomObject to hashtable for POST/PUT body.
function ConvertTo-Hashtable($obj) {
    $h = @{}
    $obj.PSObject.Properties | ForEach-Object { $h[$_.Name] = $_.Value }
    return $h
}
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
```

**Note on env-var substitution:** the script uses chained `-replace` for the 6 known LDAP_* placeholders. This is intentionally narrow — we don't substitute every env-var generically because env values can contain regex-special chars. The 6 listed are the only ones the JSON template references.

- [ ] **Step 1:** Create `infra/keycloak/` directory:
  ```bash
  mkdir -p /Users/m/CCE/infra/keycloak
  ```

- [ ] **Step 2:** Create `infra/keycloak/realm-cce-ldap-federation.json` with the contents above.

- [ ] **Step 3:** Create `infra/keycloak/apply-realm.ps1` with the contents above.

- [ ] **Step 4:** Verify JSON parses:
  ```bash
  python3 -c "import json; json.load(open('/Users/m/CCE/infra/keycloak/realm-cce-ldap-federation.json'))"
  ```
  Expected: no errors.

- [ ] **Step 5:** Commit:
  ```bash
  git -C /Users/m/CCE add infra/keycloak/realm-cce-ldap-federation.json infra/keycloak/apply-realm.ps1
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(infra): apply-realm.ps1 + LDAP federation realm JSON

  Idempotent Keycloak provisioning for the cce realm's LDAP user-
  federation provider (vendor=ad, READ_ONLY, LDAPS port 636, AD
  attribute matrix). Includes a child group-ldap-mapper for AD
  security groups → Keycloak roles.

  apply-realm.ps1 reads KEYCLOAK_ADMIN_* + LDAP_* from the env-file
  (-Environment switch), acquires a master-admin token, looks up
  the federation provider by name, PUTs to update or POSTs to
  create. Then does the same for the group mapper (attached as
  child via parentId). Best-effort initial sync trigger at end.

  Sub-10c Phase 01 Task 1.1.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 1.2: Testcontainers Keycloak + OpenLDAP fixture

**Files:**
- Create: `backend/tests/CCE.Infrastructure.Tests/Identity/KeycloakLdapFixture.cs` — xUnit collection fixture that boots a Keycloak container + an OpenLDAP container on a shared docker network, seeds the LDAP with a fixture LDIF (one user, one group), and exposes a method for tests to call `apply-realm.ps1` against the running Keycloak.

**Why a fixture not per-test boot:** Keycloak boots in ~10 seconds; OpenLDAP in ~3. Per-test would multiply that. xUnit's collection fixture pattern (already used by `MigratorFixture` from Sub-10b Phase 00) is the established shape.

**Stack:**
- Keycloak: `Testcontainers.Keycloak` (already pinned + referenced from Sub-10b).
- OpenLDAP: generic `Testcontainers` builder against `bitnami/openldap:2.6` (no NuGet `Testcontainers.OpenLdap`; we use the generic builder, same as Sub-10b's `MigratorFixture` does for SQL).

**Final state of `KeycloakLdapFixture.cs`:**

```cs
using System.Diagnostics.CodeAnalysis;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.Keycloak;
using Xunit;

namespace CCE.Infrastructure.Tests.Identity;

/// <summary>
/// xUnit fixture that boots one Keycloak container + one OpenLDAP container
/// on a shared docker network. Shared across all tests in
/// <see cref="KeycloakLdapCollection"/>. OpenLDAP is seeded from a fixture
/// LDIF (one user 'alice' under OU=Users, one group 'CCE-Admins' under
/// OU=Groups). Each test should clean up any state it modifies (drop the
/// federation provider before exiting).
/// </summary>
public sealed class KeycloakLdapFixture : IAsyncLifetime
{
    private const string LdapAdminPassword = "admin-pass-1234";
    private const string LdapBaseDn        = "DC=cce,DC=local";
    private const string LdapBindDn        = "cn=admin,DC=cce,DC=local";

    public INetwork Network { get; }
    public KeycloakContainer Keycloak { get; }
    public IContainer OpenLdap { get; }

    public string KeycloakAdminUser     => "admin";
    public string KeycloakAdminPassword => "admin";
    public string LdapHost              => "openldap";       // hostname inside the docker network
    public int    LdapPort              => 1389;             // bitnami openldap default
    public string LdapBindDnPublic      => LdapBindDn;
    public string LdapBindPasswordPublic=> LdapAdminPassword;
    public string LdapUsersDnPublic     => $"OU=Users,{LdapBaseDn}";
    public string LdapGroupsDnPublic    => $"OU=Groups,{LdapBaseDn}";

    public KeycloakLdapFixture()
    {
        Network = new NetworkBuilder()
            .WithName($"cce-keycloak-ldap-{Guid.NewGuid():N}")
            .Build();

        Keycloak = new KeycloakBuilder()
            .WithImage("quay.io/keycloak/keycloak:26.0")
            .WithUsername(KeycloakAdminUser)
            .WithPassword(KeycloakAdminPassword)
            .WithNetwork(Network)
            .Build();

        OpenLdap = new ContainerBuilder()
            .WithImage("bitnami/openldap:2.6")
            .WithNetwork(Network)
            .WithNetworkAliases("openldap")
            .WithEnvironment("LDAP_ADMIN_USERNAME", "admin")
            .WithEnvironment("LDAP_ADMIN_PASSWORD", LdapAdminPassword)
            .WithEnvironment("LDAP_ROOT", LdapBaseDn)
            .WithEnvironment("LDAP_PORT_NUMBER", "1389")
            .WithEnvironment("LDAP_USERS", "alice")
            .WithEnvironment("LDAP_PASSWORDS", "alice-pass")
            .WithPortBinding(1389, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1389))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Network.CreateAsync().ConfigureAwait(false);
        // Boot Keycloak + OpenLDAP in parallel.
        var kcTask = Keycloak.StartAsync();
        var ldapTask = OpenLdap.StartAsync();
        await Task.WhenAll(kcTask, ldapTask).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await Keycloak.DisposeAsync().ConfigureAwait(false);
        await OpenLdap.DisposeAsync().ConfigureAwait(false);
        await Network.DisposeAsync().ConfigureAwait(false);
    }
}

[CollectionDefinition(nameof(KeycloakLdapCollection))]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xUnit's CollectionDefinition pattern uses 'Collection' as the conventional suffix.")]
public sealed class KeycloakLdapCollection : ICollectionFixture<KeycloakLdapFixture> { }
```

**Note on OpenLDAP image:** `bitnami/openldap:2.6` accepts `LDAP_USERS` + `LDAP_PASSWORDS` env-vars at boot to provision a minimal user. That gives us "alice" under `OU=Users,DC=cce,DC=local` automatically — sufficient for the federation-provider tests in Task 1.3. We're not testing AD's full schema; we're testing that Keycloak's REST API accepts the federation provider config and the group mapper, and that re-applying is idempotent.

**Note on parent class for Identity test folder:** the existing `Migration/MigratorFixture.cs` from Sub-10b uses the same xUnit collection-fixture pattern. We follow it for consistency.

- [ ] **Step 1:** Create the test folder:
  ```bash
  mkdir -p /Users/m/CCE/backend/tests/CCE.Infrastructure.Tests/Identity
  ```

- [ ] **Step 2:** Create `Identity/KeycloakLdapFixture.cs` with the contents above.

- [ ] **Step 3:** Verify it compiles (no test class yet, so it's just the fixture):
  ```bash
  cd /Users/m/CCE/backend && dotnet build tests/CCE.Infrastructure.Tests/ --nologo 2>&1 | tail -8
  ```
  Expected: success.

- [ ] **Step 4:** Commit:
  ```bash
  git -C /Users/m/CCE add backend/tests/CCE.Infrastructure.Tests/Identity/KeycloakLdapFixture.cs
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "test(identity): KeycloakLdapFixture xUnit collection fixture

  Boots a Keycloak 26.0 container + a bitnami/openldap:2.6 container
  on a shared docker network with hostname 'openldap'. OpenLDAP
  seeded with a single user 'alice' via bitnami's LDAP_USERS env-
  var (no LDIF file needed for the federation-provider tests).
  Both containers boot in parallel during InitializeAsync.

  Properties expose Keycloak admin creds + LDAP bind config to
  consuming tests. Container hostname 'openldap' resolves inside
  the network, so the federation provider's connectionUrl uses
  ldap://openldap:1389 (port 1389 = bitnami default; not LDAPS in
  test — production uses 636).

  Sub-10c Phase 01 Task 1.2.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 1.3: `KeycloakLdapFederationTests` — apply-realm idempotency tests

**Files:**
- Create: `backend/tests/CCE.Infrastructure.Tests/Identity/KeycloakLdapFederationTests.cs` — three tests using the fixture from Task 1.2.

**Test scope:** the script `apply-realm.ps1` is PowerShell and would be hard to invoke from xUnit. We test the *behaviour* it produces — that the Keycloak REST API correctly accepts the federation-provider + group-mapper components, and that re-POST-then-PUT is idempotent — by exercising the same REST endpoints from C# against the fixture's running Keycloak.

This is a deliberate trade-off: we get fast, hermetic tests against real Keycloak; we don't end-to-end-test the PowerShell script (deploy-smoke.yml in Phase 04 will exercise the script invocation on Windows runners).

**Final state of `KeycloakLdapFederationTests.cs`:**

```cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace CCE.Infrastructure.Tests.Identity;

[Collection(nameof(KeycloakLdapCollection))]
public sealed class KeycloakLdapFederationTests : IDisposable
{
    private readonly KeycloakLdapFixture _fixture;
    private readonly HttpClient _http;

    public KeycloakLdapFederationTests(KeycloakLdapFixture fixture)
    {
        _fixture = fixture;
        _http = new HttpClient { BaseAddress = new Uri(fixture.Keycloak.GetBaseAddress()) };
    }

    public void Dispose()
    {
        _http.Dispose();
    }

    private async Task<string> AcquireMasterAdminTokenAsync()
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id",  "admin-cli"),
            new KeyValuePair<string, string>("username",   _fixture.KeycloakAdminUser),
            new KeyValuePair<string, string>("password",   _fixture.KeycloakAdminPassword),
        });
        var resp = await _http.PostAsync("/realms/master/protocol/openid-connect/token", form);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("access_token").GetString()!;
    }

    private async Task<string> EnsureRealmExistsAsync(string token)
    {
        // Master admin can create realms via POST /admin/realms.
        const string realmName = "cce";
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var get = await _http.GetAsync($"/admin/realms/{realmName}");
        if (get.IsSuccessStatusCode) return realmName;

        var body = JsonSerializer.Serialize(new { realm = realmName, enabled = true });
        var post = await _http.PostAsync(
            "/admin/realms",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        post.EnsureSuccessStatusCode();
        return realmName;
    }

    private object BuildFederationProviderBody() => new
    {
        name = "cce-ldap",
        providerId = "ldap",
        providerType = "org.keycloak.storage.UserStorageProvider",
        config = new Dictionary<string, string[]>
        {
            ["enabled"]                  = new[] { "true" },
            ["vendor"]                   = new[] { "ad" },
            ["connectionUrl"]            = new[] { $"ldap://{_fixture.LdapHost}:{_fixture.LdapPort}" },
            ["authType"]                 = new[] { "simple" },
            ["bindDn"]                   = new[] { _fixture.LdapBindDnPublic },
            ["bindCredential"]           = new[] { _fixture.LdapBindPasswordPublic },
            ["editMode"]                 = new[] { "READ_ONLY" },
            ["syncRegistrations"]        = new[] { "false" },
            ["importEnabled"]            = new[] { "true" },
            ["usersDn"]                  = new[] { _fixture.LdapUsersDnPublic },
            ["searchScope"]              = new[] { "2" },
            ["usernameLDAPAttribute"]    = new[] { "sAMAccountName" },
            ["rdnLDAPAttribute"]         = new[] { "cn" },
            ["uuidLDAPAttribute"]        = new[] { "objectGUID" },
            ["userObjectClasses"]        = new[] { "person, organizationalPerson, user" },
            ["validatePasswordPolicy"]   = new[] { "true" },
            ["trustEmail"]               = new[] { "true" },
            ["pagination"]               = new[] { "true" },
            ["batchSizeForSync"]         = new[] { "1000" },
            ["fullSyncPeriod"]           = new[] { "-1" },
            ["changedSyncPeriod"]        = new[] { "-1" },
        }
    };

    [Fact]
    public async Task FederationProvider_CreatesViaPost_OnFreshRealm()
    {
        var token = await AcquireMasterAdminTokenAsync();
        var realm = await EnsureRealmExistsAsync(token);

        var body = BuildFederationProviderBody();
        var bodyJson = JsonSerializer.Serialize(body);

        var post = await _http.PostAsync(
            $"/admin/realms/{realm}/components",
            new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json"));
        post.IsSuccessStatusCode.Should().BeTrue("POST should create the federation component");

        // Verify the component is now visible.
        var listResp = await _http.GetAsync(
            $"/admin/realms/{realm}/components?type=org.keycloak.storage.UserStorageProvider");
        listResp.EnsureSuccessStatusCode();
        var components = await listResp.Content.ReadFromJsonAsync<JsonElement[]>();
        components.Should().NotBeNull();
        components!.Should().Contain(c => c.GetProperty("name").GetString() == "cce-ldap");
    }

    [Fact]
    public async Task FederationProvider_PutIsIdempotent_OnSecondApply()
    {
        var token = await AcquireMasterAdminTokenAsync();
        var realm = await EnsureRealmExistsAsync(token);

        var body = BuildFederationProviderBody();
        var bodyJson = JsonSerializer.Serialize(body);

        // First apply: POST.
        await _http.PostAsync(
            $"/admin/realms/{realm}/components",
            new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json"));

        // Lookup id.
        var listResp = await _http.GetAsync(
            $"/admin/realms/{realm}/components?type=org.keycloak.storage.UserStorageProvider");
        var components = await listResp.Content.ReadFromJsonAsync<JsonElement[]>();
        var existing = components!.First(c => c.GetProperty("name").GetString() == "cce-ldap");
        var compId = existing.GetProperty("id").GetString()!;
        var parentId = existing.GetProperty("parentId").GetString()!;

        // Second apply: PUT with id + parentId.
        var updateBody = new Dictionary<string, object?>
        {
            ["id"]           = compId,
            ["parentId"]     = parentId,
            ["name"]         = "cce-ldap",
            ["providerId"]   = "ldap",
            ["providerType"] = "org.keycloak.storage.UserStorageProvider",
            ["config"]       = ((Dictionary<string, string[]>)((dynamic)body).config)
        };
        var updateJson = JsonSerializer.Serialize(updateBody);
        var put = await _http.PutAsync(
            $"/admin/realms/{realm}/components/{compId}",
            new StringContent(updateJson, System.Text.Encoding.UTF8, "application/json"));
        put.IsSuccessStatusCode.Should().BeTrue("PUT should be idempotent — no error on re-apply");

        // Verify there's still exactly one cce-ldap component.
        var listResp2 = await _http.GetAsync(
            $"/admin/realms/{realm}/components?type=org.keycloak.storage.UserStorageProvider");
        var components2 = await listResp2.Content.ReadFromJsonAsync<JsonElement[]>();
        components2!.Count(c => c.GetProperty("name").GetString() == "cce-ldap").Should().Be(1);
    }

    [Fact]
    public async Task GroupMapper_AttachesAsChildOfFederationProvider()
    {
        var token = await AcquireMasterAdminTokenAsync();
        var realm = await EnsureRealmExistsAsync(token);

        // Create the parent.
        var parentBody = BuildFederationProviderBody();
        var parentJson = JsonSerializer.Serialize(parentBody);
        await _http.PostAsync(
            $"/admin/realms/{realm}/components",
            new StringContent(parentJson, System.Text.Encoding.UTF8, "application/json"));

        var listResp = await _http.GetAsync(
            $"/admin/realms/{realm}/components?type=org.keycloak.storage.UserStorageProvider");
        var components = await listResp.Content.ReadFromJsonAsync<JsonElement[]>();
        var parentId = components!.First(c => c.GetProperty("name").GetString() == "cce-ldap")
                                  .GetProperty("id").GetString()!;

        // Attach the group mapper.
        var mapperBody = new
        {
            name = "cce-group-mapper",
            providerId = "group-ldap-mapper",
            providerType = "org.keycloak.storage.ldap.mappers.LDAPStorageMapper",
            parentId = parentId,
            config = new Dictionary<string, string[]>
            {
                ["groups.dn"]                              = new[] { _fixture.LdapGroupsDnPublic },
                ["group.name.ldap.attribute"]              = new[] { "cn" },
                ["group.object.classes"]                   = new[] { "group" },
                ["membership.ldap.attribute"]              = new[] { "member" },
                ["membership.attribute.type"]              = new[] { "DN" },
                ["membership.user.ldap.attribute"]         = new[] { "sAMAccountName" },
                ["preserve.group.inheritance"]             = new[] { "false" },
                ["ignore.missing.groups"]                  = new[] { "true" },
                ["user.roles.retrieve.strategy"]           = new[] { "LOAD_GROUPS_BY_MEMBER_ATTRIBUTE" },
                ["mapped.group.attributes"]                = new[] { "" },
                ["mode"]                                   = new[] { "READ_ONLY" },
                ["drop.non.existing.groups.during.sync"]   = new[] { "false" },
                ["groups.path"]                            = new[] { "/" },
            }
        };
        var mapperJson = JsonSerializer.Serialize(mapperBody);
        var post = await _http.PostAsync(
            $"/admin/realms/{realm}/components",
            new StringContent(mapperJson, System.Text.Encoding.UTF8, "application/json"));
        post.IsSuccessStatusCode.Should().BeTrue("POST should attach the group mapper");

        // Verify it's listed under the parent.
        var mappersResp = await _http.GetAsync(
            $"/admin/realms/{realm}/components?type=org.keycloak.storage.ldap.mappers.LDAPStorageMapper&parent={parentId}");
        var mappers = await mappersResp.Content.ReadFromJsonAsync<JsonElement[]>();
        mappers!.Should().Contain(m => m.GetProperty("name").GetString() == "cce-group-mapper");
    }
}
```

**Note on test isolation:** the three tests share the same fixture (Keycloak + OpenLDAP) but each creates state on the realm. Subsequent tests see prior tests' state — that's fine for these tests because the assertions are about "this state exists" not "this is the only state". The `EnsureRealmExistsAsync` helper makes the realm idempotent.

- [ ] **Step 1:** Create `Identity/KeycloakLdapFederationTests.cs` with the contents above.

- [ ] **Step 2:** Build:
  ```bash
  cd /Users/m/CCE/backend && dotnet build tests/CCE.Infrastructure.Tests/ --nologo 2>&1 | tail -8
  ```
  Expected: success.

- [ ] **Step 3:** Run identity tests (Docker required; takes ~30-60 sec for Keycloak boot):
  ```bash
  cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/ --filter "FullyQualifiedName~Identity" --nologo 2>&1 | tail -10
  ```
  Expected: 3 passing.

- [ ] **Step 4:** Run full Infrastructure.Tests suite to confirm no regression:
  ```bash
  cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/ --nologo 2>&1 | tail -3
  ```
  Expected: 66 + 3 = 69 passing (1 skipped from baseline).

- [ ] **Step 5:** Commit:
  ```bash
  git -C /Users/m/CCE add backend/tests/CCE.Infrastructure.Tests/Identity/KeycloakLdapFederationTests.cs
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "test(identity): Keycloak LDAP federation provider + group mapper

  Three tests against the KeycloakLdapFixture:
   - Federation provider creates via POST on a fresh realm.
   - PUT is idempotent on a second apply (component count stays 1).
   - Group mapper attaches as a child via parentId.

  Tests exercise Keycloak's admin REST API directly (the same
  endpoints apply-realm.ps1 hits) — gives fast hermetic coverage
  without invoking PowerShell. apply-realm.ps1 is end-to-end
  exercised by the deploy-smoke.yml workflow on Windows runners.

  Infrastructure.Tests now 69 passing + 1 skipped.
  Sub-10c Phase 01 Task 1.3.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 1.4: ADR-0055 + AD federation runbook

**Files:**
- Create: `docs/adr/0055-ad-federation-via-keycloak-ldap.md` — captures the identity decision (read-only LDAP federation; SPNEGO deferred).
- Create: `docs/runbooks/ad-federation.md` — operator troubleshooting (LDAPS cert validation failures, bind-DN typos, group-mapper attribute mismatches, AD users not appearing in Keycloak).

**Final state of `docs/adr/0055-ad-federation-via-keycloak-ldap.md`:**

```markdown
# ADR-0055 — AD federation via Keycloak LDAP user federation

**Status:** Accepted
**Date:** 2026-05-04
**Deciders:** Sub-10c brainstorm (kilany113@gmail.com)
**Sub-project:** [Sub-10c — Production infra + DR](../../specs/2026-05-04-sub-10c-design.md)

## Context

CCE has used Keycloak as its IdP since Sub-1 (foundation). Both APIs (`Api.External`, `Api.Internal`) authorize via Keycloak roles; backend permissions are enforced via the `permissions.yaml` matrix from CCE.Domain.

IDD v1.2 specifies AD on `cce.local` ports 389/636 (raw LDAP). Sub-10c's task: federate Keycloak with AD so users keep their AD credentials and AD security groups drive Keycloak roles.

## Decision

Use Keycloak's built-in **LDAP user federation provider**, **read-only**, against `ldaps://${LDAP_HOST}:636`. AD security groups → Keycloak roles via Keycloak's group mapper (`group-ldap-mapper`).

**Considered alternatives:**

- **LDAP user federation + Kerberos SSO (SPNEGO):** rejected for Sub-10c. SPNEGO adds AD service-principal + keytab management + AD-side SPN registration — separate ops team usually owns those. Documented as a Sub-10c+ enhancement; the federation provider config supports flipping `allowKerberosAuthentication=true` later without breaking changes.
- **AD FS / Azure AD as an OIDC broker:** rejected. IDD specifies raw LDAP (389/636); brokering through AD FS or Azure AD would introduce a federation hop the IDD doesn't call for and a cloud dependency the IDD doesn't pre-provision.
- **Read-write LDAP federation:** rejected. Keycloak writing to AD requires elevated bind credentials and creates a second source of truth for user data. Read-only is the safe default; user creation stays in AD admin tooling.

**Why read-only:**

- AD is the system of record for users. Keycloak imports + caches; password validation hits AD on each login.
- One-way trust simplifies recovery: a Keycloak failure doesn't corrupt AD.
- Aligns with the "least-privilege bind credential" principle — `cce-keycloak-svc` only needs read on the user/group OUs.

**Group → role mapping** (committed in `infra/keycloak/realm-cce-ldap-federation.json`):

| AD security group | Keycloak role | Backend permission |
|---|---|---|
| `CCE-Admins` | `cce-admin` | full content + admin |
| `CCE-Editors` | `cce-editor` | content edit / publish |
| `CCE-Reviewers` | `cce-reviewer` | content review / approve |
| `CCE-Experts` | `cce-expert` | expert-profile self-service |
| `CCE-Users` | `cce-user` | read-only public surfaces |

The mapping is the contract. AD admins create/manage groups; CCE doesn't write to AD.

## Provisioning

`infra/keycloak/apply-realm.ps1` is the idempotent provisioner. Reads `KEYCLOAK_ADMIN_*` + `LDAP_*` from the env-file, acquires a master-admin token, looks up the federation provider by name, PUTs to update or POSTs to create. Re-runnable; CI tests prove this against a Testcontainers Keycloak.

## Consequences

**Positive:**
- Users keep AD credentials; zero workflow change.
- Group → role mapping is declarative + committed; review-able in PRs.
- Provisioning is idempotent; re-deploys re-apply without duplicating components.
- Read-only stance simplifies the security review (Keycloak can't corrupt AD).
- Path to SPNEGO/Kerberos SSO is open (config flip + keytab provisioning); doesn't require schema changes.

**Negative / accepted:**
- AD service-account password (`LDAP_BIND_PASSWORD`) is a long-lived secret. Rotation is documented in `secret-rotation.md`; Sub-10d may graduate to managed identities.
- LDAPS cert validation requires the AD CA to be trusted by Keycloak's truststore. Operator runbook documents the import procedure.
- AD outage halts new logins (cached tokens still work until expiry). Sub-10c+ HA could mirror federation state, but at single-host scale we accept this.

**Out of scope (Sub-10c+):**
- SPNEGO/Kerberos SSO for AD-joined clients.
- Group-attribute mappers beyond name → role (e.g., dept → tenant).
- AD writes from Keycloak.
- Federation against AD FS or Azure AD.

## References

- [Sub-10c design spec §Identity](../../specs/2026-05-04-sub-10c-design.md#identity--keycloak-ldap-user-federation)
- [AD federation runbook](../runbooks/ad-federation.md)
- [Keycloak User Federation docs](https://www.keycloak.org/docs/latest/server_admin/#_user-storage-federation)
- ADR-0054 — IIS reverse proxy on Windows Server (Sub-10c)
- ADR-0056 — Backup strategy: Ola Hallengren (Sub-10c)
```

**Final state of `docs/runbooks/ad-federation.md`:**

```markdown
# AD federation troubleshooting (Sub-10c)

This runbook covers issues with Keycloak's LDAP user-federation provider and AD security-group → role mapping. Provisioning is via `infra/keycloak/apply-realm.ps1`. ADR-0055 documents the design decisions.

## Smoke check: federation is alive

```powershell
# 1. Re-apply realm config (idempotent).
.\infra\keycloak\apply-realm.ps1 -Environment <env>

# 2. From the Keycloak admin UI: Realms → cce → User Federation → cce-ldap.
#    "Test connection" + "Test authentication" should both succeed.

# 3. Try a real login: open the assistant portal at https://api.CCE/...
#    enter an AD username + password. Expected: success; user appears
#    in Keycloak admin UI under Users (cached on first login).
```

## Common failures

### `Failed to acquire master-admin token`

**Symptom:** `apply-realm.ps1` exits at the first REST call with a 401 / 403.

**Cause:** `KEYCLOAK_ADMIN_USER` / `KEYCLOAK_ADMIN_PASSWORD` in `.env.<env>` don't match what Keycloak expects.

**Fix:**
1. Open Keycloak admin UI directly; verify the master-admin login works.
2. Update `.env.<env>` with the correct values.
3. `validate-env.ps1` then re-run `apply-realm.ps1`.

### `LDAPS connection failed: certificate verification failed`

**Symptom:** Federation provider creation succeeds but "Test connection" fails with a TLS error.

**Cause:** The AD CA's certificate isn't in Keycloak's truststore.

**Fix:**
1. Export the AD CA root cert as DER (from a domain-joined Windows host: `certmgr.msc` → Trusted Root → export the AD CA).
2. Import into Keycloak's truststore. For Keycloak 26.x running as a host service:
   ```powershell
   keytool -import -alias ad-ca -keystore $JAVA_HOME\lib\security\cacerts -file <path-to-AD-CA.cer>
   ```
3. Restart Keycloak.
4. Re-test "Test connection".

If LDAPS isn't possible immediately, fall back to LDAP on port 389 by setting `LDAP_PORT=389` in `.env.<env>` — but only as a temporary measure; AD bind credentials travel in the clear over LDAP.

### `Bind DN authentication failed`

**Symptom:** "Test authentication" in Keycloak admin UI fails.

**Cause:** `LDAP_BIND_DN` or `LDAP_BIND_PASSWORD` is wrong.

**Fix:**
1. Verify the bind account exists and the password is current. Test from a domain-joined host:
   ```powershell
   # Quick LDAP bind test using PowerShell:
   $cred = Get-Credential   # enter the bind DN + password
   New-Object System.DirectoryServices.DirectoryEntry("LDAP://ad.cce.local", $cred.UserName, $cred.GetNetworkCredential().Password) |
       Select-Object -ExpandProperty Name
   ```
   Expected: prints the directory root. Error → wrong creds.
2. Update `.env.<env>` with corrected values.
3. Re-apply.

### `User authenticates but has no roles`

**Symptom:** A user logs in via Keycloak but the backend rejects requests with 403.

**Cause:** Group mapper isn't finding the user's AD groups.

**Diagnosis:**
1. Keycloak admin UI → cce realm → Users → find the user → "Role Mappings". Should list at least one of `cce-admin`, `cce-editor`, etc.
2. If empty: check the user's AD security-group membership (`Get-ADUser <user> -Properties MemberOf`). Should include one of the `CCE-*` groups documented in ADR-0055.
3. If groups exist in AD but not in Keycloak: `LDAP_GROUPS_DN` may be wrong, or the group-name → role mapping is broken.

**Fix:**
1. Verify `LDAP_GROUPS_DN` matches the OU AD admins use (often `OU=Groups,DC=cce,DC=local`).
2. Re-apply realm config; re-trigger sync from Keycloak admin UI.
3. Have the user re-log-in (cached state may need refresh).

### `User exists in AD but doesn't appear in Keycloak`

**Symptom:** Trying to log in produces "user not found".

**Cause:** Federation provider's `usersDn` is too narrow, or sync hasn't run.

**Fix:**
1. Verify `LDAP_USERS_DN` matches the OU containing the user (e.g., `OU=Users,DC=cce,DC=local`).
2. From Keycloak admin UI → User Federation → cce-ldap → "Synchronize all users". Check the sync log for errors.
3. Try logging in directly — first-login triggers an import for that single user even if a full sync hasn't run.

### Re-apply changes nothing

**Symptom:** `apply-realm.ps1` runs cleanly but a config change in the realm JSON doesn't take effect.

**Cause:** Existing component's config is updated in place via PUT, but Keycloak caches federation provider state.

**Fix:**
1. Restart Keycloak after config changes (or wait for the cache TTL).
2. Verify the change: Keycloak admin UI → User Federation → cce-ldap → check the relevant attribute.

## Escalation

If federation fails entirely and a fix isn't obvious within 30 minutes:

1. **Roll back** the deploy that introduced the change (`rollback.ps1 -Environment <env> -ToTag <prev>`).
2. **File an incident.** Include: env name, last-known-good tag, error logs from `C:\ProgramData\CCE\logs\keycloak-apply-<env>-<UTC>.log`, Keycloak server logs.
3. **Escalate to AD admin** if it's an AD-side issue (cert, bind account, group structure).

## See also

- [ADR-0055 — AD federation via Keycloak LDAP](../adr/0055-ad-federation-via-keycloak-ldap.md)
- [`secret-rotation.md`](secret-rotation.md) — `LDAP_BIND_PASSWORD` rotation procedure.
- [Keycloak User Federation docs](https://www.keycloak.org/docs/latest/server_admin/#_user-storage-federation)
- [Sub-10c design spec §Identity](../../specs/2026-05-04-sub-10c-design.md#identity--keycloak-ldap-user-federation)
```

- [ ] **Step 1:** Create `docs/adr/0055-ad-federation-via-keycloak-ldap.md` with the contents above.

- [ ] **Step 2:** Create `docs/runbooks/ad-federation.md` with the contents above.

- [ ] **Step 3:** Commit:
  ```bash
  git -C /Users/m/CCE add docs/adr/0055-ad-federation-via-keycloak-ldap.md docs/runbooks/ad-federation.md
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "docs(sub-10c): ADR-0055 + AD federation troubleshooting runbook

  ADR-0055 documents the identity decision: Keycloak LDAP user
  federation, read-only, with group-mapper translating AD security
  groups to Keycloak roles per the existing permissions.yaml
  matrix. Considered + rejected: SPNEGO/Kerberos SSO, AD FS / Azure
  AD broker, read-write federation. Rationale + path to SPNEGO
  graduation documented.

  ad-federation.md runbook covers the typical failure modes:
  master-admin token failures, LDAPS cert validation, bind-DN
  auth failures, missing role mappings, missing user appearances,
  cache-related re-apply silence. Includes escalation procedure.

  Sub-10c Phase 01 Task 1.4.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Phase 01 close-out

After Task 1.4 commits cleanly:

- [ ] **Run the full check:**
  ```bash
  cd /Users/m/CCE/backend && dotnet build && \
    dotnet test tests/CCE.Application.Tests/ tests/CCE.Infrastructure.Tests/ --nologo
  ```
  Expected: backend build clean; 439 Application + 69 Infrastructure tests passing (1 skipped). Phase 01 adds +3 (KeycloakLdapFederationTests) on top of Phase 00's 66.

- [ ] **Verify CI green** on push: existing `ci.yml` workflows pass; `deploy-smoke.yml` is unchanged in Phase 01.

- [ ] **Hand off to Phase 02.** Phase 02 writes `infra/iis/Configure-IISSites.ps1` + `Install-ARRPrereqs.ps1` + `web.config.template` + `infra/dns-tls/README.md` cert + DNS operator checklist + `smoke.ps1` env-aware HTTPS probes + ADR-0054. Plan file: `phase-02-network-iis-tls.md` (to be written when ready).

**Phase 01 done when:**
- 4 commits land on `main`, each green.
- `infra/keycloak/apply-realm.ps1` provisions the LDAP federation provider + group mapper idempotently.
- `infra/keycloak/realm-cce-ldap-federation.json` declares the federation config + group mapper.
- 3 new identity tests pass against Testcontainers Keycloak + bitnami/openldap.
- ADR-0055 + AD federation runbook committed.
- Test counts: backend Application 439 (unchanged); Infrastructure 69 (was 66, +3 federation tests). Frontend 502.

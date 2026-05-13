# Phase 02 — Keycloak Realms, Clients, Roles, and Seed Users

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Replace the placeholder realm from Phase 01 with the real dual-realm configuration Foundation needs: `cce-internal` (admin/CMS users, ADFS stand-in) + `cce-external` (registered public users). Both realms ship with OIDC clients, Foundation seed roles, ADFS-compatible claim mappers (so swapping Keycloak → real ADFS in sub-project 8 is a config change, not a code change), and one seeded admin user.

**Tasks in this phase:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 01 complete; `docker compose ps` shows all 5 services healthy; `cce-keycloak` running with the Phase 01 placeholder realm loaded; `.env` exists with `KEYCLOAK_CLIENT_SECRET_INTERNAL` and `KEYCLOAK_CLIENT_SECRET_EXTERNAL` populated.

---

## Pre-execution sanity checks (new since Phase 01 plan patches)

Foundation Phase 01 taught us that assumptions about images, command syntax, and hostnames break at execution time. Run these checks before touching anything:

1. **Keycloak version matches plan.** Run: `docker compose exec keycloak /opt/keycloak/bin/kc.sh --version` → must report `25.0.x`. If Keycloak is ≥ 26, realm-import JSON schema may have shifted; stop and report.
2. **Import directory is mounted.** Run: `docker compose exec keycloak ls /opt/keycloak/data/import` → must list `realm-export.json` (the Phase 01 placeholder). If empty, the mount is wrong; stop and report.
3. **Realm currently loaded is the placeholder.** Run: `curl -s http://localhost:8080/realms/cce-placeholder/.well-known/openid-configuration | jq -r .issuer` → must print `http://localhost:8080/realms/cce-placeholder`. If 404, the placeholder didn't import; stop and report.
4. **`.env` has the two dev client secrets.** Run: `grep -E '^KEYCLOAK_CLIENT_SECRET_(INTERNAL|EXTERNAL)=' .env | wc -l` → must print `2`. If not, re-run the `.env` bootstrap from Phase 01 Task 1.2 Step 1.

If any check fails, stop and report to the orchestrator — don't improvise.

---

## Why two realms (not one realm with different client scopes)?

Spec §9.2 keeps the admin/internal user store **physically separate** from the public/external user store:

- `cce-internal` will federate to the ministry's ADFS in prod (sub-project 8). Internal users never exist in the external realm.
- `cce-external` is the customer-facing user store. Registrations land here only.
- A compromised external realm cannot grant admin access — there's no cross-realm trust. Auditors like this.
- Same-session login to both is impossible without deliberate cross-realm federation (not configured).

---

## Task 2.1: Replace placeholder with `cce-internal.json`

**Files:**

- Delete: `keycloak/realm-export.json` (the Phase 01 placeholder)
- Create: `keycloak/cce-internal.json`

**Rationale:** The `cce-internal` realm backs the admin CMS. It seeds a `SuperAdmin` realm role, one `admin@cce.local` user with that role, a confidential OIDC client `cce-admin-cms` (code-flow + PKCE, redirect URIs for localhost:4201), and claim mappers matching ADFS conventions (`upn`, `groups`, `preferred_username`).

**Important:** Keycloak auto-imports every `*.json` file in `/opt/keycloak/data/import` at startup. We'll keep two separate files per realm (simpler diffs than a single multi-realm array) and delete the Phase 01 placeholder so it isn't imported as a third realm.

- [ ] **Step 1: Delete the Phase 01 placeholder**

Run:

```bash
git rm keycloak/realm-export.json
```

Expected: `rm keycloak/realm-export.json` echoed; file deleted from working tree + index.

- [ ] **Step 2: Write `keycloak/cce-internal.json`**

```json
{
  "id": "cce-internal",
  "realm": "cce-internal",
  "displayName": "CCE — Internal (Ministry staff)",
  "displayNameHtml": "<strong>CCE</strong> — Internal",
  "enabled": true,
  "sslRequired": "none",
  "registrationAllowed": false,
  "loginWithEmailAllowed": true,
  "duplicateEmailsAllowed": false,
  "resetPasswordAllowed": true,
  "editUsernameAllowed": false,
  "bruteForceProtected": true,
  "permanentLockout": false,
  "maxFailureWaitSeconds": 900,
  "minimumQuickLoginWaitSeconds": 60,
  "waitIncrementSeconds": 60,
  "quickLoginCheckMilliSeconds": 1000,
  "maxDeltaTimeSeconds": 43200,
  "failureFactor": 5,
  "accessTokenLifespan": 900,
  "accessTokenLifespanForImplicitFlow": 900,
  "ssoSessionIdleTimeout": 900,
  "ssoSessionMaxLifespan": 28800,
  "offlineSessionIdleTimeout": 2592000,
  "accessCodeLifespan": 60,
  "accessCodeLifespanUserAction": 300,
  "accessCodeLifespanLogin": 1800,
  "actionTokenGeneratedByAdminLifespan": 43200,
  "actionTokenGeneratedByUserLifespan": 300,
  "passwordPolicy": "length(10) and upperCase(1) and lowerCase(1) and digits(1) and specialChars(1) and notUsername()",

  "roles": {
    "realm": [
      {
        "name": "SuperAdmin",
        "description": "Foundation seed — full administrative access. Permission matrix lands in sub-project 2.",
        "composite": false,
        "clientRole": false,
        "containerId": "cce-internal",
        "attributes": {}
      }
    ]
  },

  "defaultRoles": [],

  "users": [
    {
      "username": "admin@cce.local",
      "email": "admin@cce.local",
      "emailVerified": true,
      "enabled": true,
      "firstName": "Foundation",
      "lastName": "Admin",
      "credentials": [
        {
          "type": "password",
          "value": "Admin123!@",
          "temporary": false
        }
      ],
      "realmRoles": ["SuperAdmin"],
      "requiredActions": [],
      "attributes": {
        "upn": ["admin@cce.local"],
        "department": ["IT"]
      }
    }
  ],

  "clientScopes": [
    {
      "name": "adfs-compat",
      "description": "Claim mappers matching ADFS conventions — swapping to real ADFS is a config change",
      "protocol": "openid-connect",
      "attributes": {
        "include.in.token.scope": "true",
        "display.on.consent.screen": "false"
      },
      "protocolMappers": [
        {
          "name": "upn",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-usermodel-attribute-mapper",
          "consentRequired": false,
          "config": {
            "userinfo.token.claim": "true",
            "user.attribute": "upn",
            "id.token.claim": "true",
            "access.token.claim": "true",
            "claim.name": "upn",
            "jsonType.label": "String"
          }
        },
        {
          "name": "preferred_username",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-usermodel-property-mapper",
          "consentRequired": false,
          "config": {
            "userinfo.token.claim": "true",
            "user.attribute": "username",
            "id.token.claim": "true",
            "access.token.claim": "true",
            "claim.name": "preferred_username",
            "jsonType.label": "String"
          }
        },
        {
          "name": "groups",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-usermodel-realm-role-mapper",
          "consentRequired": false,
          "config": {
            "multivalued": "true",
            "userinfo.token.claim": "true",
            "id.token.claim": "true",
            "access.token.claim": "true",
            "claim.name": "groups",
            "jsonType.label": "String"
          }
        },
        {
          "name": "email",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-usermodel-property-mapper",
          "consentRequired": false,
          "config": {
            "userinfo.token.claim": "true",
            "user.attribute": "email",
            "id.token.claim": "true",
            "access.token.claim": "true",
            "claim.name": "email",
            "jsonType.label": "String"
          }
        }
      ]
    }
  ],

  "defaultDefaultClientScopes": ["adfs-compat", "web-origins", "profile", "email", "roles"],

  "clients": [
    {
      "clientId": "cce-admin-cms",
      "name": "CCE Admin CMS (Angular)",
      "description": "Angular admin portal at localhost:4201 in dev; backed by CCE.Api.Internal",
      "enabled": true,
      "clientAuthenticatorType": "client-secret",
      "secret": "dev-internal-secret-change-me",
      "redirectUris": ["http://localhost:4201/*"],
      "webOrigins": ["http://localhost:4201"],
      "notBefore": 0,
      "bearerOnly": false,
      "consentRequired": false,
      "standardFlowEnabled": true,
      "implicitFlowEnabled": false,
      "directAccessGrantsEnabled": false,
      "serviceAccountsEnabled": true,
      "publicClient": false,
      "frontchannelLogout": true,
      "protocol": "openid-connect",
      "attributes": {
        "oauth2.device.authorization.grant.enabled": "false",
        "backchannel.logout.session.required": "true",
        "backchannel.logout.revoke.offline.tokens": "false",
        "pkce.code.challenge.method": "S256",
        "use.refresh.tokens": "true",
        "tls.client.certificate.bound.access.tokens": "false"
      },
      "fullScopeAllowed": false,
      "defaultClientScopes": ["adfs-compat", "web-origins", "profile", "email", "roles"],
      "optionalClientScopes": ["address", "phone", "offline_access"]
    }
  ],

  "internationalizationEnabled": true,
  "supportedLocales": ["ar", "en"],
  "defaultLocale": "ar"
}
```

Note on the secret `dev-internal-secret-change-me`: matches the Gitleaks allowlist (`security/gitleaks.toml` from Phase 00 Task 0.5) so it doesn't trip the pre-commit hook.

- [ ] **Step 3: Validate JSON syntax**

Run:

```bash
jq empty keycloak/cce-internal.json && echo "OK"
```

Expected: prints `OK`. (`jq` is bundled with macOS via Xcode CLI tools; `brew install jq` if missing.)

- [ ] **Step 4: Commit**

```bash
git add keycloak/cce-internal.json keycloak/realm-export.json
git -c commit.gpgsign=false commit -m "feat(phase-02): add cce-internal realm (SuperAdmin role, admin@cce.local seed, ADFS-compat mappers)"
```

---

## Task 2.2: Add `cce-external.json` realm

**Files:**

- Create: `keycloak/cce-external.json`

**Rationale:** The `cce-external` realm backs public registered users. Self-registration is **enabled** here (unlike internal), MFA is hook-ready but not enforced in Foundation, and the client is a **public client** (Angular SPA uses code-flow + PKCE without a secret). Seeds a `RegisteredUser` realm role. No users seeded — registration comes online in sub-project 4.

- [ ] **Step 1: Write `keycloak/cce-external.json`**

```json
{
  "id": "cce-external",
  "realm": "cce-external",
  "displayName": "CCE — External (Public)",
  "displayNameHtml": "<strong>CCE</strong> — Public",
  "enabled": true,
  "sslRequired": "none",
  "registrationAllowed": true,
  "registrationEmailAsUsername": true,
  "rememberMe": true,
  "verifyEmail": false,
  "loginWithEmailAllowed": true,
  "duplicateEmailsAllowed": false,
  "resetPasswordAllowed": true,
  "editUsernameAllowed": false,
  "bruteForceProtected": true,
  "permanentLockout": false,
  "maxFailureWaitSeconds": 900,
  "minimumQuickLoginWaitSeconds": 60,
  "waitIncrementSeconds": 60,
  "quickLoginCheckMilliSeconds": 1000,
  "maxDeltaTimeSeconds": 43200,
  "failureFactor": 5,
  "accessTokenLifespan": 900,
  "ssoSessionIdleTimeout": 900,
  "ssoSessionMaxLifespan": 28800,
  "offlineSessionIdleTimeout": 2592000,
  "passwordPolicy": "length(10) and upperCase(1) and lowerCase(1) and digits(1) and specialChars(1) and notUsername()",

  "roles": {
    "realm": [
      {
        "name": "RegisteredUser",
        "description": "Foundation seed — baseline role for any publicly registered user.",
        "composite": false,
        "clientRole": false,
        "containerId": "cce-external",
        "attributes": {}
      },
      {
        "name": "StateRepresentative",
        "description": "Foundation seed — state representatives (BRD §4.1.26). Delegation flow in sub-project 4.",
        "composite": false,
        "clientRole": false,
        "containerId": "cce-external",
        "attributes": {}
      },
      {
        "name": "CommunityExpert",
        "description": "Foundation seed — registered community experts (BRD §6.2.17). Expert flow in sub-project 4.",
        "composite": false,
        "clientRole": false,
        "containerId": "cce-external",
        "attributes": {}
      }
    ]
  },

  "defaultRoles": ["RegisteredUser"],

  "users": [],

  "clientScopes": [
    {
      "name": "adfs-compat",
      "description": "Claim mappers matching ADFS conventions (symmetry with cce-internal)",
      "protocol": "openid-connect",
      "attributes": {
        "include.in.token.scope": "true",
        "display.on.consent.screen": "false"
      },
      "protocolMappers": [
        {
          "name": "preferred_username",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-usermodel-property-mapper",
          "consentRequired": false,
          "config": {
            "userinfo.token.claim": "true",
            "user.attribute": "username",
            "id.token.claim": "true",
            "access.token.claim": "true",
            "claim.name": "preferred_username",
            "jsonType.label": "String"
          }
        },
        {
          "name": "groups",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-usermodel-realm-role-mapper",
          "consentRequired": false,
          "config": {
            "multivalued": "true",
            "userinfo.token.claim": "true",
            "id.token.claim": "true",
            "access.token.claim": "true",
            "claim.name": "groups",
            "jsonType.label": "String"
          }
        },
        {
          "name": "email",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-usermodel-property-mapper",
          "consentRequired": false,
          "config": {
            "userinfo.token.claim": "true",
            "user.attribute": "email",
            "id.token.claim": "true",
            "access.token.claim": "true",
            "claim.name": "email",
            "jsonType.label": "String"
          }
        }
      ]
    }
  ],

  "defaultDefaultClientScopes": ["adfs-compat", "web-origins", "profile", "email", "roles"],

  "clients": [
    {
      "clientId": "cce-web-portal",
      "name": "CCE Web Portal (Angular)",
      "description": "Public Angular app at localhost:4200 in dev; backed by CCE.Api.External",
      "enabled": true,
      "clientAuthenticatorType": "client-secret",
      "secret": "dev-external-secret-change-me",
      "redirectUris": ["http://localhost:4200/*"],
      "webOrigins": ["http://localhost:4200"],
      "notBefore": 0,
      "bearerOnly": false,
      "consentRequired": false,
      "standardFlowEnabled": true,
      "implicitFlowEnabled": false,
      "directAccessGrantsEnabled": false,
      "serviceAccountsEnabled": false,
      "publicClient": true,
      "frontchannelLogout": true,
      "protocol": "openid-connect",
      "attributes": {
        "oauth2.device.authorization.grant.enabled": "false",
        "backchannel.logout.session.required": "true",
        "pkce.code.challenge.method": "S256",
        "use.refresh.tokens": "true",
        "post.logout.redirect.uris": "http://localhost:4200/*"
      },
      "fullScopeAllowed": false,
      "defaultClientScopes": ["adfs-compat", "web-origins", "profile", "email", "roles"],
      "optionalClientScopes": ["address", "phone", "offline_access"]
    }
  ],

  "internationalizationEnabled": true,
  "supportedLocales": ["ar", "en"],
  "defaultLocale": "ar"
}
```

Notes:

- **Public client** (`"publicClient": true`) — Angular SPA speaks OIDC code-flow with PKCE, no secret required client-side. The `secret` field is kept for symmetry with the admin realm and matches the `.env` value, but with `publicClient: true` Keycloak does not enforce it for auth.
- **`directAccessGrantsEnabled: false`** — blocks the Resource Owner Password Credentials flow (never use in production).

- [ ] **Step 2: Validate JSON**

Run:

```bash
jq empty keycloak/cce-external.json && echo "OK"
```

Expected: `OK`.

- [ ] **Step 3: Commit**

```bash
git add keycloak/cce-external.json
git -c commit.gpgsign=false commit -m "feat(phase-02): add cce-external realm (RegisteredUser, StateRepresentative, CommunityExpert seed roles; PKCE public client)"
```

---

## Task 2.3: Recreate Keycloak and verify both realms import

**Files:**

- Modify: none (just restart)

**Rationale:** Keycloak only reads the import directory at **container start** — editing mounted JSON has no effect on a running container. We recreate the container so the new realm JSONs are imported, and we **drop the `keycloak-data` volume** to wipe the Phase 01 placeholder realm from the embedded H2 database. (Subsequent restarts without `-v` will reuse the realms from the DB.)

- [ ] **Step 1: Recreate keycloak with a clean data volume**

Run:

```bash
# Stop and remove the container and its named volume (keeps other services running)
docker compose stop keycloak
docker compose rm -f keycloak
docker volume rm cce_keycloak-data
docker compose up -d keycloak
echo "Waiting up to 90s for Keycloak to boot and import both realms…"
for i in $(seq 1 18); do
  hstatus=$(docker inspect --format '{{.State.Health.Status}}' cce-keycloak 2>/dev/null || echo "starting")
  echo "attempt $i: $hstatus"
  [ "$hstatus" = "healthy" ] && break
  sleep 5
done
docker compose ps keycloak
```

Expected: `cce-keycloak ... (healthy)` within 90 seconds.

- [ ] **Step 2: Confirm `cce-internal` realm is loaded**

Run:

```bash
curl -s http://localhost:8080/realms/cce-internal/.well-known/openid-configuration | jq -r '{issuer, authorization_endpoint, token_endpoint}'
```

Expected: prints a JSON object with:

- `issuer`: `http://localhost:8080/realms/cce-internal`
- `authorization_endpoint`: `http://localhost:8080/realms/cce-internal/protocol/openid-connect/auth`
- `token_endpoint`: `http://localhost:8080/realms/cce-internal/protocol/openid-connect/token`

- [ ] **Step 3: Confirm `cce-external` realm is loaded**

Run:

```bash
curl -s http://localhost:8080/realms/cce-external/.well-known/openid-configuration | jq -r '{issuer, authorization_endpoint, token_endpoint}'
```

Expected: same shape but for `cce-external`.

- [ ] **Step 4: Confirm the Phase 01 placeholder realm is gone**

Run:

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/realms/cce-placeholder/.well-known/openid-configuration
```

Expected: prints `404`.

- [ ] **Step 5: (No file changes — nothing to commit in this task)**

Skip the commit step. Proceed to Task 2.4.

---

## Task 2.4: Smoke-test seeded admin user + service-account token + external discovery

**Files:**

- Modify: none

**Rationale:** Prove the realm import seeded exactly what Foundation needs: (a) `admin@cce.local` exists with `SuperAdmin` role attached, (b) `cce-admin-cms` can mint a service-account token, (c) `cce-external` exposes the public registration endpoint.

**What we're NOT doing in Foundation:** decoding a user JWT to assert `upn` / `groups` claim mappers. Keycloak's built-in `admin-cli` client (the only pre-existing client with `directAccessGrantsEnabled: true`) is imported **without** our custom `adfs-compat` scope — adding it via JSON requires overriding a built-in (fragile) or introducing a dev-only smoke-test client (extra surface). Our `cce-admin-cms` client is correctly configured with `adfs-compat` as a default scope but deliberately has password-grant disabled (security posture per spec §9.2). Claim-mapper verification lands naturally in **Phase 08** when the .NET APIs exercise the authorization-code + PKCE flow that real users will use. For Foundation, proving the seed data is in place is sufficient.

- [ ] **Step 1: Get an admin-API token (Keycloak's bootstrap admin, not our seed user)**

```bash
ADMIN_TOKEN=$(curl -s -X POST \
  "http://localhost:8080/realms/master/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=admin-cli" \
  -d "username=admin" \
  -d "password=admin" \
  | jq -r .access_token)
echo "Admin token length: ${#ADMIN_TOKEN}"
[ -n "$ADMIN_TOKEN" ] && [ "$ADMIN_TOKEN" != "null" ] && echo "Admin API login OK" || { echo "ADMIN LOGIN FAILED"; exit 1; }
```

Expected: prints `Admin token length: <big number>` and `Admin API login OK`. (The master-realm `admin/admin` credentials come from the `KC_BOOTSTRAP_ADMIN_USERNAME`/`PASSWORD` env vars set in `docker-compose.yml` Phase 01 Task 1.4 — these are Keycloak's platform admin, distinct from our seeded `admin@cce.local`.)

- [ ] **Step 2: Verify `admin@cce.local` exists in `cce-internal`**

```bash
USER_JSON=$(curl -s \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  "http://localhost:8080/admin/realms/cce-internal/users?username=admin@cce.local&exact=true")
echo "$USER_JSON" | jq '.[] | {id, username, email, enabled, emailVerified, firstName, lastName}'
USER_ID=$(echo "$USER_JSON" | jq -r '.[0].id')
[ -n "$USER_ID" ] && [ "$USER_ID" != "null" ] && echo "User found: $USER_ID" || { echo "USER NOT FOUND"; exit 1; }
```

Expected: JSON object with `username: admin@cce.local`, `enabled: true`, `emailVerified: true`, and a non-null UUID id.

- [ ] **Step 3: Verify `SuperAdmin` role is assigned to `admin@cce.local`**

```bash
ROLES_JSON=$(curl -s \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  "http://localhost:8080/admin/realms/cce-internal/users/$USER_ID/role-mappings/realm")
echo "$ROLES_JSON" | jq -r '.[].name' | sort
echo "$ROLES_JSON" | jq -e '.[] | select(.name == "SuperAdmin")' >/dev/null \
  && echo "SuperAdmin role assigned" \
  || { echo "SUPERADMIN ROLE MISSING"; exit 1; }
```

Expected: the role list contains `SuperAdmin` (plus Keycloak's `default-roles-cce-internal`).

- [ ] **Step 4: Acquire a service-account token for `cce-admin-cms` and inspect its claims**

```bash
SECRET=$(grep '^KEYCLOAK_CLIENT_SECRET_INTERNAL=' .env | cut -d= -f2-)
SATOKEN=$(curl -s -X POST \
  "http://localhost:8080/realms/cce-internal/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=cce-admin-cms" \
  -d "client_secret=${SECRET}" \
  | jq -r .access_token)
[ -n "$SATOKEN" ] && [ "$SATOKEN" != "null" ] && echo "Service-account OK" || { echo "SERVICE ACCOUNT FAILED"; exit 1; }

# Decode and inspect (pad base64url to valid base64 length)
PAYLOAD=$(echo "$SATOKEN" | awk -F. '{print $2}' | awk '{n=length($0)%4; if (n==2) print $0"=="; else if (n==3) print $0"="; else print}' | base64 -d 2>/dev/null || true)
echo "$PAYLOAD" | jq '{iss, aud, azp, clientId, scope}'
```

Expected: `Service-account OK`, and the decoded payload shows `iss` = `http://localhost:8080/realms/cce-internal`, `azp` = `cce-admin-cms`, `scope` containing at least `adfs-compat` (confirms our client scope wiring worked for real clients, even though `admin-cli` doesn't have it).

- [ ] **Step 5: Confirm external realm's public registration endpoint is reachable**

```bash
curl -s http://localhost:8080/realms/cce-external/.well-known/openid-configuration | jq -r .registration_endpoint
```

Expected: prints `http://localhost:8080/realms/cce-external/clients-registrations/openid-connect`.

- [ ] **Step 6: Deferred — note claim-mapper testing target**

No runtime action. Just acknowledge: Phase 08 integration tests will decode a real user-flow JWT (code flow + PKCE) and assert `upn` / `groups: [SuperAdmin]` at that point. Add a note to your phase report if Step 4's `scope` didn't include `adfs-compat` — that would signal a realm JSON problem worth catching early.

- [ ] **Step 7: (No file changes — nothing to commit)**

Skip the commit step.

---

## Phase 02 — completion checklist

- [ ] `keycloak/realm-export.json` (Phase 01 placeholder) deleted from git.
- [ ] `keycloak/cce-internal.json` committed with: `SuperAdmin` role, `admin@cce.local` user, `cce-admin-cms` confidential client, ADFS-compat claim mappers (`upn`, `preferred_username`, `groups`, `email`).
- [ ] `keycloak/cce-external.json` committed with: `RegisteredUser`/`StateRepresentative`/`CommunityExpert` roles, no users, `cce-web-portal` public client (PKCE), ADFS-compat mappers minus `upn` (external users don't have UPN).
- [ ] Keycloak container recreated from a wiped volume; the placeholder realm is gone (`/realms/cce-placeholder` → 404).
- [ ] Both real realms load (`/realms/cce-internal/.well-known/openid-configuration` and same for `cce-external`).
- [ ] `admin@cce.local` exists in `cce-internal` (verified via Keycloak admin API).
- [ ] `SuperAdmin` role is assigned to `admin@cce.local` (verified via role-mappings endpoint).
- [ ] `cce-admin-cms` obtains a service-account token via `client_credentials`.
- [ ] `cce-admin-cms` service-account token payload contains `scope` including `adfs-compat` — confirms custom client-scope wiring works for real clients.
- [ ] `cce-external` realm exposes `/clients-registrations/openid-connect` registration endpoint.
- [ ] Claim-mapper end-to-end verification (JWT `upn` + `groups: [SuperAdmin]`) is deferred to Phase 08 where real OIDC code flow exercises it.
- [ ] `git log --oneline | head -5` shows 2 new Phase-02 commits + 1 fix commit for the password (Task 2.3 and 2.4 don't write files).
- [ ] `git status` shows clean tree.

**If all boxes ticked, phase 02 is complete. Proceed to phase 03 (.NET solution skeleton).**

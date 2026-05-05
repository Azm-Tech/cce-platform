# CCE Entra ID provisioning

Sub-11 Phase 02 ships two PowerShell 7 scripts that provision CCE's multi-tenant Entra ID app registration plus optional company branding on the sign-in page.

## Scripts

### `apply-app-registration.ps1`

Idempotently creates or updates the **CCE Knowledge Center** app registration with:
- 5 app roles (`cce-admin`, `cce-editor`, `cce-reviewer`, `cce-expert`, `cce-user`)
- 10 OIDC redirect URIs (2 BFFs × 4 envs + 2 localhost)
- 3 Graph permissions (`User.ReadWrite.All` app, `User.Read.All` app, `User.Read` delegated)
- Multi-tenant signInAudience (`AzureADMultipleOrgs`)

Re-runs PATCH the existing app — safe to run on every deploy.

### `Configure-Branding.ps1`

Uploads CCE-branded `banner.png`, `square.png`, `background.png`, and `custom.css` to Entra ID's organizationalBranding endpoint. Renders **only for CCE-tenant users**; partner-tenant users see their own home-tenant branding.

Requires Entra ID P1 or P2 SKU. Without it, the script logs a warning and exits 0.

## Prerequisites

Both scripts require:
- PowerShell 7+
- `Microsoft.Graph` PS module 2.x (`Install-Module Microsoft.Graph`)
- A **provisioner** Entra ID app registration with `Application.ReadWrite.All` + `Organization.ReadWrite.All` admin-consented Graph application permissions. **Do not** reuse the runtime CCE app for this — split privilege.
- Env-file at `C:\ProgramData\CCE\.env.<env>` containing the `ENTRA_*` and `HOSTNAME_*` keys (see `.env.<env>.example`).

## One-time provisioner setup (per Entra ID tenant)

1. In the Entra ID portal → **App registrations** → **New registration**:
   - Name: `CCE Provisioner`
   - Supported account types: **Single tenant** (this tenant only).
2. **Certificates & secrets** → **New client secret** → save the value into `ENTRA_PROVISIONER_CLIENT_SECRET` in the env-file.
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
| 03 | Frontend swaps OIDC config to point at the registered app; backend `RoleToPermissionClaimsTransformer` rewritten to consume the `roles` claim. |
| 04 | Cutover runbook deletes Keycloak, flips `MIGRATE_*` keys, deletes `infra/keycloak/`. |

## Troubleshooting

- **`AAD_TenantThrottleLimit_<n>`** — Graph rate-limit on app PATCH. Wait 5 min and retry; the script is idempotent.
- **`Authorization_RequestDenied` on app PATCH** — provisioner missing `Application.ReadWrite.All` admin consent.
- **`Authorization_RequestDenied` on branding** — provisioner missing `Organization.ReadWrite.All` admin consent.
- **Branding doesn't render after script success** — Entra ID caches branding for ~1 hour; users may need to clear cookies.

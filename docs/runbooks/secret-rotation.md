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

# Entra ID cutover runbook (Sub-11)

Maintenance-window procedure for swapping a CCE environment from Keycloak to multi-tenant Entra ID. Run **per env** in this order: test → preprod → prod → dr.

**Estimated downtime per env:** 15–30 minutes.

**Rollback:** revert to the prior `app-v*.*.*` image tag via `deploy.ps1 -Environment <env> -Rollback` (Sub-10b). The migration `AddEntraIdObjectIdToUser` is forward-only-friendly (additive nullable column + filtered unique index); old images ignore the column.

## Prerequisites

- Phase 02 PowerShell scripts (`apply-app-registration.ps1` + `Configure-Branding.ps1`) have been run successfully against the CCE Entra ID tenant.
- The runtime CCE app's `ENTRA_CLIENT_ID` / `ENTRA_CLIENT_SECRET` / `ENTRA_TENANT_ID` are populated in `C:\ProgramData\CCE\.env.<env>`.
- A Conditional Access policy targeting the CCE app is configured (per ADR-0060).
- Outbound HTTPS to `login.microsoftonline.com` and `graph.microsoft.com` is whitelisted on the deploy host.

## Steps

1. **Pre-cutover snapshot.** Capture the current SQL Server state via `infra/backup/Test-BackupChain.ps1 -Environment <env>` and confirm the latest full backup is on the off-host UNC share.

2. **Operator check-in.** Post a `#cce-ops` Slack message announcing the maintenance window (start time + estimated end). Set the environment's status page (if any) to "Maintenance".

3. **Halt traffic.** On the deploy host: `iisreset /stop`. The IIS reverse proxy stops accepting traffic; ARR rules return 503.

4. **Deploy the cutover image.** From the repo root on the deploy host:
   ```powershell
   .\deploy\deploy.ps1 -Environment <env> -ImageTag entra-id-v1.0.0
   ```
   This pulls the Sub-11 Phase 04 backend image, runs the EF migration `AddEntraIdObjectIdToUser` (no-op if previously applied), and restarts the app containers.

5. **Verify migration applied.** SSMS:
   ```sql
   SELECT TOP 1 name FROM sys.columns
     WHERE object_id = OBJECT_ID('[identity].[Users]')
       AND name = 'entra_id_object_id';
   ```
   Expected: one row.

6. **Resume traffic.** `iisreset /start`. ARR resumes proxying; smoke probes (`https://<hostname>/health/ready`) should return 200 within 30 s.

7. **Backfill objectId for existing users.** `EntraIdUserResolver` does this lazily on first sign-in per user. No batch step needed on cutover day, but operators may run a manual sync via the `/api/admin/users/sync` endpoint (Sub-11d work — defer if not yet shipped).

8. **Smoke-test sign-in.** From a fresh browser:
   - Navigate to `https://<portal-hostname>/`
   - Click Sign In
   - Verify redirect to `login.microsoftonline.com/<tenant>/oauth2/v2.0/authorize`
   - Complete Entra ID login (with MFA if CA policy demands it)
   - Verify return to `/me/profile` with permissions resolved

9. **Smoke-test admin CMS.** Same flow against `https://<cms-hostname>/`.

10. **Verify Conditional Access.** Sign-in attempt **without** MFA (use a test account with conditional access disabled per-account via Entra ID portal): MFA prompt should fire. With MFA satisfied: sign-in completes.

11. **Decommission Keycloak (deferred — operator's call):** Stop the Keycloak container/service. Operators may keep it running for a 7-day rollback window before fully decommissioning.

12. **Operator check-out.** Post completion message to `#cce-ops`. Mark status page back to "Operational". Add an entry to the deploy-history TSV for the env.

## Rollback

If steps 5–10 surface an issue that can't be fixed within the maintenance window:

```powershell
.\deploy\rollback.ps1 -Environment <env>
```

This reverts to the prior image tag. Keycloak is still running (per step 11 deferral). Sign-in flows through Keycloak again. The `entra_id_object_id` column stays in the schema (forward-only-friendly migration); old images simply ignore it.

Post the rollback to `#cce-ops`. Open a follow-up issue documenting what failed; do not retry until the issue is resolved in a code change.

## See also

- `entra-id-troubleshooting.md` — common failure modes
- `infra/entra/README.md` — provisioning script reference
- ADR-0058, ADR-0059, ADR-0060

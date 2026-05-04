# DR-host provisioning checklist (Sub-10c)

One-time setup for the DR host. Do this **before** any disaster, not during. The DR host stays cold (no apps running, no DNS pointing to it) until [`dr-promotion.md`](../../docs/runbooks/dr-promotion.md) is invoked.

Estimated time: 4-8 hours across multiple ops teams (Windows, AD, DNS, backup, network).

## Pre-requisites

- [ ] DR site identified (different physical location or AZ from prod).
- [ ] Hardware sized to match prod: Intel Xeon Gold 6138 (per IDD v1.2) or equivalent.
- [ ] Network connectivity DR ↔ prod (for off-host backup sync).
- [ ] DR DNS zone delegated; can create A records.
- [ ] Backup-store UNC share reachable from DR host's IP.
- [ ] Sentry project for DR env exists (`cce-dr` per ADR-0052; or operator decision per local Sentry policy).

## Windows Server 2022 base

- [ ] Server installed: Windows Server 2022 Standard or Datacenter.
- [ ] All security updates applied.
- [ ] Time-sync configured against domain time source.
- [ ] Firewall rules: inbound 443 from client subnets; outbound 443 to ghcr.io + login.microsoftonline.com (Sub-11) + Sentry.
- [ ] Domain-join to `cce.local`.

## Docker

- [ ] Docker (Desktop / CE / Mirantis runtime) installed.
- [ ] `docker compose version` returns v2.20+.
- [ ] Linux containers enabled (default).
- [ ] `cce-deploy` Windows account added to `docker-users` group.

## SQL Server

- [ ] SQL Server installed (matching prod's edition + major version).
- [ ] Mixed-mode auth enabled (per IDD v1.2 — both AD-auth and SQL-auth in scope).
- [ ] `cce_app` SQL login created with same password convention as prod.
- [ ] Recovery model: `FULL` (set on DB after restore).
- [ ] Default backup directory: `D:\CCEBackups\`.
- [ ] Service account configured for SQL Agent: `cce.local\cce-sqlbackup-svc`.

## Redis

- [ ] Redis 6+ installed (per IDD v1.2 — port 6379).
- [ ] Listening on `localhost:6379` only (not exposed externally).

## Keycloak

- [ ] Keycloak 26.0 installed as a Windows service.
- [ ] Master admin account configured.
- [ ] `cce` realm imported via `apply-realm.ps1 -Environment dr` (after `.env.dr` is populated).
- [ ] LDAP federation against `ad.cce.local:636` provisioned.
- [ ] Test login as a known AD user — verify cached in Keycloak admin UI.

> **Sub-11 note**: this step changes when Keycloak is replaced by Entra ID. Re-issue this checklist as part of Sub-11 close-out.

## IIS reverse proxy

- [ ] `Install-ARRPrereqs.ps1` run (idempotent).
- [ ] Cert in store (one of the 3 paths from `infra/dns-tls/README.md`).
- [ ] `.env.dr` populated with `IIS_CERT_THUMBPRINT` or `IIS_CERT_PFX_PATH` + `IIS_HOSTNAMES`.
- [ ] `Configure-IISSites.ps1 -Environment dr` run; 4 IIS sites visible in IIS Manager.

## Env-file

- [ ] `C:\ProgramData\CCE\.env.dr` copied from `.env.dr.example` and filled in.
  - DB connection: DR host's local SQL.
  - Keycloak authority: `https://api.cce-dr/auth/realms/cce`.
  - Image tag: mirrors prod's current tag.
  - `AUTO_ROLLBACK=false` (DR is operator-driven only).
  - `SENTRY_ENVIRONMENT=dr`, `SENTRY_DSN`, `SENTRY_RELEASE` populated.
  - Backup UNC pointing at the same destination as prod (for restore-from-prod-backups workflow).
- [ ] `validate-env.ps1 -EnvFile C:\ProgramData\CCE\.env.dr -Environment dr` returns OK.
- [ ] ACL-locked: `icacls C:\ProgramData\CCE\.env.dr /inheritance:r /grant:r "Administrators:R" "<deploy-user>:R"`.

## Backup chain (cold)

- [ ] `Install-OlaHallengren.ps1 -Environment dr` run (installs maint solution into master).
- [ ] **Scheduled tasks NOT registered** while DR is cold (DR host doesn't generate its own backups until promoted).
- [ ] `cmdkey /add:${BACKUP_UNC_HOST}` cached for the deploy user (read-only access; DR pulls from prod's backup destination).
- [ ] `D:\CCEBackups\restored\` directory exists.

## DNS

- [ ] DR-environment hostnames provisioned with **low TTL (60s)**:
  - `cce-ext-dr` → DR host IP
  - `cce-admin-panel-dr` → DR host IP
  - `api.cce-dr` → DR host IP
  - `api.cce-admin-panel-dr` → DR host IP
- [ ] **Production hostnames NOT yet pointing at DR** — those are managed by [`dr-promotion.md`](../../docs/runbooks/dr-promotion.md) Step 6 during failover.

## Cold-state validation

Run quarterly against the DR host:

```powershell
# 1. Apps respond on DR hostnames.
.\deploy\smoke.ps1 -Environment dr -AllowSelfSignedCert

# 2. Backup-chain reachable.
robocopy "\\${BACKUP_UNC_HOST}\${BACKUP_UNC_SHARE}\prod" "D:\CCEBackups\restored" /L /R:1
# (/L = list-only mode; verifies UNC is reachable without copying)

# 3. Restore-FromBackup.ps1 dry-run against a test DB.
$full = (Get-ChildItem D:\CCEBackups\restored\FULL\*.bak | Sort LastWriteTime | Select -Last 1).FullName
.\infra\backup\Restore-FromBackup.ps1 -FullBackup $full -TargetDb CCE_drilltest -Environment dr
sqlcmd -S localhost -d CCE_drilltest -Q "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId"
sqlcmd -S localhost -Q "DROP DATABASE CCE_drilltest"
```

Record the result in the ops drill log.

## Done when

- [ ] All checklist items above are checked.
- [ ] Cold-state validation passes.
- [ ] Operator + ops lead sign off in the ops runbook log.

## See also

- [DR-promotion runbook](../../docs/runbooks/dr-promotion.md) — invoked when DR is needed.
- [Backup-restore runbook](../../docs/runbooks/backup-restore.md) — DR-host cold-start restore procedure.
- [Sub-10c design spec §DR](../../docs/superpowers/specs/2026-05-04-sub-10c-design.md#dr-procedure)

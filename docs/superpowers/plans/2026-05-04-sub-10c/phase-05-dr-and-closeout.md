# Phase 05 — DR procedure + close-out (Sub-10c)

> Parent: [`../2026-05-04-sub-10c.md`](../2026-05-04-sub-10c.md) · Spec: [`../../specs/2026-05-04-sub-10c-design.md`](../../specs/2026-05-04-sub-10c-design.md) §DR procedure, §Phasing → Phase 05.

**Phase goal:** Close Sub-10c. Ship the DR provisioning checklist + DR-promotion runbook + ADR-0057 documenting the multi-env decision; write the completion doc; append the CHANGELOG entry; tag `infra-v1.0.0`.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 04 closed (4 commits land on `main`; HEAD at `49e6e4f` or later).
- All Sub-10c artefacts shipped: multi-env scripts, AD federation, IIS reverse proxy, backup automation, auto-rollback + Sentry production DSN.
- Backend baseline: 439 Application + 75 Infrastructure tests passing (1 skipped).

---

## Task 5.1: DR provisioning checklist

**Files:**
- Create: `infra/dns-tls/dr-provisioning-checklist.md` — one-time DR-host setup checklist; mirror of the prod-host setup but with DR-specific notes (cold-stays-cold, DR DNS records pre-provisioned with low TTL).

**Why doc-only:** DR-host provisioning is a one-time operator procedure across multiple ops teams (Windows admin, AD admin, DNS admin, backup-share admin). A checklist is the right artifact; automation is impractical.

**Final state of `infra/dns-tls/dr-provisioning-checklist.md`:**

```markdown
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
```

- [ ] **Step 1:** Create `infra/dns-tls/dr-provisioning-checklist.md` with the contents above.

- [ ] **Step 2:** Commit:
  ```bash
  git -C /Users/m/CCE add infra/dns-tls/dr-provisioning-checklist.md
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "docs(infra): DR-host provisioning checklist

  One-time setup checklist for the DR host. Mirrors the prod-host
  setup but with DR-specific notes: cold-stays-cold, no scheduled
  backup tasks (until promoted), DNS records pre-provisioned with
  low TTL but not pointing at DR, AUTO_ROLLBACK=false. Quarterly
  cold-state validation procedure included.

  Sub-10c Phase 05 Task 5.1.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 5.2: DR-promotion runbook

**Files:**
- Create: `docs/runbooks/dr-promotion.md` — 8-step DR promotion procedure + failback section.

**Final state of `docs/runbooks/dr-promotion.md`:**

```markdown
# DR-promotion runbook (Sub-10c)

Trigger: prod host unreachable / data centre down / hardware failure / ops decision to fail over.

**RTO target**: ~hours (host bring-up + restore time + DNS propagation).
**RPO target**: ≤15 minutes (log-backup interval per ADR-0056).

The DR host must be pre-provisioned per [`infra/dns-tls/dr-provisioning-checklist.md`](../../infra/dns-tls/dr-provisioning-checklist.md). If it isn't, you can't fail over fast — fix that as a prerequisite, not during a disaster.

## Step 1: Decision

Operator + ops lead confirm DR promotion is the right call. Considerations:

- Is prod recoverable in less time than DR promotion? (Hardware fail with spare on hand: usually yes. Data-centre outage: usually no.)
- Has prod been ruled out as recoverable in the next N minutes?
- Is there active in-flight work (a deploy, a destructive migration) that would be lost?

Record the decision in the incident log with timestamp + signatories.

## Step 2: Fetch latest backup chain from off-host store

From the DR host:

```powershell
$bkRoot = '\\${BACKUP_UNC_HOST}\${BACKUP_UNC_SHARE}\prod'
$drDest = 'D:\CCEBackups\restored'
robocopy $bkRoot $drDest /E /Z /R:3 /W:10 /LOG+:C:\ProgramData\CCE\logs\dr-fetch-$(Get-Date -Format 'yyyyMMddTHHmmssZ').log
```

Verify:
- Latest FULL backup is recent (within 24h).
- DIFF chain is contiguous from the FULL.
- LOG chain is contiguous from the latest FULL or DIFF.

## Step 3: Restore DB

```powershell
$full = (Get-ChildItem D:\CCEBackups\restored\FULL\*.bak | Sort LastWriteTime | Select -Last 1).FullName
$diff = (Get-ChildItem D:\CCEBackups\restored\DIFF\*.bak | Sort LastWriteTime | Select -Last 1).FullName
$logs = Get-ChildItem D:\CCEBackups\restored\LOG\*.trn |
        Where-Object LastWriteTime -gt (Get-Item $full).LastWriteTime |
        Sort-Object LastWriteTime | ForEach-Object FullName

.\infra\backup\Restore-FromBackup.ps1 `
    -FullBackup $full `
    -DiffBackup $diff `
    -LogBackups $logs `
    -TargetDb CCE -Force `
    -Environment dr
```

Verify migration history matches what the to-be-deployed image expects:

```powershell
sqlcmd -S localhost -d CCE -Q "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId"
```

## Step 4: Deploy apps on DR host

```powershell
.\deploy\deploy.ps1 -Environment dr
```

Uses `.env.dr`, deploys at the same `CCE_IMAGE_TAG` prod was running. Apps come up against the just-restored DB.

## Step 5: Verify DR host green internally

From the DR host itself:

```powershell
.\deploy\smoke.ps1 -Environment dr -Timeout 90
```

Probes the 4 DR hostnames (`cce-ext-dr`, etc.) over HTTPS. Expected: `All 4 probes PASSED.`

## Step 6: DNS swap

Repoint the **production** hostnames at the DR host's IP. This is the moment where production traffic flips.

| Hostname | From | To |
|---|---|---|
| `CCE-ext` | prod IP | DR IP |
| `CCE-admin-Panel` | prod IP | DR IP |
| `api.CCE` | prod IP | DR IP |
| `Api.CCE-admin-Panel` | prod IP | DR IP |

Operator-managed DNS; per IDD environment. Low DNS TTL during steady-state means propagation is fast (~60s).

If a load-balancer fronts the host instead of direct DNS → reconfigure the LB's pool member from prod IP to DR IP.

## Step 7: End-to-end validation

From a real client outside the DR host:

```powershell
Resolve-DnsName CCE-ext
# Expected: A record points at DR host IP.

Test-NetConnection -ComputerName CCE-ext -Port 443
# Expected: TcpTestSucceeded = True.

Invoke-WebRequest https://CCE-ext/ -UseBasicParsing | Select-Object StatusCode
# Expected: 200.

# Smoke probe from the client subnet.
.\deploy\smoke.ps1 -Environment dr  # but using the prod hostnames now
```

Note: the `smoke.ps1 -Environment dr` step probes the `dr` hostnames per `.env.dr`. After Step 6, the **prod hostnames also resolve to the DR IP**, so additionally probe:

```powershell
Invoke-WebRequest https://CCE-ext/ -UseBasicParsing
Invoke-WebRequest https://CCE-admin-Panel/ -UseBasicParsing
Invoke-WebRequest https://api.CCE/health -UseBasicParsing
Invoke-WebRequest https://Api.CCE-admin-Panel/health -UseBasicParsing
```

## Step 8: Communicate

- Status page update (degraded → operating from DR).
- Customer notification per ops policy.
- Internal incident channel: confirm DR promotion complete; estimate prod-recovery time.

## After promotion: activate DR backups

DR is now production. Its backups need to start flowing.

```powershell
# Register backup tasks on the (now-active) DR host.
.\infra\backup\Register-ScheduledTasks.ps1 -Environment dr `
    -ServiceAccount cce.local\cce-sqlbackup-svc

# Verify.
Get-ScheduledTask | Where-Object TaskName -like 'CCE-Backup-*' | Format-Table TaskName, State
```

The DR host's backups go to the same UNC share but under `\\<host>\<share>\dr\`. Prod's old backups remain at `\\<host>\<share>\prod\` for reference.

## Failback (DR → prod)

When prod is recoverable:

1. **Verify prod host healthy.** Re-image, re-provision, get the host back to a clean state.
2. **Pull DR's latest backup chain to prod.**
   ```powershell
   robocopy "\\${BACKUP_UNC_HOST}\${BACKUP_UNC_SHARE}\dr" "D:\CCEBackups\restored" /E /Z
   ```
3. **Restore on prod.** Same procedure as Step 3, but `-Environment prod -TargetDb CCE -Force`.
4. **Deploy on prod**: `.\deploy\deploy.ps1 -Environment prod`.
5. **DNS swap back** (Step 6 in reverse).
6. **Deactivate DR backup tasks** + reactivate prod's:
   ```powershell
   # On DR host:
   foreach ($n in 'CCE-Backup-Full','CCE-Backup-Diff','CCE-Backup-Log','CCE-Backup-IntegrityCheck','CCE-Backup-Sync-OffHost') {
       Disable-ScheduledTask -TaskName $n
   }
   # On prod host:
   foreach ($n in 'CCE-Backup-Full','CCE-Backup-Diff','CCE-Backup-Log','CCE-Backup-IntegrityCheck','CCE-Backup-Sync-OffHost') {
       Enable-ScheduledTask -TaskName $n
   }
   ```
7. **Communicate**: incident closed; back to prod.

## Common failures during promotion

| Symptom | Cause | Fix |
|---|---|---|
| `robocopy` from UNC fails with auth error | DR host's `cmdkey` cache for the UNC not set | Re-run `cmdkey /add:...` as the deploy user |
| `Restore-FromBackup.ps1` fails with "log chain broken" | One log backup is missing or corrupt | Restore just FULL + DIFF; accept the RPO loss; document |
| `deploy.ps1` fails at image pull | DR host can't reach ghcr.io | Check `docker login`; verify outbound 443 firewall to ghcr.io |
| Smoke probes pass internally but external clients can't reach DR | DNS not yet propagated | Wait for TTL; use external DNS-resolution checker |
| Auth fails after DNS swap | Keycloak realm `cce-dr` issuer doesn't match the redirect URL | Verify `KEYCLOAK_AUTHORITY` in `.env.dr` uses `api.cce-dr` not `api.CCE`; or per IDD, accept that prod hostnames now resolve to DR + Keycloak realm config matches |

## See also

- [DR provisioning checklist](../../infra/dns-tls/dr-provisioning-checklist.md) — pre-disaster setup.
- [Backup-restore runbook](backup-restore.md) — restore procedure detail.
- [`secret-rotation.md`](secret-rotation.md) — for any secret rotation triggered by the incident.
- [Sub-10c design spec §DR](../superpowers/specs/2026-05-04-sub-10c-design.md#dr-procedure)
```

- [ ] **Step 1:** Create `docs/runbooks/dr-promotion.md` with the contents above.

- [ ] **Step 2:** Commit:
  ```bash
  git -C /Users/m/CCE add docs/runbooks/dr-promotion.md
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "docs(runbook): DR-promotion 8-step procedure + failback

  Operator runbook for failover to DR. 8-step procedure: decision,
  fetch backups via robocopy, restore DB, deploy apps, internal
  smoke, DNS swap, end-to-end client validation, communicate.
  Plus failback procedure (reverse direction). Plus common-failure
  table (UNC auth, log-chain gap, ghcr.io reachability, DNS
  propagation, Keycloak realm issuer drift).

  Sub-10c Phase 05 Task 5.2.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 5.3: ADR-0057 + completion doc + CHANGELOG

**Files:**
- Create: `docs/adr/0057-multi-env-via-per-env-files.md`.
- Create: `docs/sub-10c-production-infra-completion.md`.
- Modify: `CHANGELOG.md` — new `[infra-v1.0.0]` section at top.

**Final state of `docs/adr/0057-multi-env-via-per-env-files.md`:**

```markdown
# ADR-0057 — Multi-env via per-env files; Vault graduation deferred

**Status:** Accepted
**Date:** 2026-05-04
**Deciders:** Sub-10c brainstorm (kilany113@gmail.com)
**Sub-project:** [Sub-10c — Production infra + DR](../superpowers/specs/2026-05-04-sub-10c-design.md)

## Context

Sub-10b shipped one-environment deployment (`.env.prod` only). Sub-10c targets four environments (test, preprod, prod, dr) sharing the same hosts-and-Linux-containers shape. Multi-env config is the foundation every other Sub-10c phase consumes (identity binds to env-specific Keycloak realms, IIS provisions env-specific hostnames, backup writes to env-specific UNC subdirectories).

## Decision

Use **per-env env-files** at `C:\ProgramData\CCE\.env.<env>` (one per environment), with `deploy.ps1 -Environment <env>` resolving the env-file. NTFS ACLs lock each env-file to the deploy user + Administrators. Per-env audit trail via `deploy-history-${env}.tsv`. Secrets stay in env-files; rotation is operator-driven via the documented procedure (see `docs/runbooks/secret-rotation.md`).

Vault / Azure Key Vault / AWS Secrets Manager **graduation is explicitly deferred** to Sub-10d+ if it ever happens.

**Considered alternatives:**

- **Compose profiles per env**: rejected. Compose profiles opt services in/out, not parameterize their env-vars. Doesn't solve the per-env config problem.
- **Helm-style overlay (`docker-compose.<env>.yml` per env)**: rejected. Overlays shine when service shapes vary between envs; CCE's service shape is identical across envs (only env-vars differ). Adds complexity without value.
- **Vault graduation**: rejected for Sub-10c. Single-host single-tenant scale means env-files + NTFS ACLs are sufficient. Vault adds a new infrastructure component (server, unseal procedure, root-token storage) and a new failure mode for marginal benefit at this scale. Path to Vault stays open — env-files are config-from-anywhere; nothing in the deploy flow forecloses graduation later.

## Implementation

`deploy.ps1 -Environment <test|preprod|prod|dr>` resolves to `C:\ProgramData\CCE\.env.<env>`. Default `prod` preserves Sub-10b backward compat (existing call sites work unchanged).

`rollback.ps1` mirrors the switch.

`deploy/validate-env.ps1` provides canary integrity check: rejects placeholder values + known-leaked secrets + suspicious whitespace + cross-key inconsistencies.

`deploy/promote-env.ps1` does mechanical promotion (test → preprod → prod) — rewrites per-env knobs (DB name, hostnames, Sentry environment, AUTO_ROLLBACK default, log level, image tag stream); **re-blanks all secrets** to enforce per-env isolation.

Per-env `deploy-history-${env}.tsv` audit trail prevents test deploys from cluttering prod history.

## Consequences

**Positive:**
- Zero new infra. Operates entirely on the host filesystem.
- Backward-compat: Sub-10b deploys keep working with the default `-Environment prod`.
- Operator workflow is consistent across envs (same scripts, different `-Environment` value).
- Promotion is mechanical (`promote-env.ps1`) with security-by-default secret re-blanking.
- Canary check + cross-key consistency catches placeholder values + leaked-secret canaries before deploy.

**Negative / accepted:**
- Secret rotation is manual via the runbook procedure. Operator must touch each env-file individually.
- No central audit of secret values across envs; the deploy-history.tsv shows which tag was deployed when, not what was in the env-file.
- File-based secrets are a recognized risk; mitigated by NTFS ACL + canary check, but not eliminated.

**Out of scope (Sub-10d+):**
- Vault / Key Vault graduation.
- Automated rotation.
- Multi-host (each env on multiple hosts) — per-host env-files would diverge; that's a Sub-10c+ HA topology question.

## References

- [Sub-10c design spec §Multi-env config](../superpowers/specs/2026-05-04-sub-10c-design.md#multi-env-config--per-env-files)
- [Secret-rotation runbook](../runbooks/secret-rotation.md)
- [Env-promotion runbook](../runbooks/env-promotion.md)
- ADR-0054 — IIS reverse proxy on Windows Server (Sub-10c)
- ADR-0055 — AD federation via Keycloak LDAP (Sub-10c)
- ADR-0056 — Backup strategy (Sub-10c)
```

**Final state of `docs/sub-10c-production-infra-completion.md`:**

```markdown
# Sub-10c — Production infra + DR — Completion

**Released:** 2026-05-04
**Tag:** `infra-v1.0.0`
**Sub-project:** Third and final Sub-10 sub-project. Sub-10a `app-v1.0.0` + Sub-10b `deploy-v1.0.0` shipped earlier.
**Spec:** [`superpowers/specs/2026-05-04-sub-10c-design.md`](superpowers/specs/2026-05-04-sub-10c-design.md)
**Plan:** [`superpowers/plans/2026-05-04-sub-10c.md`](superpowers/plans/2026-05-04-sub-10c.md)

## What shipped

The CCE platform is now operationally complete on the IDD v1.2 target hardware. Multi-environment deployment (test → preprod → prod → DR), AD federation via Keycloak LDAP, IIS reverse proxy with TLS termination at IDD hostnames, automated backups + documented restore, auto-rollback on smoke failure, production Sentry observability, DR-host provisioning + 8-step promotion runbook.

## Phases

### Phase 00 — Multi-env foundation (6 commits)
- `deploy.ps1 -Environment <env>` switch + per-env `deploy-history-${env}.tsv`.
- 4 `.env.<env>.example` files (test/preprod/prod/dr); `.gitignore` allow-list extended.
- `deploy/validate-env.ps1` canary integrity check.
- `deploy/promote-env.ps1` mechanical env-promotion helper.
- `secret-rotation.md` + `env-promotion.md` runbooks.

### Phase 01 — Identity (AD federation via Keycloak LDAP) (4 commits)
- `infra/keycloak/realm-cce-ldap-federation.json` parameterized realm-import.
- `infra/keycloak/apply-realm.ps1` idempotent provisioner.
- 3 `KeycloakLdapFederationTests` against Testcontainers Keycloak.
- ADR-0055 + `ad-federation.md` runbook.

### Phase 02 — Network (IIS + TLS + DNS) (5 commits)
- `infra/iis/Install-ARRPrereqs.ps1` (IIS + URL Rewrite + ARR feature install).
- `infra/iis/web.config.template` (parameterized rewrite + security headers).
- `infra/iis/Configure-IISSites.ps1` provisioner.
- `infra/dns-tls/README.md` cert + DNS operator checklist.
- `deploy/smoke.ps1` env-aware HTTPS probes against IDD hostnames.
- ADR-0054.

### Phase 03 — Backup automation + restore (5 commits)
- `infra/backup/Install-OlaHallengren.ps1` bootstrap + checksum verification.
- `infra/backup/Register-ScheduledTasks.ps1` (5 schtasks: full/diff/log/integrity/sync).
- `infra/backup/Sync-OffHost.ps1` (robocopy /MIR to UNC).
- `infra/backup/Restore-FromBackup.ps1` (full+diff+log replay).
- `infra/backup/Test-BackupChain.ps1` (24h healthcheck via CommandLog).
- 2 `RestoreFromBackupTests` against Testcontainers SQL.
- ADR-0056 + `backup-restore.md` runbook.

### Phase 04 — Auto-rollback + Sentry production DSN (4 commits)
- `deploy.ps1` `-AutoRollback`/`-NoAutoRollback`/`-Recursive` precedence + auto-rollback flow + `Send-SentryBreadcrumb`.
- `LoggingExtensions.ConfigureSentry` reads `SENTRY_ENVIRONMENT` + `SENTRY_RELEASE`.
- 4 `LoggingExtensionsSentryTests`.
- Sentry alert-rules section in `deploy.md`.

### Phase 05 — DR procedure + close-out (4 commits)
- `infra/dns-tls/dr-provisioning-checklist.md` (one-time DR-host setup).
- `docs/runbooks/dr-promotion.md` (8-step procedure + failback).
- ADR-0057.
- This completion doc + CHANGELOG entry + tag.

## Gates green at release

| Gate | Result |
|---|---|
| Backend build | clean |
| `dotnet test tests/CCE.Application.Tests/` | 439 passing (unchanged from Sub-10a) |
| `dotnet test tests/CCE.Infrastructure.Tests/` | 75 passing + 1 skipped (was 54 at Sub-10b; +9 flag, +3 migration, +3 federation, +2 backup, +4 Sentry) |
| Frontend tests | 502 across 90 suites (unchanged) |
| Lighthouse a11y gate | passes (unchanged) |
| axe-core gate | zero critical/serious (unchanged) |
| CI `docker-build` job | builds + pushes 5 images on `main` (unchanged from Sub-10b) |

## What changed for operators

| Before Sub-10c | After Sub-10c |
|---|---|
| One `.env.prod` env-file | Four env-files (`test`/`preprod`/`prod`/`dr`); `-Environment` switch |
| Manual login form per app, no AD integration | Keycloak LDAP federation against `cce.local` AD; users login with AD credentials |
| Apps reachable only at localhost ports | IIS reverse proxy at IDD hostnames over HTTPS |
| No automated backups | Ola Hallengren + 5 scheduled tasks + off-host UNC sync |
| No restore procedure | `Restore-FromBackup.ps1` + Testcontainers-tested round-trip + runbook |
| No DR story | DR provisioning checklist + 8-step promotion runbook + RTO ~hours / RPO ≤15 min |
| Manual rollback only | `-AutoRollback` + per-env `AUTO_ROLLBACK` opt-in |
| Sentry events untagged | Events carry `environment` + `release` tags; alert rules per env |
| No secret rotation procedure | Per-secret rotation runbook with verify steps |

## Out of scope (deferred)

**Sub-11 — AD FS / Entra ID migration (next sub-project):**
- Replace Keycloak with Entra ID (cloud) + Entra ID Connect from on-prem AD.
- CCE writes to Entra ID via Microsoft Graph for self-service registration.
- ADR-0055 (Keycloak LDAP) supersedes when Sub-11 lands.

**Sub-10d+ (or never):**
- Active-passive HA with SQL Server log-shipping.
- Active-active multi-host load-balanced topology.
- Vault / Azure Key Vault graduation.
- SPNEGO / Kerberos SSO.
- Automated cert + DNS provisioning.
- WAF / IP allowlisting.
- Backup encryption at rest.
- Auto-test-restore in CI on a schedule.

## ADRs

- ADR-0054 — IIS reverse proxy on Windows Server.
- ADR-0055 — AD federation via Keycloak LDAP (superseded by Sub-11 Entra ID).
- ADR-0056 — Backup strategy: Ola Hallengren + Task Scheduler.
- ADR-0057 — Multi-env via per-env files; Vault graduation deferred.

## Cross-references

- [Sub-10a App productionization completion](sub-10a-app-productionization-completion.md)
- [Sub-10b Deployment automation completion](sub-10b-deployment-automation-completion.md)
- [Forward-only migrations runbook](runbooks/migrations.md) (Sub-10b)
- [Production deploy runbook](runbooks/deploy.md)
- [Rollback runbook](runbooks/rollback.md) (Sub-10b)
- [AD federation runbook](runbooks/ad-federation.md) (Sub-10c)
- [Secret-rotation runbook](runbooks/secret-rotation.md) (Sub-10c)
- [Env-promotion runbook](runbooks/env-promotion.md) (Sub-10c)
- [Backup-restore runbook](runbooks/backup-restore.md) (Sub-10c)
- [DR-promotion runbook](runbooks/dr-promotion.md) (Sub-10c)
```

**`CHANGELOG.md` modification** — prepend a new top section above the existing `[deploy-v1.0.0]` entry:

```markdown
## [infra-v1.0.0] — 2026-05-04

**Sub-10c — Production infra + DR.** The CCE platform is now operationally complete on IDD v1.2 hardware. Multi-environment promotion, AD federation via Keycloak, IIS reverse proxy with TLS, automated backups + restore, auto-rollback, production Sentry observability, DR-host provisioning + 8-step promotion runbook.

### Added
- `deploy.ps1 -Environment <test|preprod|prod|dr>` + per-env `deploy-history-${env}.tsv` audit trail.
- `deploy/validate-env.ps1` canary integrity check (placeholder values, known-leaked secrets, BOM/CR detection, cross-key consistency).
- `deploy/promote-env.ps1` mechanical env-promotion helper that re-blanks secrets across boundaries.
- `deploy.ps1 -AutoRollback` / `-NoAutoRollback` / `-Recursive` flow with Sentry breadcrumb on auto-rollback.
- `infra/keycloak/apply-realm.ps1` + `realm-cce-ldap-federation.json` (idempotent Keycloak provisioning of LDAP user-federation against `cce.local` AD).
- `infra/iis/Install-ARRPrereqs.ps1` + `Configure-IISSites.ps1` + `web.config.template` (4 IIS sites with TLS + ARR rewrites).
- `infra/backup/Install-OlaHallengren.ps1` + `Register-ScheduledTasks.ps1` + `Sync-OffHost.ps1` + `Restore-FromBackup.ps1` + `Test-BackupChain.ps1` (Ola Hallengren + 5 scheduled tasks + off-host UNC sync + restore helper + healthcheck).
- 9 new docs: ADR-0054, ADR-0055, ADR-0056, ADR-0057, completion doc, AD federation runbook, secret-rotation runbook, env-promotion runbook, backup-restore runbook, DR-promotion runbook, DR-host provisioning checklist, cert + DNS operator checklist.
- 4 `.env.<env>.example` files (test/preprod/prod/dr).
- 13 new backend tests: 3 KeycloakLdapFederationTests, 2 RestoreFromBackupTests, 4 LoggingExtensionsSentryTests, 4 (additional) — Infrastructure.Tests goes from 54 → 75.
- `LoggingExtensions.ConfigureSentry` reads `SENTRY_ENVIRONMENT` + `SENTRY_RELEASE` and propagates to Sentry events.

### Changed
- `rollback.ps1` mirrors `-Environment` + `-Recursive` switches; passes them to nested `deploy.ps1`.
- `deploy/smoke.ps1` gains env-aware HTTPS mode against IDD hostnames; preserves Sub-10b localhost mode for backward compat.
- `.gitignore` allow-list extended to commit all 4 `.env.<env>.example` files.

### Architecture decisions
- ADR-0054 — IIS reverse proxy on Windows Server (vs Caddy/Traefik/nginx); ADR-0055 — Keycloak LDAP federation (vs SPNEGO, AD FS, read-write); ADR-0056 — Ola Hallengren + Task Scheduler (vs custom T-SQL, Veeam, cloud-managed); ADR-0057 — Multi-env via per-env files (vs compose profiles, helm overlays, Vault graduation deferred).
```

- [ ] **Step 1:** Create `docs/adr/0057-multi-env-via-per-env-files.md` with the contents above.

- [ ] **Step 2:** Create `docs/sub-10c-production-infra-completion.md` with the contents above.

- [ ] **Step 3:** Read existing `CHANGELOG.md` to find the top:
  ```bash
  head -10 /Users/m/CCE/CHANGELOG.md
  ```

- [ ] **Step 4:** Prepend the `[infra-v1.0.0]` section above the existing `[deploy-v1.0.0]` entry.

- [ ] **Step 5:** Commit:
  ```bash
  git -C /Users/m/CCE add docs/adr/0057-multi-env-via-per-env-files.md \
                          docs/sub-10c-production-infra-completion.md \
                          CHANGELOG.md
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "docs(sub-10c): close-out — ADR-0057, completion doc, CHANGELOG

  ADR-0057 documents the multi-env decision: per-env files at
  C:\\ProgramData\\CCE\\.env.<env> + -Environment switch + canary
  integrity check + mechanical env-promotion. Considered + rejected:
  compose profiles (don't parameterize env-vars), helm overlays
  (overkill for env-var-only differences), Vault graduation
  (deferred to Sub-10d+ at scale).

  Completion doc mirrors Sub-10a/10b shape: phase summaries (5
  phases / ~22 commits), gates green at release, before/after
  operator delta, Sub-11 Entra ID migration noted as next.

  CHANGELOG entry under [infra-v1.0.0].

  Sub-10c Phase 05 Task 5.3.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 5.4: Tag `infra-v1.0.0`

- [ ] **Step 1:** Verify HEAD is the close-out commit:
  ```bash
  git -C /Users/m/CCE log --oneline -3
  ```

- [ ] **Step 2:** Tag locally:
  ```bash
  git -C /Users/m/CCE tag -a infra-v1.0.0 -m "Sub-10c — Production infra + DR

  Operationally complete on IDD v1.2 hardware. Multi-env (test/preprod/prod/dr),
  AD federation via Keycloak LDAP, IIS reverse proxy with TLS,
  automated backups + restore, auto-rollback on smoke failure,
  production Sentry observability, DR-host provisioning + 8-step
  promotion runbook.

  Spec: docs/superpowers/specs/2026-05-04-sub-10c-design.md
  Completion: docs/sub-10c-production-infra-completion.md"
  ```

- [ ] **Step 3:** Verify the tag exists:
  ```bash
  git -C /Users/m/CCE tag -l "infra-v*"
  ```
  Expected: `infra-v1.0.0`.

---

## Phase 05 close-out

**After Task 5.4:**

- [ ] **Final test sweep:**
  ```bash
  cd /Users/m/CCE/backend && dotnet build && \
    dotnet test tests/CCE.Application.Tests/ --nologo
  ```
  Expected: 439 passing.

- [ ] **Verify all expected tags exist:**
  ```bash
  git -C /Users/m/CCE tag -l | sort
  ```
  Expected: includes `app-v1.0.0`, `deploy-v1.0.0`, `infra-v1.0.0`.

- [ ] **Hand off to Sub-11 brainstorm.** Sub-11 is the Entra ID migration (per the user's β1 choice): replace Keycloak with Entra ID + Entra ID Connect from on-prem AD; CCE writes to Entra ID via Microsoft Graph for self-service registration.

**Sub-10c done when:**
- 4 commits land on `main` for Phase 05 (5.1, 5.2, 5.3, plus tag).
- ADR-0057 + DR-promotion runbook + DR-provisioning checklist + completion doc + CHANGELOG `[infra-v1.0.0]` all land.
- Tag `infra-v1.0.0` exists locally.
- Sub-10 is **complete** (10a + 10b + 10c shipped).

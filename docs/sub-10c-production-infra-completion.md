# Sub-10c — Production infra + DR — Completion

**Released:** 2026-05-04
**Tag:** `infra-v1.0.0`
**Sub-project:** Third and final Sub-10 sub-project. Sub-10a `app-v1.0.0` + Sub-10b `deploy-v1.0.0` shipped earlier.
**Spec:** [`../project-plan/specs/2026-05-04-sub-10c-design.md`](../project-plan/specs/2026-05-04-sub-10c-design.md)
**Plan:** [`../project-plan/plans/2026-05-04-sub-10c.md`](../project-plan/plans/2026-05-04-sub-10c.md)

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

**Sub-11 — Entra ID migration (next sub-project):**
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

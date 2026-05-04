# ADR-0056 — Backup strategy: Ola Hallengren + Task Scheduler

**Status:** Accepted
**Date:** 2026-05-04
**Deciders:** Sub-10c brainstorm (kilany113@gmail.com)
**Sub-project:** [Sub-10c — Production infra + DR](../superpowers/specs/2026-05-04-sub-10c-design.md)

## Context

CCE runs SQL Server on the Windows host (per IDD). Sub-10c needs scheduled backups that survive a host-level disaster, plus a documented restore procedure. RTO ~hours, RPO ≤15 minutes.

## Decision

Use **Ola Hallengren's SQL Server Maintenance Solution** for backup execution + retention; **Windows Task Scheduler** for triggering; **robocopy** for off-host sync to a UNC share.

5 scheduled tasks:

| Task | Frequency | Type | Retention | Destination |
|---|---|---|---|---|
| `CCE-Backup-Full` | Daily 02:00 local | `FULL` | 7 days | `D:\CCEBackups\FULL\` |
| `CCE-Backup-Diff` | Every 6 hours | `DIFF` | 7 days | `D:\CCEBackups\DIFF\` |
| `CCE-Backup-Log` | Every 15 minutes | `LOG` | 24 hours | `D:\CCEBackups\LOG\` |
| `CCE-Backup-IntegrityCheck` | Sunday 03:00 | `DBCC CHECKDB` | logs only | `C:\ProgramData\CCE\logs\` |
| `CCE-Backup-Sync-OffHost` | Hourly | `robocopy /MIR` | 30-day at destination | `\\${BACKUP_UNC_HOST}\${BACKUP_UNC_SHARE}\<env>\` |

Recovery model: `FULL` (required for log backups; gives 15-minute RPO).

**Considered alternatives:**

- **Built-in `BACKUP DATABASE` via custom PowerShell:** rejected. Reinvents Ola's retention + log-chain logic. No reason to roll our own when the standard tool is free and battle-tested.
- **Veeam / commercial backup tool:** rejected for Sub-10c. Adds licensing + new infra. Sub-10c stays within free tooling; commercial backup is a Sub-10d+ decision when the IDD ops team standardizes one.
- **Cloud-managed backup (Azure SQL backups, RDS snapshots):** rejected. SQL Server is on-prem per IDD; no cloud DB.

**Why Ola Hallengren won:**
- Industry-standard for SQL Server on Windows. Free. Documented procs (`DatabaseBackup`, `DatabaseIntegrityCheck`, `IndexOptimize`).
- Battle-tested retention logic (`@CleanupTime` parameter; understands the full → diff → log chain dependency).
- Logs every operation to `master.dbo.CommandLog` — trivial healthcheck + audit trail.
- Single-script install via `MaintenanceSolution.sql`. Idempotent (`CREATE OR ALTER PROC`).
- Active community + Microsoft endorsement.

## Implementation

`infra/backup/Install-OlaHallengren.ps1` is the bootstrap installer. Downloads `MaintenanceSolution.sql` from `ola.hallengren.com`, verifies SHA256 against `MaintenanceSolution.checksum`, applies via sqlcmd. First-install detection asks operator to record the SHA256 + commit.

`infra/backup/Register-ScheduledTasks.ps1` is the schtasks provisioner. 5 tasks; idempotent (deletes + re-creates by name).

`infra/backup/Sync-OffHost.ps1` is the robocopy wrapper for off-host sync. UNC auth via `cmdkey` cache (operator one-time setup).

`infra/backup/Restore-FromBackup.ps1` is the operator-driven restore helper. Refuses to restore over live `CCE` DB without `-Force`.

`infra/backup/Test-BackupChain.ps1` queries `master.dbo.CommandLog` for last-24h health; warn-only mode for deploy.ps1's post-deploy step.

## Consequences

**Positive:**
- Standard SQL Server DBA tooling; ops team already familiar.
- Zero new infra (no third-party services or agents).
- Backup logs centralized in `master.dbo.CommandLog` for auditing.
- 15-minute RPO via 15-min log backups.
- Off-host sync gives geographic redundancy via the UNC destination.
- Restore script covers full + diff + log chain; tested via Testcontainers SQL round-trip.

**Negative / accepted:**
- Restore is operator-driven; not auto-tested in CI beyond the SQL-command-sequence tests. Quarterly drill restore is documented as an ops procedure.
- UNC sync requires `cmdkey` credential setup on the host (one-time; documented).
- Backup encryption at rest not enabled by default. Can flip via Ola's `@EncryptionAlgorithm` parameter when IDD requires; deferred.
- 7-day full + diff retention may be tight; tunable via env-vars (`BACKUP_RETENTION_DAYS_FULL`).

**Out of scope (Sub-10c+):**
- Log shipping for HA (Sub-10d+; Decision 7B was rejected).
- Veeam / commercial integration.
- Backup encryption at rest (operator can enable per IDD).
- Auto-test-restore in CI on a regular schedule (operator-driven quarterly drill instead).

## References

- [Sub-10c design spec §Backup](../superpowers/specs/2026-05-04-sub-10c-design.md#backup--ola-hallengren--task-scheduler)
- [Backup-restore runbook](../runbooks/backup-restore.md)
- [Ola Hallengren's Maintenance Solution](https://ola.hallengren.com/)
- ADR-0054 — IIS reverse proxy on Windows Server (Sub-10c)
- ADR-0055 — AD federation via Keycloak LDAP (Sub-10c)

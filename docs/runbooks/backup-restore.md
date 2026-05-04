# Backup-restore runbook (Sub-10c)

CCE backups run via Ola Hallengren's maintenance solution + 5 Windows Task Scheduler tasks. ADR-0056 documents the design.

## Smoke-check: backup chain is healthy

```powershell
.\infra\backup\Test-BackupChain.ps1 -Environment <env>
```

Expected: `Backup chain HEALTHY.` Reports failures, last-full-success time, log-backup count over 24h.

## One-time host setup

1. **Install Ola Hallengren maintenance solution.**
   ```powershell
   .\infra\backup\Install-OlaHallengren.ps1 -Environment <env>
   ```
   First run: prints the downloaded SHA256 + asks operator to record it in `MaintenanceSolution.checksum` + re-run.

2. **Provision the backup-account.** Create a Windows account `cce.local\cce-sqlbackup-svc` (or whatever AD admins prefer) with SQL Server `sysadmin` (for `BACKUP DATABASE`) + filesystem write to `D:\CCEBackups\` + UNC write to the destination share.

3. **Cache the UNC credential.** From the deploy host as the backup-account:
   ```powershell
   cmdkey /add:${BACKUP_UNC_HOST} /user:${BACKUP_UNC_USER} /pass:${BACKUP_UNC_PASSWORD}
   ```

4. **Register scheduled tasks.**
   ```powershell
   .\infra\backup\Register-ScheduledTasks.ps1 -Environment <env> `
       -ServiceAccount cce.local\cce-sqlbackup-svc
   ```

5. **Verify.**
   ```powershell
   Get-ScheduledTask | Where-Object TaskName -like 'CCE-Backup-*' | Format-Table TaskName, State, LastRunTime, NextRunTime
   ```

## Quarterly drill: test restore

Recommended every quarter on a non-prod host:

```powershell
# Pick the latest backup chain.
$full = Get-ChildItem D:\CCEBackups\FULL\*.bak | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$diff = Get-ChildItem D:\CCEBackups\DIFF\*.bak | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$logs = Get-ChildItem D:\CCEBackups\LOG\*.trn  | Where-Object LastWriteTime -gt $full.LastWriteTime |
        Sort-Object LastWriteTime | ForEach-Object FullName

# Restore to a test DB.
.\infra\backup\Restore-FromBackup.ps1 `
    -FullBackup $full.FullName `
    -DiffBackup $diff.FullName `
    -LogBackups $logs `
    -TargetDb CCE_restoretest

# Verify row counts vs the live CCE DB.
sqlcmd -S <server> -d CCE_restoretest -Q "SELECT COUNT(*) FROM <key-table>"
sqlcmd -S <server> -d CCE              -Q "SELECT COUNT(*) FROM <key-table>"

# Cleanup.
sqlcmd -S <server> -Q "DROP DATABASE CCE_restoretest"
```

Record the result in the ops runbook log.

## Post-incident restore (live)

After a destructive incident (data corruption, accidental delete) on the live DB:

1. **Stop apps** to prevent further writes:
   ```powershell
   docker compose -f docker-compose.prod.yml down
   ```

2. **Identify the last good backup point.** Use `Test-BackupChain.ps1` to find the last full + last diff before the incident, plus all logs up to (but not past) the incident time.

3. **Run restore with `-Force`:**
   ```powershell
   .\infra\backup\Restore-FromBackup.ps1 `
       -FullBackup <path> -DiffBackup <path> -LogBackups <list> `
       -TargetDb CCE -Force
   ```

4. **Verify migration history matches** what the running image expects:
   ```powershell
   sqlcmd -S <server> -d CCE -Q "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId"
   ```

5. **Restart apps**:
   ```powershell
   .\deploy\deploy.ps1 -Environment <env>
   ```

## DR-host cold-start restore

After DR promotion (see [`dr-promotion.md`](dr-promotion.md)):

1. From the DR host, fetch the latest backup chain from the off-host UNC store:
   ```powershell
   robocopy "\\${BACKUP_UNC_HOST}\${BACKUP_UNC_SHARE}\prod" "D:\CCEBackups\restored" /E /Z /R:3 /W:10
   ```

2. Run restore with `-Force` against the DR host's SQL Server:
   ```powershell
   .\infra\backup\Restore-FromBackup.ps1 `
       -FullBackup "D:\CCEBackups\restored\FULL\<latest>.bak" `
       -DiffBackup "D:\CCEBackups\restored\DIFF\<latest>.bak" `
       -LogBackups (Get-ChildItem "D:\CCEBackups\restored\LOG\*.trn" | Sort Name).FullName `
       -TargetDb CCE -Force -Environment dr
   ```

3. Continue with the deploy step in `dr-promotion.md`.

## Common failures

| Symptom | Cause | Fix |
|---|---|---|
| `RESTORE failed: file 'X' is being used by another process` | Live CCE DB still in use | Stop apps via `docker compose down` before restore |
| `RESTORE LOG fails: cannot find a backup that includes time T` | Log chain has a gap (one log backup skipped) | Use the latest contiguous chain; restore just FULL + DIFF without the broken-chain LOG |
| `cmdkey credentials missing` (robocopy auth fails) | UNC credential not cached on host | Re-run `cmdkey /add:...` as the backup-account user |
| `Backup chain healthcheck reports 0 FULL successes in 24h` | Daily 02:00 task didn't run | Check `Get-ScheduledTaskInfo CCE-Backup-Full`; investigate task history |
| `DBCC CHECKDB reports allocation errors` | Possible disk corruption | STOP. File an incident; restore from latest known-good backup |

## See also

- [ADR-0056 — Backup strategy](../adr/0056-backup-strategy-ola-hallengren.md)
- [`migrations.md`](migrations.md) — forward-only migration discipline (relevant for restore-vs-migration-history checks)
- [`dr-promotion.md`](dr-promotion.md) — DR promotion procedure (Phase 05)
- [Ola Hallengren's docs](https://ola.hallengren.com/)
- [Sub-10c design spec §Backup](../superpowers/specs/2026-05-04-sub-10c-design.md#backup--ola-hallengren--task-scheduler)

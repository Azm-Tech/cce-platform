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
- [Sub-10c design spec §DR](../../project-plan/specs/2026-05-04-sub-10c-design.md#dr-procedure)

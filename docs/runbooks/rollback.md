# Rollback runbook (Sub-10b)

When a production deploy is bad — smoke probes fail, errors spike, manual verification turns up regressions — roll back to the previous known-good image tag.

## When to roll back

- Smoke probes failed after `deploy.ps1` (script printed the rollback hint).
- `/health` returns Unhealthy after deploy completed.
- User-visible regression confirmed against the new image.
- Operator gut-feel "this isn't right" — better to roll back and investigate.

## When NOT to roll back

- Migration is in-flight (Step 6 of `deploy.ps1` not yet exited). Wait for it to finish or abort.
- Suspected schema drift (rare; forward-only discipline prevents it). See escape hatch below.
- DB corruption / data loss. Rollback won't fix it; need DBA + backup.

## Procedure

1. **Identify the previous good tag** from `deploy-history.tsv`:
   ```powershell
   Get-Content C:\ProgramData\CCE\deploy-history.tsv | Select-Object -Last 10
   ```

   Example output:
   ```
   2026-05-04T10:32:18Z	c612812...	app-v1.0.0	OK
   2026-05-04T11:15:02Z	5a6eb7b...	deploy-v1.0.0	OK
   2026-05-04T14:02:55Z		deploy-v1.0.1	OK   ← the bad deploy
   ```
   Pick the most recent `OK` row before the bad one. Here that's `deploy-v1.0.0`.

2. **Run the rollback script**:
   ```powershell
   cd C:\path\to\CCE
   .\deploy\rollback.ps1 -ToTag deploy-v1.0.0
   ```

3. **Verify smoke probes pass** (rollback.ps1 invokes deploy.ps1 which runs them):
   ```
   Probing api-external/health... OK
   Probing api-internal/health... OK
   Probing web-portal/...        OK
   Probing admin-cms/...         OK
   All 4 probes PASSED.
   ```

4. **Verify the audit trail**:
   ```powershell
   Get-Content C:\ProgramData\CCE\deploy-history.tsv | Select-Object -Last 3
   ```
   You should see a `ROLLBACK_FROM=deploy-v1.0.1` row and a fresh `OK` row for `deploy-v1.0.0`.

## Common failures during rollback

| Symptom | Cause | Fix |
|---|---|---|
| `Image pull failed` for `<previous-tag>` | Tag not in ghcr.io | Image-tag retention is unlimited in ghcr.io free tier, so this is rare. Check the tag was actually pushed (search for it in old GHA run summaries). |
| `Migrator failed` | Forward-only discipline violated — schema drift | STOP. File an incident. Restore-from-backup is the only path; backup automation is Sub-10c work. |
| `Smoke probe FAILED` post-rollback | Old image has its own bug | Roll back further: `.\deploy\rollback.ps1 -ToTag <even-older-tag>`. |

## Forward-only escape hatch

If a rollback fails because the previous image can't run against the current schema, you've hit a forward-only-discipline violation. The release that broke it should have been a destructive-migration release (separate spec, backup-restore runbook). See [`migrations.md`](./migrations.md) for the rules.

Recovery from this state is a Sub-10c+ scenario: backup-restore. Sub-10b explicitly defers backup automation. For now: page the DBA, restore the pre-deploy DB snapshot, redeploy the older image.

## See also

- [`deploy.md`](./deploy.md) — green-path deploy procedure
- [`migrations.md`](./migrations.md) — forward-only discipline rules
- [Sub-10b design spec](../../project-plan/specs/2026-05-03-sub-10b-design.md) §Rollback procedure

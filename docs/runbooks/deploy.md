# Production deploy runbook (Sub-10b)

## Pre-deploy checklist

- [ ] Verify the target image tag is pushed to ghcr.io. Check the GitHub Actions run summary for the commit you want to deploy — it lists every image + tag pushed.
- [ ] Verify `C:\ProgramData\CCE\.env.prod` is up to date and ACL-locked.
- [ ] Verify SQL Server, Redis, and Keycloak are reachable from the deploy host.
- [ ] Verify Docker daemon is running (`docker info`).

## Deploy

1. **Update image tag** in `.env.prod`:
   ```
   CCE_IMAGE_TAG=<new-tag>          # e.g. app-v1.0.1, sha-c612812, or full SHA
   ```

2. **Run the deploy script**:
   ```powershell
   cd C:\path\to\CCE
   .\deploy\deploy.ps1
   ```

3. **Watch for green checkmarks** on each step:
   ```
   [INFO] Step 1/10: Resolving env-file path.
   [INFO] Env-file: C:\ProgramData\CCE\.env.prod
   [INFO] Step 2/10: Validating required keys.
   [INFO] CCE_IMAGE_TAG = app-v1.0.1
   [INFO] Step 3/10: Checking docker daemon.
   [INFO] Step 4/10: Registry login.
   [INFO] Step 5/10: Pulling images for tag app-v1.0.1.
   [INFO] Step 6/10: Running migrator.
   [INFO] Applying EF Core migrations…
   [INFO] No pending migrations.
   [INFO] Step 7/10: Bringing up apps.
   [INFO] Step 8/10: Running smoke probes.
   Probing api-external/health... OK
   Probing api-internal/health... OK
   Probing web-portal/...        OK
   Probing admin-cms/...         OK
   All 4 probes PASSED.
   [INFO] Step 9/10: deploy-history.tsv (Phase 02 implements).
   [INFO] Step 10/10: Done.
   ```

4. **Verify the apps respond on the host**:
   ```powershell
   Invoke-WebRequest http://localhost:5001/health | Select-Object -Expand Content
   Invoke-WebRequest http://localhost:5002/health | Select-Object -Expand Content
   ```

## Common failures

| Symptom | Likely cause | Fix |
|---|---|---|
| `Missing required env-keys: ...` | Forgot a key in `.env.prod` | Add the key, retry. |
| `Docker daemon not reachable` | Docker Desktop / CE not started | Start Docker, retry. |
| `Image pull failed` for one image | Typo in `CCE_IMAGE_TAG` | Verify tag in GitHub Actions run summary. |
| `Image pull failed` (auth) | ghcr.io session expired | Set `CCE_GHCR_TOKEN` in `.env.prod`, retry. |
| `Migrator failed (exit 1)` | DB unreachable, or migration error | Check log file under `C:\ProgramData\CCE\logs\`. **Apps NOT started** — system unchanged. |
| `Smoke probe FAILED: api-external/health` | App startup error (config, DB) | Check `docker compose logs api-external`. App still running for inspection. |
| `Smoke probe FAILED: web-portal/` | nginx couldn't start | Check `docker compose logs web-portal`. |

## Logs

Every deploy writes to `C:\ProgramData\CCE\logs\deploy-<UTC-timestamp>.log`. Logs older than 30 days can be deleted manually (10c will add automated rotation).

## Sentry alert rules (recommended baseline)

Sentry org admin configures these in the Sentry web UI per environment. The CCE backend tags every event with `environment` (production / preprod / test / dr) and `release` (`CCE_IMAGE_TAG`), making per-env alerting trivial.

### Recommended rules per project

| Rule | Trigger | Action |
|---|---|---|
| **First-occurrence in production** | New issue seen for the first time, environment = production | Email to ops on-call |
| **High error rate** | More than 1% of events in production are errors over 5 minutes | Email to ops on-call |
| **Auto-rollback triggered** | Event with tag `deploy.auto_rollback = true` (deploy.ps1 sends this) | Email + Slack/PagerDuty alert; treat as a real-time signal |
| **Regression after release** | Issue marked resolved seen again in a newer release | Email to dev team |

### Configuration steps

1. Sentry → Project → Alerts → Create Alert.
2. Pick "Issues" or "Metric Alert" depending on the rule.
3. Set conditions per the table above.
4. For environment-scoped alerts: under "If" filters, add `environment` equals the target environment.
5. For auto-rollback alerts: filter on `deploy.auto_rollback` tag (set by `deploy.ps1`).

### Verification

After alert rules are configured, fire a test event:

```powershell
# From the backend host: trigger a test exception (any endpoint that returns 500
# will do; or use Sentry's test-event endpoint).
curl -X POST https://api.CCE/admin/__test-error__ -H "Authorization: Bearer <token>"
```

Verify the event appears in Sentry within ~30 seconds + the alert fires per the rule.

### Auto-rollback events

Whenever `deploy.ps1` triggers an auto-rollback (smoke probe failed and `AUTO_ROLLBACK=true`), it POSTs a Sentry event tagged `deploy.auto_rollback = true` with:
- `environment` = `SENTRY_ENVIRONMENT` from the env-file
- `release` = the failed `CCE_IMAGE_TAG` (the bad tag, not the rolled-back-to tag)
- `tags.deploy.from_tag` = bad tag
- `tags.deploy.to_tag` = previous good tag
- `extra.reason` = "smoke-probe failure" (or future failure reasons)
- `extra.host` = `$env:COMPUTERNAME`

Use these tags in the alert filter for the "Auto-rollback triggered" rule.

## On failure: rollback

If the smoke probes fail or the system is broken after deploy, roll back to the previous known-good image tag:

```powershell
# Phase 02 — rollback.ps1 not yet shipped.
# Manual rollback: edit .env.prod, set CCE_IMAGE_TAG to previous,
# re-run deploy.ps1.
notepad C:\ProgramData\CCE\.env.prod
.\deploy\deploy.ps1
```

Phase 02 ships `rollback.ps1` and `deploy-history.tsv` for proper audit trails.

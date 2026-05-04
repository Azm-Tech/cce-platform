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

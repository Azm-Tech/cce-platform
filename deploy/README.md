# CCE deploy/

PowerShell scripts for deploying the CCE production stack to a single Windows Server 2022 host.

## First-time host setup

```powershell
# 1. Create the runtime config directory.
New-Item -ItemType Directory -Path C:\ProgramData\CCE -Force
New-Item -ItemType Directory -Path C:\ProgramData\CCE\logs -Force

# 2. Copy the example env-file to the host config directory.
Copy-Item .\.env.prod.example C:\ProgramData\CCE\.env.prod

# 3. Edit the env-file. Fill in: CCE_REGISTRY_OWNER, CCE_IMAGE_TAG,
#    INFRA_SQL (with real password), KEYCLOAK_AUTHORITY/AUDIENCE,
#    ANTHROPIC_API_KEY (if using Anthropic), CCE_GHCR_TOKEN (if needed).
notepad C:\ProgramData\CCE\.env.prod

# 4. Lock down ACLs so only Administrators + the deploy user can read.
icacls C:\ProgramData\CCE\.env.prod /inheritance:r `
    /grant:r "Administrators:R" `
    /grant:r "<deploy-user>:R"
```

## Deploy

```powershell
cd C:\path\to\CCE
.\deploy\deploy.ps1
```

The script:
- Validates `.env.prod` (aborts on missing required keys).
- Pulls the 5 images at `CCE_IMAGE_TAG`.
- Runs the migrator to completion (skip via `MIGRATE_ON_DEPLOY=false`).
- Brings up the 4 app services.
- Smoke-probes `/health` on the APIs and `/` on the SPAs.
- Logs every step to `C:\ProgramData\CCE\logs\deploy-<UTC-timestamp>.log`.

## Rollback (Phase 02)

```powershell
.\deploy\rollback.ps1 -ToTag <previous-tag>
```

Available after Sub-10b Phase 02 lands. Find prior tags in `C:\ProgramData\CCE\deploy-history.tsv`.

## Smoke probe (standalone)

```powershell
.\deploy\smoke.ps1 [-Timeout 90] [-Quiet]
```

Useful for ad-hoc verification without redeploying.

## Files in this directory

| File | Purpose |
|---|---|
| `deploy.ps1` | Main deploy entry point |
| `smoke.ps1`  | Localhost endpoint probes |
| `rollback.ps1` | Image-tag rollback (Phase 02) |
| `README.md`  | This file |

## See also

- [Production deploy runbook](../docs/runbooks/deploy.md)
- [Rollback runbook](../docs/runbooks/rollback.md) (Phase 02)
- [Forward-only migrations](../docs/runbooks/migrations.md)
- [Sub-10b design spec](../docs/superpowers/specs/2026-05-03-sub-10b-design.md)

# ADR-0053 — Deployment shape: Linux containers on Windows Server 2022

**Status:** Accepted
**Date:** 2026-05-03
**Deciders:** Sub-10b brainstorm (kilany113@gmail.com)
**Sub-project:** [Sub-10b — Deployment automation](../superpowers/specs/2026-05-03-sub-10b-design.md)

## Context

Sub-10a shipped 4 production Linux Docker images (`Api.External`, `Api.Internal`, `web-portal`, `admin-cms`) plus the `CCE.Seeder` console. Sub-10b's job is to wrap those into a deployable system targeting one environment end-to-end on a Windows Server 2022 host (per IDD v1.2: Windows Server 2022 + Intel Xeon Gold 6138 hardware, SQL Server, Redis 6379, AD 389/636).

Five orthogonal decisions had to be made before the rest of the design could proceed.

## Decision

### 1. Linux containers on Windows Server 2022 (not Windows-native rebuild, not hybrid)

Reuse Sub-10a's 4 Linux images verbatim. Run via Docker (Desktop or CE/Mirantis runtime) on the Windows host.

**Considered alternatives:**
- **Windows-native rebuild (IIS hosting + Windows Service):** rejected — would discard Sub-10a's working CI image pipeline; rebuild ASP.NET Core hosting under IIS introduces app-pool quirks (in-process hosting, recycling, log redirection); frontends would need separate IIS-served static delivery. Significant rework.
- **Hybrid (backend Linux containers + frontends in IIS):** rejected — worst of both. Two deployment models, doubled rollback surface, still requires Docker for backend.

**Why this won:** zero rebuild work, identical to dev environment, IDD allows containers (calls out Windows Server 2022 hardware target but doesn't mandate native Windows hosting).

**Trade-off accepted:** Docker Desktop licensing + a Linux VM running on the Windows host. For one-environment-end-to-end this is acceptable; multi-tenant licensing is a Sub-10c question.

### 2. Sidecar migrator (auto on deploy, configurable kill-switch via `MIGRATE_ON_DEPLOY=false`)

Compose declares the migrator service with `depends_on: { condition: service_completed_successfully }` so APIs wait for it. `deploy.ps1` invokes the migrator explicitly with `docker compose run --rm --no-deps migrator` to capture exit code, then brings up apps with `--no-deps`.

**Considered alternative:**
- **Gated (operator runs migrator manually, then APIs):** rejected as default. Two-step deploy is easier to skip accidentally; migration becomes an opt-in step instead of an automated one.

**Why this won:** automation by default + the gate when needed (`MIGRATE_ON_DEPLOY=false` in `.env.prod`).

### 3. `.env.prod` on the host with NTFS-restricted ACLs (not Vault, not Docker secrets)

Single file at `C:\ProgramData\CCE\.env.prod`, mode-locked via `icacls` to Administrators + the deploy user. Compose reads it via the `env_file:` directive on each service.

**Considered alternatives:**
- **Docker secrets (`docker secret`):** rejected — requires Swarm mode or compose `secrets:` blocks pointing at host files; every consumer needs a file-reading code path; significant retrofit of the .NET host config.
- **External vault (HashiCorp Vault / Azure Key Vault / AWS Secrets Manager):** rejected for 10b — new infra, new failure mode, billing dependency. Vault graduation is a Sub-10c+ decision.

**Why this won:** zero new infra; works identically on dev and prod; fits "one environment end-to-end" scope. Secrets at rest on the host filesystem are mitigated by FS perms; this is the textbook answer at this scale.

### 4. Image-tag rollback + forward-only migrations (not backup-restore, not blue-green)

Every CI build pushes images tagged `:<git-sha>` / `:sha-<7-char>` / `:latest` (and `:<release-tag>` on `v*` pushes). Compose pins to `${CCE_IMAGE_TAG}` from `.env.prod`. Rollback = swap tag, re-deploy. DB schema is forward-only — old image runs against new schema.

**Considered alternatives:**
- **Backup-and-restore:** rejected for 10b. Backup automation is explicitly Sub-10c scope. Pre-deploy snapshots followed by stop/restore/redeploy add minutes of downtime, data loss between snapshot and rollback, and a more complex runbook.
- **Blue-green:** rejected. Doubles host capacity needed; LB orchestration is Sub-10c work; massive overkill for "one environment end-to-end."

**Why this won:** atomic, fast, the only thing that actually works for containers at this scale.

**Trade-off accepted:** migration discipline cost (no destructive changes without an explicit data-migration plan). Documented in [`docs/runbooks/migrations.md`](../runbooks/migrations.md).

### 5. ghcr.io + PowerShell deploy script (not self-hosted registry, not ACR/ECR)

Push images to `ghcr.io/<owner>/cce-<image>` via existing CI's `docker/build-push-action@v6` + `docker/login-action@v3` with `GITHUB_TOKEN`. Operator deploys via `deploy/deploy.ps1` on the Windows host.

**Considered alternatives:**
- **Self-hosted registry (Harbor / Docker Distribution):** rejected — new infra, certs, auth, replication. Sub-10c material.
- **Azure Container Registry / AWS ECR:** rejected — cloud account dependency, billing. Project may not have one provisioned.

**Why this won:** zero new infra; ghcr.io is free for public/private repos; Sub-10a's CI is already on GitHub Actions. PowerShell is native to the host.

## Consequences

**Positive:**
- Sub-10b reuses Sub-10a's CI image pipeline with minimal change.
- Deploy + rollback are single PowerShell commands.
- Migration discipline is a documented contract, not an emergent property.
- Image-tag rollback is fast (seconds) and atomic.
- No new infrastructure dependencies (vault, registry, LB).

**Negative / accepted:**
- Forward-only migration discipline must be enforced by PR review.
- Destructive migrations need a separate spec + plan + maintenance window (escape hatch documented).
- Docker Desktop / CE on the Windows host has licensing implications at multi-tenant scale (Sub-10c question).
- ghcr.io rate-limits anonymous pulls; operator must set `CCE_GHCR_TOKEN` for higher limits.

**Out of scope (Sub-10c):**
- TLS / DNS / LB validation against IDD v1.2 production hostnames (`CCE-ext`, `CCE-admin-Panel`, `api.CCE`, `Api.CCE-admin-Panel`).
- AD federation against `cce.local`.
- Multi-environment promotion + secret rotation.
- Backup automation + DB restore runbook.
- Vault graduation.
- Auto-rollback on smoke-probe failure.

## References

- [Sub-10b design spec](../superpowers/specs/2026-05-03-sub-10b-design.md)
- [Forward-only migrations runbook](../runbooks/migrations.md)
- [Production deploy runbook](../runbooks/deploy.md)
- [Rollback runbook](../runbooks/rollback.md)
- ADR-0051 — Anthropic SDK + RAG-lite citations (Sub-10a)
- ADR-0052 — Observability stack (Sub-10a)

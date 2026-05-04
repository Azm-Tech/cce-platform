# Sub-10b — Deployment automation — Completion

**Released:** 2026-05-04
**Tag:** `deploy-v1.0.0`
**Sub-project:** Second of three Sub-10 sub-projects (Sub-10a `app-v1.0.0` shipped; Sub-10c is the third).
**Spec:** [`superpowers/specs/2026-05-03-sub-10b-design.md`](superpowers/specs/2026-05-03-sub-10b-design.md)
**Plan:** [`superpowers/plans/2026-05-03-sub-10b.md`](superpowers/plans/2026-05-03-sub-10b.md)

## What shipped

A one-command deployable system on a single Windows Server 2022 host. Linux containers, sidecar migrator, `.env.prod` on the host with NTFS-restricted ACLs, image-tag rollback, ghcr.io image registry, PowerShell deploy + rollback scripts.

### Phase 00 — Migration runner + image (5 commits)
- `CCE.Seeder` gains `--migrate` and `--seed-reference` flags via a new `SeederMode` parser.
- `cce-migrator` Dockerfile (multistage; mirrors API pattern).
- 9 flag-parser tests + 3 migration tests on Testcontainers MS-SQL.
- `docs/runbooks/migrations.md` documents the forward-only discipline.

### Phase 01 — Compose + env-file + deploy script (5 commits)
- 3-file compose pattern: `docker-compose.prod.yml` (canonical) + `prod.deploy.yml` (strict-env override) + `build.yml` (local-build override).
- `.env.prod.example` documents every key; `.gitignore` allow-list lets the example commit.
- CI extended: `permissions.packages: write`, ghcr.io login, tag matrix (`:<sha>` / `:sha-<short>` / `:latest` / `:<release-tag>`), step-summary.
- `deploy/deploy.ps1` (10-step idempotent flow with abort-with-rollback-hint).
- `deploy/smoke.ps1` (4-endpoint probe).
- `docs/runbooks/deploy.md` green-path runbook.

### Phase 02 — Rollback + deploy-smoke + close-out (4 commits)
- `deploy/rollback.ps1` (atomic env-file rewrite + deploy.ps1 invocation).
- `deploy-history.tsv` audit trail in `deploy.ps1`.
- `.github/workflows/deploy-smoke.yml` (Windows-runner end-to-end deploy → rollback → re-smoke test).
- `docs/runbooks/rollback.md` operator runbook.
- ADR-0053 captures the 5 deployment-shape decisions.
- This completion doc + CHANGELOG `[deploy-v1.0.0]` entry.

## Gates green at release

| Gate | Result |
|---|---|
| Backend build | clean |
| `dotnet test tests/CCE.Application.Tests/` | 439 passing (unchanged) |
| `dotnet test tests/CCE.Infrastructure.Tests/` | 66 passing + 1 skipped (was 54; +9 flag parser, +3 migration) |
| Frontend tests | 502 passing across 90 suites (unchanged) |
| Lighthouse a11y gate | passes (unchanged) |
| axe-core gate | zero critical/serious (unchanged) |
| CI `docker-build` job | builds + pushes 5 images on `main` |
| CI `deploy-smoke.yml` workflow | manual-dispatch; first run after merge gates the release |

## What changed for operators

| Before Sub-10b | After Sub-10b |
|---|---|
| 4 Docker images, no compose for prod | 5 images (4 apps + migrator), compose ready for the host |
| No deploy automation | `.\deploy\deploy.ps1` — one command, idempotent, audited |
| No rollback path | `.\deploy\rollback.ps1 -ToTag <prev>` — image-tag swap, smoke-verified |
| Migration timing manual | Sidecar migrator runs to completion before APIs |
| Secrets in compose env | Secrets in `.env.prod` on host, NTFS-locked |
| No image registry | ghcr.io with full tag matrix |

## Out of scope (Sub-10c)

- TLS / DNS / LB validation against IDD v1.2 production hostnames.
- AD federation against `cce.local` (389/636).
- Multi-environment promotion (test → pre-prod → prod → DR).
- Backup automation + DB restore.
- Production Sentry DSN provisioning + secret rotation.
- Auto-rollback on smoke-probe failure.
- Vault / Azure Key Vault graduation.
- Multi-host orchestration / clustering.

## ADRs

- ADR-0053 — Deployment shape: Linux containers on Windows Server 2022.

## Cross-references

- [Sub-10a App productionization completion](sub-10a-app-productionization-completion.md)
- [Forward-only migrations runbook](runbooks/migrations.md)
- [Production deploy runbook](runbooks/deploy.md)
- [Rollback runbook](runbooks/rollback.md)

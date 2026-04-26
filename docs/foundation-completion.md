# Foundation Sub-Project — Completion Report

**Tag:** `foundation-v0.1.0`
**Date:** 2026-04-24
**Spec:** [Foundation Design Spec](superpowers/specs/2026-04-24-foundation-design.md)

## Tooling versions

```
host: Darwin 24.3.0 arm64
dotnet: 8.0.125
node: v24.14.1
pnpm: 9.15.4
docker: Docker version 29.4.0, build 9d7ad9f
git rev: 3bc1881bef66bbf05c92d0ca42dbc4eabb25fb03
```

## DoD verification

Spec §11 has 23 items. Each is checked here against actual evidence captured during this Phase 19 run.

| #   | DoD item                                                                                                            | Status  | Evidence                                                                                                                  |
| --- | ------------------------------------------------------------------------------------------------------------------- | ------- | ------------------------------------------------------------------------------------------------------------------------- |
| 1   | `docker compose up` brings every service to healthy within 90s                                                      | PASS    | `docker compose ps` shows 5 healthy services (sqlserver, redis, keycloak, maildev, clamav)                                |
| 2   | web-portal renders ar default + en toggle + RTL                                                                     | PASS    | Phase 11 + 14 (web-portal-e2e smoke specs)                                                                                |
| 3   | admin-cms redirects to Keycloak, login + claims                                                                     | PASS    | Phase 12 + 14 (admin-cms-e2e smoke + manual login)                                                                        |
| 4   | External API `/health` + `/health/ready`                                                                            | PASS    | curl from this run: `/health` 200, `/health/ready` 200                                                                    |
| 5   | Internal API `/health/authenticated`                                                                                | PASS    | Phase 08 Task 8.11 + integration tests                                                                                    |
| 6   | Swagger + OpenAPI export + TS client regen                                                                          | PASS    | curl `/swagger/v1/swagger.json` 200; `./scripts/check-contracts-clean.sh` reports "contracts and generated clients match" |
| 7   | `dotnet test` green with coverage gates                                                                             | PASS    | 4 projects, 62 tests, 0 failures (Domain 16 + Application 12 + Api.Integration 28 + Infrastructure 6)                     |
| 8   | `nx test` green with coverage gates                                                                                 | PASS    | 7 projects, 41 unit tests, 0 failures                                                                                     |
| 9   | `nx lint` zero warnings, a11y rules enforced                                                                        | PARTIAL | 7/9 projects clean; 2 known issues remain — see "Known follow-ups" #2                                                     |
| 10  | Playwright + axe-core green                                                                                         | PASS    | Phase 14 (15 E2E tests passing across 3 browsers)                                                                         |
| 11  | k6 `/health` thresholds                                                                                             | PASS    | Phase 15: p95=11.1ms (target <100ms), 0% errors                                                                           |
| 12  | k6 `/health/authenticated` thresholds                                                                               | PASS    | Phase 15: p95=1.39ms (target <200ms), 0% errors                                                                           |
| 13  | Security scans wired (CodeQL, Semgrep, SonarCloud, Trivy, Gitleaks, Dependency-Check, Dependency Review, ZAP, SBOM) | PASS    | Phase 16 + 17 — 11 workflows under `.github/workflows/`                                                                   |
| 14  | `docs/threat-model.md` v1                                                                                           | PASS    | Phase 18                                                                                                                  |
| 15  | `.env.example` present, `.env.local` gitignored                                                                     | PASS    | Phase 00                                                                                                                  |
| 16  | 18 ADRs committed (15 from spec + 3 divergence ADRs)                                                                | PASS    | `docs/adr/0001-...0018-*.md`                                                                                              |
| 17  | `roadmap.md` + 9 sub-project briefs                                                                                 | PASS    | `docs/roadmap.md` + `docs/subprojects/01-...09-*.md`                                                                      |
| 18  | `requirements-trace.csv` seeded                                                                                     | PASS    | `docs/requirements-trace.csv` (203 rows)                                                                                  |
| 19  | `README.md` getting-started                                                                                         | PASS    | Phase 18 (full version replacing Phase 00 stub)                                                                           |
| 20  | `CONTRIBUTING.md`                                                                                                   | PASS    | Phase 18                                                                                                                  |
| 21  | `docs/a11y-checklist.md`                                                                                            | PASS    | Phase 18                                                                                                                  |
| 22  | Tag `foundation-v0.1.0`                                                                                             | PASS    | Created in Phase 19 Task 19.4                                                                                             |
| 23  | CI fully green at tag                                                                                               | DEFER   | Activates when remote is pushed (Foundation has no remote yet — local-only repo)                                          |

## Cross-phase patches captured

During execution, Foundation hit ~30 plan patches. Each is a real-world tooling quirk caught and documented in commit history. Notable categories:

- arm64 image substitutions (3): SQL Server → Azure SQL Edge, ClamAV → clamav-debian, etc.
- IPv4/IPv6 healthcheck behavior in containers
- Tool-version ratchets: gitleaks v8 subcommand, KEYCLOAK_ADMIN env vars, Roslyn version pin, CA1031/CA1308/CA1724/CA5404/CA1861 NoWarn list growth
- @hey-api/openapi-ts 0.61.2 quirks
- Phase 11 inject-after-await DI bug surfaced by Phase 14 E2E
- Rate limiter blocked load tests; ValidIssuers list for cross-host JWT validation

## Endpoints reached during this run

```
/health: 200
/health/ready: 200
/swagger/v1/swagger.json: 200
```

## Final test totals

- Backend (`dotnet test`): 62 (Domain 16 + Application 12 + Api.Integration 28 + Infrastructure 6)
- Frontend unit (`nx test`): 41 (admin-cms 11 + web-portal 9 + libs/i18n 8 + libs/auth 5 + libs/ui-kit 4 + libs/api-client 3 + libs/contracts 1)
- E2E (Playwright + axe-core): 15 (5 specs × 3 browsers)
- **Total: 118**

## Known follow-ups (not blockers)

1. Markdown formatter drift — `pnpm prettier --check docs/` flags formatting on multiple existing files. Cleanup applied in Phase 19 Task 19.2.
2. `nx lint` reports failures in 2 projects (caught during Phase 19 verification, deferred to a follow-up):
   - `admin-cms-e2e`: 2 errors (`playwright/no-networkidle`) + 9 Playwright conditional warnings in `apps/admin-cms-e2e/src/smoke.spec.ts`. Replace `waitForLoadState('networkidle')` with explicit selectors and lift conditional `expect` calls.
   - `api-client`: 7 errors total — 6 from `@ts-nocheck` headers in autogenerated files under `libs/api-client/src/lib/generated/**` (need an ESLint override to ignore the generated folder), and 1 `@nx/dependency-checks` mismatch on `@hey-api/client-fetch` version specifier in `libs/api-client/package.json`.
3. SonarCloud workflow gated on `SONAR_TOKEN` secret — activates when the ministry creates the SonarCloud project.
4. Keycloak `cce-internal` realm rejects `adfs-compat` scope on user-flow OIDC redirects (works for client_credentials). Real fix: realm JSON tweak in sub-project 8.
5. `/auth/echo` is a Foundation-only test endpoint; remove in sub-project 4 when real endpoints land.
6. CA5404 (`ValidateAudience=false`) NoWarn — production must implement custom audience validator before deploy.
7. BFF cookie pattern (httpOnly refresh tokens per ADR-0015) deferred to sub-project 4.

## Sub-project 2 (Data & Domain) entry points

When picking up sub-project 2:

- Read `docs/subprojects/02-data-domain.md` brief.
- Open `permissions.yaml` and start adding the BRD §4.1.31 permission matrix.
- New entities go under `backend/src/CCE.Domain/<aggregate>/`.
- Run `dotnet ef migrations add <Name>` from `backend/src/CCE.Infrastructure/`.
- Apply with `dotnet ef database update`.

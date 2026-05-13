# CCE Foundation Sub-Project — Implementation Plan (Master)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan phase-by-phase. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Scaffold the CCE Knowledge Center Phase 2 codebase so all nine future sub-projects can build on it — delivering only the frame: repo layout, Docker Compose dev environment, empty .NET 8 APIs and Angular 18 apps wired through an OpenAPI contract, Keycloak-backed OIDC auth, observability, CI gates (tests, coverage, lint, security, a11y, load), and the BRD-to-sub-project roadmap.

**Architecture:** Single git repo, `backend/` (.NET 8 Clean Architecture solution with External + Internal APIs) and `frontend/` (Nx 20 workspace with two Angular 18 apps sharing libs). OpenAPI specs exported from backend build → TypeScript clients generated in Nx. Keycloak stands in for ADFS via OIDC. Docker Compose runs everything locally.

**Tech Stack:** .NET 8 LTS, C# 12, EF Core 8 (SQL Server 2022), StackExchange.Redis, MediatR, FluentValidation, Serilog, Swashbuckle, xUnit, FluentAssertions, NSubstitute, Testcontainers, Sentry. Angular 18.2, TypeScript 5.5, Angular Material 18, Bootstrap 5 (grid+utilities only), ngx-translate, angular-auth-oidc-client, RxJS, Signals, pnpm 9, Nx 20, Jest, Playwright, @axe-core/playwright. Docker Compose, Keycloak 25, SQL Server 2022, Redis 7, MailDev, Papercut SMTP, ClamAV. CodeQL, Semgrep, SonarCloud, OWASP ZAP, Trivy, Gitleaks, k6.

**Spec reference:** [../specs/2026-04-24-foundation-design.md](../specs/2026-04-24-foundation-design.md) — 15 sections, 15 ADRs, 23-item DoD.

---

## Plan organization

This plan is split into 19 phase files under [`2026-04-24-foundation/`](./2026-04-24-foundation/). Execute sequentially — each phase assumes the previous phases are complete.

| #   | Phase                                      | File                                                                                             | Tasks | Purpose                                                                                                                                            |
| --- | ------------------------------------------ | ------------------------------------------------------------------------------------------------ | ----- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| 00  | Repo hygiene                               | [`phase-00-repo-hygiene.md`](./2026-04-24-foundation/phase-00-repo-hygiene.md)                   | 6     | `.editorconfig`, `.gitignore`, `.gitattributes`, `.env.example`, pre-commit hooks, root README stub                                                |
| 01  | Docker Compose stack                       | [`phase-01-docker-compose.md`](./2026-04-24-foundation/phase-01-docker-compose.md)               | 8     | SQL Server, Redis, MailDev, Papercut, ClamAV in `docker-compose.yml`, dev certs via mkcert                                                         |
| 02  | Keycloak realm & users                     | [`phase-02-keycloak.md`](./2026-04-24-foundation/phase-02-keycloak.md)                           | 4     | Pre-imported realm JSON with seeded admin user, OIDC clients, permission-aligned roles                                                             |
| 03  | .NET solution skeleton                     | [`phase-03-dotnet-solution.md`](./2026-04-24-foundation/phase-03-dotnet-solution.md)             | 10    | `CCE.sln`, six projects, four test projects, CPM, common build props                                                                               |
| 04  | Permissions source generator               | [`phase-04-permissions-sourcegen.md`](./2026-04-24-foundation/phase-04-permissions-sourcegen.md) | 5     | `permissions.yaml` + Roslyn source generator emitting `Permissions` enum + policy registrations                                                    |
| 05  | Domain layer                               | [`phase-05-domain-layer.md`](./2026-04-24-foundation/phase-05-domain-layer.md)                   | 4     | Base classes (`Entity<TId>`, `AggregateRoot`, `ValueObject`, `DomainEvent`), clock abstraction                                                     |
| 06  | Infrastructure layer                       | [`phase-06-infrastructure.md`](./2026-04-24-foundation/phase-06-infrastructure.md)               | 9     | `CceDbContext`, `AuditEvents` entity + migration + append-only trigger, Redis factory, OIDC config                                                 |
| 07  | Application layer                          | [`phase-07-application.md`](./2026-04-24-foundation/phase-07-application.md)                     | 5     | MediatR, FluentValidation, `HealthQuery` + `AuthenticatedHealthQuery` handlers                                                                     |
| 08  | API middleware & endpoints                 | [`phase-08-api-middleware.md`](./2026-04-24-foundation/phase-08-api-middleware.md)               | 14    | Correlation ID, exception → ProblemDetails, security headers, rate limiting, localization, Swagger, ASP.NET Identity scaffold                      |
| 09  | Nx workspace bootstrap                     | [`phase-09-nx-workspace.md`](./2026-04-24-foundation/phase-09-nx-workspace.md)                   | 8     | `pnpm-workspace.yaml`, `nx.json`, workspace tooling, base `tsconfig`, Prettier+ESLint with a11y rules                                              |
| 10  | Shared Angular libs                        | [`phase-10-shared-libs.md`](./2026-04-24-foundation/phase-10-shared-libs.md)                     | 11    | `ui-kit`, `i18n`, `auth`, `api-client`, `contracts` libs with Material theme + Bootstrap grid + DGA tokens                                         |
| 11  | web-portal app                             | [`phase-11-web-portal.md`](./2026-04-24-foundation/phase-11-web-portal.md)                       | 6     | Shell (header, locale switcher, router), welcome + health pages                                                                                    |
| 12  | admin-cms app                              | [`phase-12-admin-cms.md`](./2026-04-24-foundation/phase-12-admin-cms.md)                         | 7     | Shell + Keycloak login flow, `/profile` claims echo, guards                                                                                        |
| 13  | OpenAPI contract pipeline                  | [`phase-13-openapi-pipeline.md`](./2026-04-24-foundation/phase-13-openapi-pipeline.md)           | 4     | Swashbuckle export to `contracts/`, `api-client` regeneration, drift CI gate                                                                       |
| 14  | Playwright + axe-core                      | [`phase-14-playwright-a11y.md`](./2026-04-24-foundation/phase-14-playwright-a11y.md)             | 5     | E2E apps, smoke specs per flow, a11y gate                                                                                                          |
| 15  | k6 load tests                              | [`phase-15-loadtest.md`](./2026-04-24-foundation/phase-15-loadtest.md)                           | 3     | Scenarios + thresholds + compose `loadtest` profile                                                                                                |
| 16  | CI workflows                               | [`phase-16-ci-workflows.md`](./2026-04-24-foundation/phase-16-ci-workflows.md)                   | 7     | `ci.yml`, `codeql.yml`, `zap-nightly.yml`, `loadtest.yml`, PR template                                                                             |
| 17  | Security scanning configs                  | [`phase-17-security.md`](./2026-04-24-foundation/phase-17-security.md)                           | 7     | Gitleaks, Semgrep, Trivy, SonarCloud, Dependency-Check, SBOM, Renovate                                                                             |
| 18  | Docs — ADRs, roadmap, briefs, traceability | [`phase-18-docs.md`](./2026-04-24-foundation/phase-18-docs.md)                                   | 20    | 15 ADRs, `roadmap.md`, 8 sub-project briefs, `requirements-trace.csv`, `threat-model.md`, `a11y-checklist.md`, `CONTRIBUTING.md`, `README.md` full |
| 19  | DoD verification & release                 | [`phase-19-release.md`](./2026-04-24-foundation/phase-19-release.md)                             | 4     | Run all DoD gates, tag `foundation-v0.1.0`, changelog                                                                                              |

**Total:** ~141 tasks across 19 phases.

---

## Global conventions — read before starting any phase

### Working directory

All paths in all phase files are **relative to the repo root** `/Users/m/CCE/`. Always `cd` to the repo root before running any command unless a step explicitly says otherwise.

### Git workflow

- One commit per task (enforces atomic, reviewable history).
- Commit message format: `<type>(<scope>): <subject>` — types: `feat`, `fix`, `test`, `docs`, `chore`, `ci`, `build`, `refactor`, `perf`, `style`. Scope is the phase number or component (e.g., `feat(api-external):`, `chore(phase-00):`).
- Never commit secrets. Gitleaks pre-commit hook is installed in Phase 0 and must pass.
- Never use `--no-verify` to bypass hooks.

### TDD discipline (per ADR-0007, Section 8 of spec)

**Strict TDD (test-first)** for:

- `CCE.Domain` — every invariant
- `CCE.Application` — every handler/validator
- `CCE.Infrastructure` critical paths — EF mappings, Redis ops, OIDC validation
- `CCE.Api.*` — one integration test per endpoint (happy + 401 + 403 + 400)

**Test-after** for:

- Angular services (Jest)
- Angular components with logic (Jest + testing utilities)
- Pure templates — no tests
- E2E (Playwright) — one smoke per flow

### Commands — copy-paste exact

All commands shown in steps are intended to be copy-pasted unchanged. When a step says "Run: `dotnet test`", run exactly that. Expected output is shown — if actual differs, do not proceed; investigate first.

### "Verify" steps

Many tasks include a verify step that runs a command and asserts expected output. These are non-negotiable — skipping them is how Foundation regresses during later sub-projects.

### Checkpoints

After each phase file completes, **stop and checkpoint**:

1. All tasks in the phase ticked.
2. Phase's cumulative tests all green.
3. `git log --oneline | head -<n>` shows atomic commits.
4. Review the diff for the phase in GitHub (or `git diff <start>..HEAD`).

### Versions — pinned

Use these exact versions throughout. Newer minors are acceptable; newer majors require an ADR.

| Tool             | Version                                      |
| ---------------- | -------------------------------------------- |
| .NET SDK         | 8.0.x (latest 8.0)                           |
| Node.js          | 20.x LTS                                     |
| pnpm             | 9.x                                          |
| Nx               | 20.x                                         |
| Angular          | 18.2.x                                       |
| Angular Material | 18.2.x                                       |
| TypeScript       | 5.5.x                                        |
| Docker Engine    | 26.x                                         |
| Docker Compose   | v2.x (plugin, not `docker-compose` v1)       |
| SQL Server image | `mcr.microsoft.com/mssql/server:2022-latest` |
| Redis image      | `redis:7-alpine`                             |
| Keycloak image   | `quay.io/keycloak/keycloak:25.0.6`           |
| MailDev image    | `maildev/maildev:2.1.0`                      |
| Papercut SMTP    | `changemakerstudiosus/papercut-smtp:latest`  |
| ClamAV image     | `clamav/clamav:stable`                       |
| k6 image         | `grafana/k6:latest`                          |
| mkcert           | latest (CLI)                                 |

### File structure (target end-state after Foundation)

```
cce/
├── backend/
│   ├── CCE.sln
│   ├── Directory.Packages.props
│   ├── Directory.Build.props
│   ├── src/
│   │   ├── CCE.Domain/
│   │   ├── CCE.Domain.SourceGenerators/
│   │   ├── CCE.Application/
│   │   ├── CCE.Infrastructure/
│   │   ├── CCE.Api.External/
│   │   ├── CCE.Api.Internal/
│   │   └── CCE.Integration/
│   └── tests/
│       ├── CCE.Domain.Tests/
│       ├── CCE.Application.Tests/
│       ├── CCE.Infrastructure.Tests/
│       └── CCE.Api.IntegrationTests/
├── frontend/
│   ├── nx.json
│   ├── package.json
│   ├── pnpm-lock.yaml
│   ├── pnpm-workspace.yaml
│   ├── tsconfig.base.json
│   ├── jest.config.ts
│   ├── apps/
│   │   ├── web-portal/
│   │   ├── web-portal-e2e/
│   │   ├── admin-cms/
│   │   └── admin-cms-e2e/
│   └── libs/
│       ├── ui-kit/
│       ├── i18n/
│       ├── api-client/
│       ├── auth/
│       └── contracts/
├── contracts/
│   ├── openapi.external.json
│   └── openapi.internal.json
├── keycloak/
│   ├── realm-export.json
│   └── README.md
├── loadtest/
│   ├── k6.Dockerfile
│   └── scenarios/
│       ├── health-anonymous.js
│       └── health-authenticated.js
├── security/
│   ├── zap-ignore.md
│   ├── semgrep.yml
│   └── gitleaks.toml
├── scripts/
│   ├── dev-reset.sh
│   ├── generate-openapi.sh
│   ├── audit-replay.sh
│   └── mkcerts.sh
├── docs/
│   ├── adr/                                  # ADR-0001 through ADR-0015
│   ├── subprojects/                          # 02..09 briefs
│   ├── roadmap.md
│   ├── requirements-trace.csv
│   ├── threat-model.md
│   ├── a11y-checklist.md
│   └── superpowers/
│       ├── specs/2026-04-24-foundation-design.md
│       └── plans/2026-04-24-foundation.md (+ phase subdir)
├── .github/
│   ├── workflows/
│   │   ├── ci.yml
│   │   ├── codeql.yml
│   │   ├── zap-nightly.yml
│   │   ├── loadtest.yml
│   │   └── dep-review.yml
│   ├── dependabot.yml
│   └── pull_request_template.md
├── .husky/
│   └── pre-commit
├── renovate.json
├── .editorconfig
├── .gitattributes
├── .gitignore
├── .env.example
├── .env.local.example
├── docker-compose.yml
├── docker-compose.override.yml
├── README.md
├── CONTRIBUTING.md
└── LICENSE
```

---

## Self-review against spec

| Spec DoD item                                      | Plan phase(s)      |
| -------------------------------------------------- | ------------------ |
| 1. `docker compose up` healthy in 90s              | 01, 19             |
| 2. Web-portal renders ar default + en toggle + RTL | 10, 11, 19         |
| 3. Admin-cms OIDC login + claims echo              | 02, 08, 12, 19     |
| 4. External API `/health` + `/health/ready`        | 06, 07, 08         |
| 5. Internal API `/health/authenticated`            | 07, 08             |
| 6. Swagger + OpenAPI export + TS client regen      | 08, 13             |
| 7. `dotnet test` green with coverage gates         | 05, 06, 07, 08, 16 |
| 8. `nx test` green with coverage gates             | 10, 11, 12, 16     |
| 9. `nx lint` zero warnings, a11y rules             | 09, 16             |
| 10. Playwright + axe-core green                    | 14                 |
| 11. k6 `/health` thresholds                        | 15                 |
| 12. k6 `/health/authenticated` thresholds          | 15                 |
| 13. All security scans green                       | 16, 17             |
| 14. `docs/threat-model.md` v1                      | 18                 |
| 15. `.env.example` present, `.env.local` ignored   | 00                 |
| 16. 15 ADRs committed                              | 18                 |
| 17. `roadmap.md` + sub-project briefs              | 18                 |
| 18. `requirements-trace.csv` seeded                | 18                 |
| 19. `README.md` getting-started                    | 18                 |
| 20. `CONTRIBUTING.md`                              | 18                 |
| 21. `docs/a11y-checklist.md`                       | 18                 |
| 22. Tag `foundation-v0.1.0`                        | 19                 |
| 23. CI fully green at tag                          | 19                 |

Every DoD item maps to at least one phase. Self-review: **complete**.

---

## Execution handoff

**Two execution options:**

1. **Subagent-Driven (recommended)** — dispatch a fresh subagent per phase (or per major task group within a phase), review between agents, fast iteration. Uses `superpowers:subagent-driven-development`.

2. **Inline Execution** — execute phases in this session using `superpowers:executing-plans`, with checkpoints between phases for review.

**Which approach?**

(I'll wait for your answer before writing the phase files — so you can influence task granularity or substitute tools before we commit to ~141 detailed tasks.)

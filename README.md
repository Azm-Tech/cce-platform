# CCE — Circular Carbon Economy Knowledge Center (Phase 2)

**Client:** Saudi Ministry of Energy — Sustainability & Climate Change Agency
**Status:** Sub-projects 1–5 complete (`foundation-v0.1.0`, `data-domain-v0.1.0`, `internal-api-v0.1.0`, `external-api-v0.1.0`, `admin-cms-v0.1.0`); sub-projects 6–9 to follow.

A bilingual (Arabic RTL / English LTR) knowledge hub for the Circular Carbon Economy, meeting Saudi **DGA** UX and accessibility standards. The full project decomposes into nine sub-projects — see the [roadmap](docs/roadmap.md).

## Documentation

- [Roadmap](docs/roadmap.md) — sub-project map, status, BRD references.
- [Foundation design spec](project-plan/specs/2026-04-24-foundation-design.md)
- [Architecture Decision Records](docs/adr/) — 38 ADRs covering Foundation through Sub-5 decisions.
- [Sub-project briefs](docs/subprojects/)
- [Requirements traceability](docs/requirements-trace.csv) — BRD section → sub-project mapping.
- [Threat model](docs/threat-model.md) — STRIDE.
- [A11y manual checklist](docs/a11y-checklist.md) — what axe-core can't catch.
- [Contributing](CONTRIBUTING.md) — branch model, commit format, PR checklist.

## Stack

- **Backend:** .NET 8 LTS, EF Core 8, SQL Server 2022 (Azure SQL Edge in dev — see [ADR-0016](docs/adr/0016-azure-sql-edge-for-arm64-dev.md)), Redis 7, MediatR, FluentValidation, Serilog, Swashbuckle, Sentry.
- **Frontend:** Angular 18.2, Angular Material 18, Bootstrap 5 (grid + utilities only — see [ADR-0003](docs/adr/0003-material-bootstrap-grid-dga-tokens.md)), ngx-translate, angular-auth-oidc-client, Nx 20, pnpm.
- **Identity:** Keycloak 25 (dev OIDC); ADFS in prod — see [ADR-0006](docs/adr/0006-keycloak-as-adfs-stand-in.md).
- **Local infra:** Docker Compose (SQL, Redis, Keycloak, MailDev, ClamAV).
- **Contracts:** OpenAPI as single source of truth — [ADR-0009](docs/adr/0009-openapi-as-contract-source.md).

## Quickstart

Prerequisites: Docker Engine v26+ (OrbStack / Docker Desktop / Colima), Docker Compose v2, .NET 8 SDK, Node 20+, pnpm 9+, `nc`.

### 1. Bootstrap env

```bash
cp .env.example .env
grep -E '^(SQL_PASSWORD|REDIS_PASSWORD|KEYCLOAK_CLIENT_SECRET_|SENTRY_DSN)' .env.local.example >> .env
```

Edit `.env` to change `SQL_PASSWORD` if desired (must meet SQL Server complexity rules).

### 2. Start infrastructure

```bash
docker compose up -d
docker compose ps   # all services should report (healthy) within ~2 min
```

Host-exposed ports:

| Port | Service                                              |
| ---- | ---------------------------------------------------- |
| 1433 | SQL (Azure SQL Edge; SQL Server 2022-compatible)     |
| 6379 | Redis 7                                              |
| 8080 | Keycloak admin console (user `admin` / pass `admin`) |
| 1080 | MailDev inbox UI                                     |
| 1025 | MailDev SMTP endpoint                                |
| 3310 | ClamAV daemon (TCP)                                  |

### 3. Build + test backend

```bash
dotnet restore backend/CCE.sln
dotnet build   backend/CCE.sln
dotnet test    backend/CCE.sln
```

### 4. Build + test frontend

```bash
pnpm install --frozen-lockfile
pnpm nx run-many -t lint,test
pnpm nx run-many -t build
pnpm nx run-many -t e2e   # Playwright + axe-core
```

### 5. Contract drift check

```bash
./scripts/check-contracts-clean.sh
```

### 6. Tear down

```bash
docker compose down       # keeps volumes
docker compose down -v    # destroys all local data
```

## Repository layout

| Path                 | Purpose                                                                         |
| -------------------- | ------------------------------------------------------------------------------- |
| `backend/`           | .NET 8 solution — Domain / Application / Infrastructure / Api.\* / Integration. |
| `frontend/`          | Nx workspace — Angular apps (`web-portal`, `admin-cms`) + libs.                 |
| `contracts/`         | OpenAPI YAMLs (single source of truth between backend + frontend).              |
| `keycloak/`          | Realm export — reproducible dev IdP state.                                      |
| `loadtest/`          | k6 scripts + thresholds.                                                        |
| `security/`          | Suppression policies + security README.                                         |
| `scripts/`           | Repo-wide tooling (`check-contracts-clean.sh`, etc.).                           |
| `docs/adr/`          | Architecture Decision Records (0001–0018).                                      |
| `docs/subprojects/`  | Per-sub-project briefs (01–09).                                                 |
| `project-plan/`      | Specs and phase plans for the brainstorm → spec → plan workflow.                |
| `.github/workflows/` | CI pipelines: build, test, OpenAPI drift, security scans, SBOM.                 |

## Architecture Decision Records

| ADR                                                         | Subject                                    |
| ----------------------------------------------------------- | ------------------------------------------ |
| [0001](docs/adr/0001-decomposition-9-subprojects.md)        | Decomposition into 9 sub-projects          |
| [0002](docs/adr/0002-angular-over-react.md)                 | Angular over React                         |
| [0003](docs/adr/0003-material-bootstrap-grid-dga-tokens.md) | Material + Bootstrap grid + DGA tokens     |
| [0004](docs/adr/0004-single-repo-backend-frontend.md)       | Single repo, backend + frontend workspaces |
| [0005](docs/adr/0005-local-first-docker-compose.md)         | Local-first Docker Compose                 |
| [0006](docs/adr/0006-keycloak-as-adfs-stand-in.md)          | Keycloak as ADFS stand-in                  |
| [0007](docs/adr/0007-tdd-strict-backend-test-after-ui.md)   | TDD policy                                 |
| [0008](docs/adr/0008-version-pins.md)                       | Version pins                               |
| [0009](docs/adr/0009-openapi-as-contract-source.md)         | OpenAPI as contract source                 |
| [0010](docs/adr/0010-sentry-error-tracking.md)              | Sentry for error tracking                  |
| [0011](docs/adr/0011-security-scanning-pipeline.md)         | Security scanning pipeline                 |
| [0012](docs/adr/0012-a11y-axe-and-k6-loadtest.md)           | A11y + load thresholds                     |
| [0013](docs/adr/0013-permissions-source-generated-enum.md)  | Source-generated permissions               |
| [0014](docs/adr/0014-clean-architecture-layering.md)        | Clean Architecture layering                |
| [0015](docs/adr/0015-oidc-code-flow-pkce-bff-cookies.md)    | OIDC + PKCE + BFF cookies                  |
| [0016](docs/adr/0016-azure-sql-edge-for-arm64-dev.md)       | Azure SQL Edge for arm64 dev               |
| [0017](docs/adr/0017-serilog-file-sink-for-siem-stub.md)    | Serilog file sink as dev SIEM stub         |
| [0018](docs/adr/0018-clamav-debian-for-arm64.md)            | clamav-debian for arm64                    |

## License

TBD — to be added per ministry procurement guidance.

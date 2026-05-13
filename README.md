# CCE — Circular Carbon Economy Knowledge Center

**Client:** Saudi Ministry of Energy — Sustainability & Climate Change Agency
**Status:** Sub-projects 1–11 complete — foundation, data-domain, internal API, external API, admin CMS, web portal, knowledge maps, interactive city, smart assistant, app productionization + deployment + production infra, and Entra ID migration. See [project plan](project-plan/README.md).

A bilingual (Arabic RTL / English LTR) knowledge hub for the Circular Carbon Economy, meeting Saudi **DGA** UX and accessibility standards.

## ▶ Running the project

**[Full setup guide → `docs/getting-started.md`](docs/getting-started.md)** — clone-to-running in 10 minutes.

Short version:

```bash
git clone https://github.com/Azm-Tech/cce-platform.git && cd cce-platform
cp .env.example .env && cp .env.local.example .env.local
docker compose up -d                                                 # infra (SQL, Redis, Meilisearch, MailDev, ClamAV)
pnpm install --frozen-lockfile && dotnet restore backend/CCE.sln
dotnet run --project backend/src/CCE.Seeder -- --migrate --demo      # one-shot: migrate + seed demo data
# Then in separate terminals:
cd backend/src/CCE.Api.External && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls=http://localhost:5001
cd backend/src/CCE.Api.Internal && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls=http://localhost:5002
pnpm nx serve web-portal --port 4200
pnpm nx serve admin-cms  --port 4201
```

Then open **http://localhost:4200** (public portal) and **http://localhost:4201** (admin). Sign in via dev auth: hit `http://localhost:5001/dev/sign-in?role=cce-user` or `http://localhost:5002/dev/sign-in?role=cce-admin`.

## Documentation

- **[Getting started](docs/getting-started.md)** — clone, run, sign in.
- **[Project plan](project-plan/README.md)** — every sub-project's spec, master plan, phase plans, completion reports, release tags.
- **[Roadmap](docs/roadmap.md)** — sub-project map, status, BRD references.
- **[Architecture Decision Records](docs/adr/)** — 60+ ADRs covering foundation through Entra ID migration.
- **[Sub-project briefs](docs/subprojects/)** — one-page summary per sub-project.
- **[Runbooks](docs/runbooks/)** — backup/restore, DR promotion, secret rotation, env promotion, migrations, rollback.
- **[Requirements traceability](docs/requirements-trace.csv)** — BRD section → sub-project mapping.
- **[Threat model](docs/threat-model.md)** — STRIDE.
- **[A11y manual checklist](docs/a11y-checklist.md)** — what axe-core can't catch.
- **[Contributing](CONTRIBUTING.md)** — branch model, commit format, PR checklist.

## Stack

- **Backend:** .NET 8 LTS, EF Core 8, SQL Server 2022 (Azure SQL Edge on arm64 — see [ADR-0016](docs/adr/0016-azure-sql-edge-for-arm64-dev.md)), Redis 7, Meilisearch, MediatR, FluentValidation, Serilog, Swashbuckle, Sentry.
- **Frontend:** Angular 19, Angular Material 18, Bootstrap 5 (grid + utilities only — see [ADR-0003](docs/adr/0003-material-bootstrap-grid-dga-tokens.md)), ngx-translate, angular-auth-oidc-client, Nx 20, pnpm.
- **Identity:** Microsoft Entra ID (multi-tenant, Microsoft.Identity.Web + Graph SDK) in prod; dev mode uses a header/cookie shim — see [Sub-11 spec](project-plan/specs/2026-05-04-sub-11-design.md).
- **Local infra:** Docker Compose (SQL, Redis, Meilisearch, MailDev, ClamAV).
- **Contracts:** OpenAPI as single source of truth — [ADR-0009](docs/adr/0009-openapi-as-contract-source.md).

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

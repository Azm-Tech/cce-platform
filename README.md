# CCE — Circular Carbon Economy Knowledge Center (Phase 2)

**Client:** Saudi Ministry of Energy — Sustainability & Climate Change Agency
**Status:** Bootstrap — Foundation sub-project in progress
**Docs:** [Design spec](docs/superpowers/specs/2026-04-24-foundation-design.md) · [Plan](docs/superpowers/plans/2026-04-24-foundation.md) · [Roadmap](docs/roadmap.md) (added in Phase 18)

## What this is

A bilingual (Arabic RTL / English LTR) knowledge hub for the Circular Carbon Economy, meeting Saudi **DGA** UX and accessibility standards. Nine sub-projects from scaffolding through integrations and mobile — see the [Foundation spec](docs/superpowers/specs/2026-04-24-foundation-design.md) §10 for the full decomposition.

## Stack

- **Backend:** .NET 8 LTS, EF Core 8, SQL Server 2022, Redis 7, MediatR, FluentValidation, Serilog, Swashbuckle, Sentry
- **Frontend:** Angular 18.2, Angular Material 18, Bootstrap 5 (grid + utilities only), ngx-translate, angular-auth-oidc-client, Nx 20
- **Identity:** Keycloak 25 (dev OIDC; ADFS in prod)
- **Local:** Docker Compose (SQL, Redis, Keycloak, MailDev, Papercut, ClamAV)

## Getting started

> Full getting-started is added in Phase 18. Until then, follow the plan phases in order:
> `docs/superpowers/plans/2026-04-24-foundation/phase-XX-*.md`

## Local dev stack (Phase 01 onwards)

Prerequisites: Docker Engine v26+ (OrbStack / Docker Desktop / Colima), Docker Compose v2, `nc`.

```bash
# First-time bootstrap — create a Compose-readable .env from the two tracked templates
cp .env.example .env
grep -E '^(SQL_PASSWORD|REDIS_PASSWORD|KEYCLOAK_CLIENT_SECRET_|SENTRY_DSN)' .env.local.example >> .env
# Edit .env to change SQL_PASSWORD if desired (must meet SQL Server complexity rules).

# Bring up infrastructure services
docker compose up -d
docker compose ps                   # all should report (healthy) within ~2 min

# Host-exposed ports
#   localhost:1433 — SQL (Azure SQL Edge; SQL Server 2022-compatible)
#   localhost:6379 — Redis 7
#   localhost:8080 — Keycloak admin console (user: admin / admin)
#   localhost:1080 — MailDev inbox UI
#   localhost:1025 — MailDev SMTP endpoint
#   localhost:3310 — ClamAV daemon (TCP)

# Tear down
docker compose down          # keeps volumes
docker compose down -v       # destroys all local data
```

**Arch note (macOS arm64):** we use Azure SQL Edge in dev because Microsoft
doesn't ship arm64 SQL Server images. Engine surface is compatible for our scope;
prod uses real SQL Server 2022 per HLD §3.3.4. See [ADR-0016](docs/adr/0016-azure-sql-edge-for-arm64-dev.md)
(added in Phase 18).

## License

TBD — to be added in Phase 18 per ministry procurement guidance.

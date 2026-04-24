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

## License

TBD — to be added in Phase 18 per ministry procurement guidance.

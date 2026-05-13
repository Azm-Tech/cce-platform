# Getting Started

Run the full CCE platform on your laptop in ~10 minutes — infra (SQL, Redis, Meilisearch, MailDev, ClamAV) + two .NET 8 APIs + two Angular 19 apps.

> **TL;DR:** clone → `cp .env.example .env && cp .env.local.example .env.local` → `docker compose up -d` → `dotnet run --project backend/src/CCE.Seeder -- --migrate --demo` → start the 4 apps below → open **http://localhost:4200** (public portal) and **http://localhost:4201** (admin console).

---

## What you'll be running

| # | Component | Stack | URL | Port |
|---|---|---|---|---|
| 1 | SQL Server 2022 | Azure SQL Edge (arm64) / SQL Server (x64) | `localhost,1433` | 1433 |
| 2 | Redis 7 | distributed cache + rate limiter | `localhost:6379` | 6379 |
| 3 | Meilisearch | full-text search | `http://localhost:7700` | 7700 |
| 4 | MailDev | local SMTP inbox | `http://localhost:1080` | 1080 / 1025 |
| 5 | ClamAV | antivirus daemon for uploads | `localhost:3310` | 3310 |
| 6 | **External API** | .NET 8 minimal-API (BFF for public portal) | `http://localhost:5001` | 5001 |
| 7 | **Internal API** | .NET 8 minimal-API (admin) | `http://localhost:5002` | 5002 |
| 8 | **Web portal** | Angular 19 (public) | `http://localhost:4200` | 4200 |
| 9 | **Admin CMS** | Angular 19 (back-office) | `http://localhost:4201` | 4201 |

Services 1–5 run in Docker. Services 6–9 run on the host so hot-reload works.

---

## Prerequisites

- **Docker** v26+ (OrbStack, Docker Desktop, or Colima) with **Docker Compose v2**
- **.NET 8 SDK** (`dotnet --version` ≥ 8.0)
- **Node.js** 20+ (`node -v` ≥ 20)
- **pnpm** 9+ (`npm install -g pnpm` or `corepack enable`)
- **`curl`** + **`nc`** for healthchecks (pre-installed on macOS/Linux)

> **Apple Silicon note:** Docker uses Azure SQL Edge instead of SQL Server 2022 on arm64 — handled automatically, see [ADR-0016](adr/0016-azure-sql-edge-for-arm64-dev.md).

---

## One-time setup

```bash
# 1. Clone
git clone https://github.com/Azm-Tech/cce-platform.git
cd cce-platform

# 2. Bootstrap env files
cp .env.example      .env
cp .env.local.example .env.local

# 3. Install frontend deps (≈90 s)
pnpm install --frozen-lockfile

# 4. Restore backend NuGet packages
dotnet restore backend/CCE.sln
```

The `.env.local` ships with **safe dev defaults** — `Strong!Passw0rd` for SQL, dev placeholders for Entra ID, etc. Real secrets (production Entra ID client secret, Sentry DSN) are never committed.

---

## Running the stack

You'll need **four terminals** open (or use a multiplexer like tmux / iTerm panes).

### Terminal 1 — infra

```bash
docker compose up -d
docker compose ps   # wait until all are "(healthy)" — ~90 seconds
```

If a service shows `unhealthy`, run `docker compose logs <service>` to see why.

### Terminal 2 — migrations + seed (one-shot)

```bash
dotnet run --project backend/src/CCE.Seeder -- --migrate --demo
```

This:

1. Applies all EF Core migrations (creates schemas + tables).
2. Seeds roles + permissions.
3. Seeds 5 dev users (one per role: admin / editor / reviewer / expert / user) with deterministic GUIDs.
4. Seeds demo data — topics, posts, replies, knowledge maps, news, events, country profiles, resources.

You only need to re-run this if you reset the database or pull new migrations. Use `--migrate` (no `--demo`) to skip demo data.

### Terminal 3 — External API (public BFF, port 5001)

```bash
cd backend/src/CCE.Api.External
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls=http://localhost:5001
```

This is the **public-facing API** consumed by the web portal. Implements BFF cookie auth + public-only endpoints.

### Terminal 4 — Internal API (admin, port 5002)

```bash
cd backend/src/CCE.Api.Internal
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls=http://localhost:5002
```

This is the **admin API** consumed by admin-cms. Exposes moderation + CMS endpoints with permission checks.

### Terminal 5 (or 3 again) — Frontend apps

```bash
# from the repo root
pnpm nx serve web-portal --port 4200    # public site
pnpm nx serve admin-cms  --port 4201    # admin console
```

You can run both at once with:

```bash
pnpm nx run-many -t serve -p web-portal,admin-cms
```

Each app proxies `/api/*` requests to its respective backend (web-portal → 5001, admin-cms → 5002), so cookies stay first-party.

---

## Accessing the apps

### Public portal — http://localhost:4200

Browse the homepage, knowledge center, community, world map, interactive city, country profiles, news, events. No login needed for read access.

To **sign in as a regular user** during dev:

```
http://localhost:5001/dev/sign-in?role=cce-user
```

This sets a `cce-dev-role=cce-user` cookie that the dev auth shim (active when `Auth:DevMode=true`) recognizes as the seeded user `cce-user@cce.local`. Reload the portal — you're signed in.

### Admin console — http://localhost:4201

Same dev auth flow against the **internal** API:

```
http://localhost:5002/dev/sign-in?role=cce-admin
```

Then open http://localhost:4201 — you'll see all admin features: users, content, community moderation, knowledge maps, etc.

Available dev roles:

| Role | Capability |
|---|---|
| `cce-admin` | Full admin — all permissions |
| `cce-editor` | Author + edit content |
| `cce-reviewer` | Review-only |
| `cce-expert` | Community expert (gold badge on replies) |
| `cce-user` | Regular member |

### Calling APIs directly (curl / Postman)

Skip the cookie dance and pass the role in the `Authorization` header:

```bash
curl -H 'Authorization: Bearer dev:cce-admin' \
     'http://localhost:5002/api/admin/community/posts?status=all'
```

> The dev auth shim is **only registered when `Auth:DevMode=true`** in `appsettings.Development.json`. Production deployments use real Entra ID and never expose this path — see [Sub-11 Entra ID migration](../project-plan/specs/2026-05-04-sub-11-design.md).

---

## Tests

```bash
# Backend (≈30 s)
dotnet test backend/CCE.sln

# Frontend unit tests (≈30 s)
pnpm nx run-many -t test

# Frontend lint (≈10 s)
pnpm nx run-many -t lint

# E2E with Playwright + axe-core (≈3 min — needs the stack running)
pnpm nx run-many -t e2e

# Contract drift check (OpenAPI ↔ generated TS clients)
./scripts/check-contracts-clean.sh
```

---

## Common issues

### Port already in use

```
Error: listen EADDRINUSE: address already in use :::4200
```

Find and kill the process:

```bash
lsof -nP -iTCP:4200 -sTCP:LISTEN
kill -9 <pid>
```

### Stale Vite optimized-deps (web-portal blank, "Failed to fetch dynamically imported module")

Vite's dependency cache went out of sync with the running server. Clear it and restart:

```bash
rm -rf frontend/.angular frontend/node_modules/.vite
# then restart `pnpm nx serve web-portal` and hard-reload your browser (⌘⇧R)
```

### Backend crash: `Infrastructure:SqlConnectionString missing`

You launched `dotnet run` without `ASPNETCORE_ENVIRONMENT=Development`. Either prefix the env var (as shown above) or remove `--no-launch-profile` if you added it.

### SQL connection refused

```bash
docker compose ps sqlserver   # is it healthy?
docker compose logs sqlserver # what's it complaining about?
```

On low-memory machines (< 4 GB free), SQL Server / Azure SQL Edge can fail to start. Bump Docker's memory allocation.

### Migrations failed

Most often `Login failed for user 'sa'` — the SQL password in `.env.local` doesn't match the seeded SA password. Tear down with `docker compose down -v` (destroys data) and start fresh.

---

## Tearing down

```bash
# Stop services, keep data volumes
docker compose down

# Stop services AND destroy all SQL/Redis/Meilisearch data
docker compose down -v
```

---

## Next steps

- **[`project-plan/README.md`](../project-plan/README.md)** — sub-project index (specs, plans, completion reports)
- **[`docs/adr/`](adr/)** — 60+ architecture decisions explaining why things are built this way
- **[`docs/runbooks/`](runbooks/)** — production operational procedures (DR, backup/restore, secret rotation)
- **[`docs/subprojects/`](subprojects/)** — one-page brief per sub-project
- **[`CONTRIBUTING.md`](../CONTRIBUTING.md)** — branch model, commit format, PR checklist

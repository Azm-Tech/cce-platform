# Phase 01 — Docker Compose Stack

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Stand up the local development infrastructure (SQL, Redis, Keycloak, MailDev, ClamAV) as Docker Compose services, independent of the .NET or Angular code that will be added in later phases. Services must start cleanly, pass healthchecks, and be addressable from the host and from future container peers.

**Tasks in this phase:** 8
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 00 complete (`.gitignore`, `.env.example`, Gitleaks hook in place).

---

## Phase 01 preamble — divergences from spec (read before starting)

Three changes from spec §4.1 / §5.3, made during planning for this phase. All three are documented as ADR-0016 (arm64 SQL), ADR-0017 (SIEM stub), and ADR-0018 (arm64 ClamAV) in Phase 18.

### Divergence 1 — Azure SQL Edge instead of SQL Server 2022 for local dev
**Why:** Host is Apple Silicon (arm64) — Microsoft does not publish a native arm64 image for `mcr.microsoft.com/mssql/server`. Running it under Rosetta emulation is slow (2–3× slower startup, intermittent crashes) and blocks CI on arm64 runners. **Azure SQL Edge** publishes native arm64, uses the same T-SQL surface as SQL Server 2022 for everything Foundation needs (DDL, basic triggers, sequences, EF Core migrations). Missing features (SQL Server Agent, full-text search, some FILESTREAM features) aren't used in Foundation.
**Prod unchanged:** production deploys real SQL Server 2022 per HLD §3.3.4. Swap is a one-line image change in a prod compose/helm config.
**Mitigation:** Phase 06 adds a migration-compatibility integration test that runs against both Azure SQL Edge and real SQL Server 2022 (the latter via Testcontainers on an amd64 CI runner).

### Divergence 2 — Drop Papercut, use Serilog file sink as SIEM stub
**Why:** Spec §4.1 labeled `papercut:25` as "SIEM stub" — Papercut SMTP is an SMTP server, not a log/security-event collector. Including it would duplicate MailDev's role. A Serilog file sink writing `logs/siem-events.log` in JSON is a more honest dev stand-in; real SIEM shipping lands in sub-project 8.
**What changes:** no `papercut` service in `docker-compose.yml`. Phase 06/07 wires a Serilog file sink. Real SIEM integration is in the roadmap for sub-project 8.

### Divergence 3 — `clamav/clamav-debian:stable` instead of `clamav/clamav:stable`
**Why:** The official `clamav/clamav:stable` Alpine-based image publishes amd64 only — on arm64 the daemon never gets pulled (`no matching manifest for linux/arm64/v8`). `clamav/clamav-debian:stable` is maintained by the same ClamAV team, ships true multi-arch manifests (amd64 + arm64), and exposes an identical `clamd` daemon on TCP 3310 with the same `PING`/`PONG` protocol, same signature update mechanism (`freshclam`), and same `/var/lib/clamav` data path. Swap is transparent to any downstream .NET client code.
**What changes:** Task 1.6 uses `image: clamav/clamav-debian:stable`. Prod can pick either variant depending on host arch; both accept the same config.

---

## Task 1.1: `docker-compose.yml` baseline

**Files:**
- Create: `docker-compose.yml`

**Rationale:** Start with a well-formed skeleton (networks, volumes, project name) and no services. Services are added service-by-service in subsequent tasks so each can be verified independently.

- [ ] **Step 1: Write `docker-compose.yml`**

```yaml
# CCE — Local development stack (infrastructure services only)
# Docker Compose v2 syntax. No top-level `version:` key (obsolete in v2).
# Backend/frontend app services are added in later phases.
#
# Usage:
#   docker compose up -d            # start infra services
#   docker compose ps               # list running services + health
#   docker compose logs <service>   # tail logs
#   docker compose down             # stop services (keeps volumes)
#   docker compose down -v          # stop + remove volumes (DESTROYS data)

name: cce

networks:
  # Single internal network all services share. Services resolve each other by service name.
  cce-net:
    driver: bridge

volumes:
  # Named volumes for persistent state. Survive `docker compose down` but not `down -v`.
  sqlserver-data:
  redis-data:
  keycloak-data:
  clamav-data:

services: {}
```

- [ ] **Step 2: Validate the compose file syntactically**

Run:
```bash
docker compose config --quiet && echo "OK"
```
Expected: prints `OK`. Any parse error aborts here — fix YAML before continuing.

- [ ] **Step 3: Commit**

```bash
git add docker-compose.yml
git -c commit.gpgsign=false commit -m "feat(phase-01): add docker-compose.yml skeleton with cce-net network and named volumes"
```

---

## Task 1.2: Add Azure SQL Edge service (SQL Server 2022-compatible for arm64)

**Files:**
- Modify: `docker-compose.yml`
- Create: `.env` (developer copies from `.env.example`; `.env` is the filename Compose auto-reads)

**Rationale:** Azure SQL Edge is Microsoft's arm64-native build with a SQL Server 2022-compatible engine (see Phase 01 preamble). Password must meet SQL Server complexity requirements (≥8 chars, 3 of 4: upper/lower/digit/symbol).

**Note on env files:** Docker Compose auto-loads `.env` (not `.env.local`). Phase 00 created `.env.example` (tracked) and `.env.local.example` (tracked). `.env.local` is reserved for the Angular apps in later phases. For Compose we use a plain `.env` — copied from `.env.example` + the dev secret from `.env.local.example`. Both `.env` and `.env.local` are gitignored.

- [ ] **Step 1: Create `.env` for Compose by merging the two example files**

Run:
```bash
# Start from the shape template
cp .env.example .env
# Append/override the dev secrets from the developer template
grep -E '^(SQL_PASSWORD|REDIS_PASSWORD|KEYCLOAK_CLIENT_SECRET_|SENTRY_DSN)' .env.local.example >> .env
# Sanity-check
grep '^SQL_PASSWORD=' .env
```
Expected: final grep prints `SQL_PASSWORD=Strong!Passw0rd` (the value from `.env.local.example`).

Verify `.env` is gitignored:
```bash
git check-ignore -v .env
```
Expected: prints a line mentioning `.gitignore` and the `.env` rule.

- [ ] **Step 2: Add the `sqlserver` service block to `docker-compose.yml`**

Replace the existing `services: {}` line with the following (keep indentation exactly; YAML is whitespace-sensitive):

```yaml
services:

  sqlserver:
    # Azure SQL Edge — native arm64, SQL Server 2022-compatible for our scope.
    # Prod swaps this for mcr.microsoft.com/mssql/server:2022-latest (amd64).
    image: mcr.microsoft.com/azure-sql-edge:1.0.7
    container_name: cce-sqlserver
    platform: linux/arm64/v8
    environment:
      ACCEPT_EULA: "Y"
      # SA_PASSWORD must be ≥8 chars with 3 of 4: upper/lower/digit/symbol.
      # Read from .env via host env — never baked into the image.
      MSSQL_SA_PASSWORD: "${SQL_PASSWORD:?SQL_PASSWORD not set — run: cp .env.example .env && grep -E '^(SQL_PASSWORD|REDIS_PASSWORD|KEYCLOAK_CLIENT_SECRET_|SENTRY_DSN)' .env.local.example >> .env}"
      MSSQL_PID: "Developer"
      MSSQL_COLLATION: "SQL_Latin1_General_CP1_CI_AS"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    networks:
      - cce-net
    healthcheck:
      # Non-blocking TCP probe. Azure SQL Edge 1.0.7 does NOT bundle sqlcmd, so we can't use a real SQL query.
      # `exec 3<>/dev/tcp/localhost/1433` opens+closes the socket without blocking on EOF (which is what
      # `cat </dev/tcp/...` would do). The port being accepting-connections means the SQL server process is up.
      # Phase 06 adds a migration-level test that actually runs a query.
      test: ["CMD-SHELL", "timeout 3 bash -c 'exec 3<>/dev/tcp/localhost/1433' 2>/dev/null"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s
    restart: unless-stopped
```

- [ ] **Step 3: Validate the compose file**

Run:
```bash
docker compose config --quiet && echo "OK"
```
Expected: `OK`. If YAML errors, fix indentation.

- [ ] **Step 4: Start only the sqlserver service and wait for health**

Run:
```bash
docker compose up -d sqlserver
# Poll for health. Timeout after 90s.
for i in $(seq 1 18); do
  status=$(docker inspect --format '{{.State.Health.Status}}' cce-sqlserver 2>/dev/null || echo "starting")
  echo "attempt $i: $status"
  [ "$status" = "healthy" ] && break
  sleep 5
done
docker compose ps sqlserver
```
Expected: final line shows `cce-sqlserver ... Up ... (healthy)`.

- [ ] **Step 5: Smoke-test TCP reachability from another container on the same network**

Azure SQL Edge 1.0.7 ships no CLI tools, and `mcr.microsoft.com/mssql-tools` has no arm64 image. So we verify via a tiny Alpine sidecar that proves the SQL port is reachable by name on the Compose network. A full SQL query smoke test lands in Phase 06 when EF Core + migrations go in.

Run:
```bash
docker run --rm --network cce_cce-net alpine:3 sh -c "apk add --quiet --no-cache netcat-openbsd >/dev/null 2>&1; nc -z -w 3 sqlserver 1433 && echo 'SQL TCP OK'"
```
Expected: prints `SQL TCP OK`.

- [ ] **Step 6: Commit**

```bash
git add docker-compose.yml
git -c commit.gpgsign=false commit -m "feat(phase-01): add SQL service (Azure SQL Edge, arm64-native, SQL Server 2022-compatible)"
```

---

## Task 1.3: Add Redis service

**Files:**
- Modify: `docker-compose.yml`

- [ ] **Step 1: Append the `redis` service block to `docker-compose.yml`**

Add directly under the `sqlserver:` block (at the same indentation level, inside `services:`):

```yaml

  redis:
    image: redis:7-alpine
    container_name: cce-redis
    ports:
      - "6379:6379"
    command:
      - "redis-server"
      - "--save"
      - "60"
      - "1000"
      - "--loglevel"
      - "notice"
    volumes:
      - redis-data:/data
    networks:
      - cce-net
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5
      start_period: 5s
    restart: unless-stopped
```

- [ ] **Step 2: Validate compose syntax**

Run: `docker compose config --quiet && echo "OK"`
Expected: `OK`.

- [ ] **Step 3: Start redis and wait for healthy**

Run:
```bash
docker compose up -d redis
for i in $(seq 1 10); do
  status=$(docker inspect --format '{{.State.Health.Status}}' cce-redis 2>/dev/null || echo "starting")
  echo "attempt $i: $status"
  [ "$status" = "healthy" ] && break
  sleep 3
done
docker compose ps redis
```
Expected: `cce-redis ... (healthy)`.

- [ ] **Step 4: Smoke-test SET/GET against Redis**

Run:
```bash
docker exec cce-redis redis-cli SET foundation:ping "pong"
docker exec cce-redis redis-cli GET foundation:ping
docker exec cce-redis redis-cli DEL foundation:ping
```
Expected: first prints `OK`, second prints `pong`, third prints `(integer) 1`.

- [ ] **Step 5: Commit**

```bash
git add docker-compose.yml
git -c commit.gpgsign=false commit -m "feat(phase-01): add Redis 7 service with AOF-style persistence and healthcheck"
```

---

## Task 1.4: Add Keycloak service with realm-import volume

**Files:**
- Modify: `docker-compose.yml`
- Create: `keycloak/realm-export.json` (minimal placeholder — Phase 02 writes the full realm config)
- Create: `keycloak/README.md`

**Rationale:** Keycloak service is wired now but realm content is filled in Phase 02. A minimal placeholder realm lets the service start cleanly during Phase 01 verification and Phase 02 adds realms/clients/users/roles.

- [ ] **Step 1: Create the `keycloak/` directory and minimal placeholder realm file**

Run:
```bash
mkdir -p keycloak
```

File `keycloak/realm-export.json`:

```json
{
  "id": "cce-placeholder",
  "realm": "cce-placeholder",
  "enabled": true,
  "displayName": "CCE Placeholder Realm (Phase 01 only — replaced in Phase 02)",
  "sslRequired": "none"
}
```

Note: Keycloak treats this as a valid single-realm import at startup. Phase 02 replaces this file with the real multi-realm export (`cce-internal` + `cce-external`).

- [ ] **Step 2: Write `keycloak/README.md`**

```markdown
# Keycloak realm export

This directory holds the Keycloak realm configuration auto-imported on container startup.

- `realm-export.json` — single-file realm export. Overwritten in Phase 02 of the Foundation plan with the real `cce-internal` + `cce-external` realms, clients, roles, and seeded admin user.

## How import works

The `keycloak` service in `docker-compose.yml` mounts this directory read-only at
`/opt/keycloak/data/import` and runs `start-dev --import-realm`, which imports every
`*.json` file in that directory on startup.

## Do not commit real secrets

Client secrets in `realm-export.json` are **dev-only placeholder values** matched
against the Gitleaks allowlist (`security/gitleaks.toml`). Production clients regenerate
secrets via Keycloak admin API or ADFS federation (handled in sub-project 8).
```

- [ ] **Step 3: Append the `keycloak` service block to `docker-compose.yml`**

```yaml

  keycloak:
    image: quay.io/keycloak/keycloak:25.0.6
    container_name: cce-keycloak
    command: ["start-dev", "--import-realm"]
    environment:
      # Use the legacy KEYCLOAK_ADMIN/_PASSWORD vars — in KC 25.0.6 the newer
      # KC_BOOTSTRAP_ADMIN_* vars don't reliably provision a master-realm admin
      # under `start-dev`. The legacy vars are deprecated but still create a
      # permanent admin on empty DB. Real prod uses federated ADFS — sub-project 8.
      KEYCLOAK_ADMIN: "admin"
      KEYCLOAK_ADMIN_PASSWORD: "admin"
      KC_HTTP_ENABLED: "true"
      KC_HOSTNAME_STRICT: "false"
      KC_HEALTH_ENABLED: "true"
      KC_HTTP_PORT: "8080"
      KC_MANAGEMENT_PORT: "9000"
    ports:
      - "8080:8080"
      - "9000:9000"
    volumes:
      - keycloak-data:/opt/keycloak/data
      - ./keycloak:/opt/keycloak/data/import:ro
    networks:
      - cce-net
    healthcheck:
      test: ["CMD-SHELL", "exec 3<>/dev/tcp/localhost/9000 && echo -e 'GET /health/ready HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3 && cat <&3 | grep -q 'status.*UP'"]
      interval: 10s
      timeout: 5s
      retries: 15
      start_period: 30s
    restart: unless-stopped
```

- [ ] **Step 4: Validate compose syntax**

Run: `docker compose config --quiet && echo "OK"`
Expected: `OK`.

- [ ] **Step 5: Start keycloak and wait for healthy**

Run:
```bash
docker compose up -d keycloak
for i in $(seq 1 24); do
  status=$(docker inspect --format '{{.State.Health.Status}}' cce-keycloak 2>/dev/null || echo "starting")
  echo "attempt $i: $status"
  [ "$status" = "healthy" ] && break
  sleep 5
done
docker compose ps keycloak
```
Expected: `cce-keycloak ... (healthy)`. Keycloak's first start takes ~30–60s while it imports the placeholder realm and initializes the H2 database.

- [ ] **Step 6: Smoke-test admin console reachability**

Run:
```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/
```
Expected: prints `200` or `302` (Keycloak redirects to the admin console landing page).

- [ ] **Step 7: Commit**

```bash
git add docker-compose.yml keycloak/
git -c commit.gpgsign=false commit -m "feat(phase-01): add Keycloak 25 service with placeholder realm import (full realm in Phase 02)"
```

---

## Task 1.5: Add MailDev service (SMTP capture)

**Files:**
- Modify: `docker-compose.yml`

**Rationale:** MailDev captures all outbound email from the .NET APIs during dev so the developer can inspect account-creation / password-reset messages without touching a real SMTP server. Web UI on port 1080, SMTP on port 1025.

- [ ] **Step 1: Append the `maildev` service block to `docker-compose.yml`**

```yaml

  maildev:
    image: maildev/maildev:2.1.0
    container_name: cce-maildev
    environment:
      MAILDEV_INCOMING_USER: ""
      MAILDEV_INCOMING_PASS: ""
      MAILDEV_WEB_PORT: "1080"
      MAILDEV_SMTP_PORT: "1025"
    ports:
      - "1025:1025"   # SMTP
      - "1080:1080"   # Web UI
    networks:
      - cce-net
    healthcheck:
      # Use 127.0.0.1 not `localhost` — in the container `localhost` resolves to IPv6 `::1` first,
      # but MailDev 2.1.0 binds IPv4 only, so wget on `localhost` hits `Connection refused`.
      test: ["CMD-SHELL", "wget -q -O - http://127.0.0.1:1080/healthz >/dev/null 2>&1 || exit 1"]
      interval: 10s
      timeout: 3s
      retries: 5
      start_period: 5s
    restart: unless-stopped
```

- [ ] **Step 2: Validate compose syntax**

Run: `docker compose config --quiet && echo "OK"`
Expected: `OK`.

- [ ] **Step 3: Start maildev and wait for healthy**

Run:
```bash
docker compose up -d maildev
for i in $(seq 1 10); do
  status=$(docker inspect --format '{{.State.Health.Status}}' cce-maildev 2>/dev/null || echo "starting")
  echo "attempt $i: $status"
  [ "$status" = "healthy" ] && break
  sleep 3
done
docker compose ps maildev
```
Expected: `cce-maildev ... (healthy)`.

- [ ] **Step 4: Smoke-test the web UI**

Run:
```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:1080/
```
Expected: prints `200`.

- [ ] **Step 5: Commit**

```bash
git add docker-compose.yml
git -c commit.gpgsign=false commit -m "feat(phase-01): add MailDev service for SMTP capture (UI :1080, SMTP :1025)"
```

---

## Task 1.6: Add ClamAV service (antivirus stub)

**Files:**
- Modify: `docker-compose.yml`

**Rationale:** File-upload endpoints in later sub-projects call ClamAV's daemon over TCP (port 3310) to scan uploads before storage. Foundation wires the service so later sub-projects have a real daemon to call — no business code depends on it yet.

- [ ] **Step 1: Append the `clamav` service block to `docker-compose.yml`**

```yaml

  clamav:
    # Multi-arch (amd64 + arm64) Debian-based variant of the official ClamAV image.
    # See Phase 01 Divergence 3 — `clamav/clamav:stable` is amd64-only.
    # No `environment:` block — the image's entrypoint treats every CLAMD_CONF_* var as
    # a line appended to /etc/clamav/clamd.conf. Setting CLAMD_CONF_FOREGROUND=yes breaks
    # parsing because FOREGROUND is not a valid clamd.conf option. The baked defaults
    # already run clamd in the foreground and bind 0.0.0.0:3310, which is what we need.
    image: clamav/clamav-debian:stable
    container_name: cce-clamav
    ports:
      - "3310:3310"
    volumes:
      - clamav-data:/var/lib/clamav
    networks:
      - cce-net
    healthcheck:
      # PING/PONG clamd protocol over TCP:3310. Use 127.0.0.1 not `localhost` to avoid
      # IPv6-first resolver behavior (ClamAV binds IPv4 only in the default config).
      test: ["CMD-SHELL", "echo 'PING' | nc -w 1 127.0.0.1 3310 | grep -q 'PONG'"]
      interval: 30s
      timeout: 10s
      retries: 10
      start_period: 120s
    restart: unless-stopped
```

- [ ] **Step 2: Validate compose syntax**

Run: `docker compose config --quiet && echo "OK"`
Expected: `OK`.

- [ ] **Step 3: Start clamav and wait (patient: signature update is slow on first run)**

Run:
```bash
docker compose up -d clamav
echo "ClamAV first-start downloads signatures (~150MB). Allow up to 5 minutes."
for i in $(seq 1 60); do
  status=$(docker inspect --format '{{.State.Health.Status}}' cce-clamav 2>/dev/null || echo "starting")
  echo "attempt $i: $status"
  [ "$status" = "healthy" ] && break
  sleep 5
done
docker compose ps clamav
```
Expected: eventually `cce-clamav ... (healthy)`. If still `starting` after 5 minutes, check `docker compose logs clamav` for freshclam errors (common: no internet access — container needs outbound HTTPS for signature download).

- [ ] **Step 4: Smoke-test clamd responds to PING**

Run:
```bash
printf "PING\n" | nc -w 2 localhost 3310
```
Expected: prints `PONG`.

- [ ] **Step 5: Commit**

```bash
git add docker-compose.yml
git -c commit.gpgsign=false commit -m "feat(phase-01): add ClamAV service for virus scanning of file uploads"
```

---

## Task 1.7: Add `docker-compose.override.yml` for local-only overrides

**Files:**
- Create: `docker-compose.override.yml`

**Rationale:** Compose auto-merges `docker-compose.override.yml` on top of `docker-compose.yml` for local dev. This file holds dev-only settings (extra logging, bind mounts for hot reload) that shouldn't ship to CI/prod. Foundation's override is minimal; later phases add bind mounts for API source dirs when those services are added.

- [ ] **Step 1: Write `docker-compose.override.yml`**

```yaml
# CCE — Local dev overrides (merged on top of docker-compose.yml by default)
# Ship to CI/prod? No — rename/remove before building prod images.
# To run WITHOUT this override:
#   docker compose -f docker-compose.yml up -d

services:
  sqlserver:
    # Verbose errors on first-start issues during dev
    environment:
      MSSQL_LCID: "1033"
      MSSQL_MEMORY_LIMIT_MB: "2048"

  redis:
    # Lower memory footprint for a single dev; override to a larger maxmemory in perf testing
    command:
      - "redis-server"
      - "--save"
      - "60"
      - "1000"
      - "--loglevel"
      - "notice"
      - "--maxmemory"
      - "256mb"
      - "--maxmemory-policy"
      - "allkeys-lru"

  keycloak:
    # Faster first-start logs for dev — set DEBUG to debug realm-import problems
    environment:
      KC_LOG_LEVEL: "INFO"
      # Override to DEBUG to troubleshoot realm import: set via `KC_LOG_LEVEL=DEBUG docker compose up`

  maildev:
    # No override needed — left explicit so devs know this file is where to add per-service dev tweaks

  clamav:
    # No dev-time override — the baked /etc/clamav/freshclam.conf defaults are fine.
    # Note: unprefixed env vars are NOT interpreted by this image's entrypoint (only
    # CLAMD_CONF_* and FRESHCLAM_CONF_* are), so setting FRESHCLAM_CHECKS here is a no-op.
    # If you want to tune freshclam in dev, mount a patched freshclam.conf instead.
```

- [ ] **Step 2: Validate merged compose is still well-formed**

Run: `docker compose config --quiet && echo "OK"`
Expected: `OK`.

- [ ] **Step 3: Commit**

```bash
git add docker-compose.override.yml
git -c commit.gpgsign=false commit -m "feat(phase-01): add docker-compose.override.yml for dev-only tweaks (lower redis memory, Keycloak log level)"
```

---

## Task 1.8: Full-stack smoke test + README phase note

**Files:**
- Modify: `README.md`

**Rationale:** Final Phase 01 verification brings the entire stack up from a clean state, validates every healthcheck, and documents the current state in the root README so the next developer (or subagent) knows what's available.

- [ ] **Step 1: Tear down and bring up from clean state**

Run:
```bash
# Bring stack down but keep volumes (so signatures/cached data persist)
docker compose down
docker compose up -d
echo "Waiting 120s for all services to become healthy…"
sleep 120
docker compose ps
```
Expected: all services show `(healthy)` after 120s. ClamAV may still be `starting` on first run — that's OK if it eventually becomes healthy.

- [ ] **Step 2: Verify all host-exposed ports**

Run:
```bash
for port in 1433 6379 8080 1080 1025 3310; do
  nc -z -w 2 localhost $port && echo "localhost:$port OK" || echo "localhost:$port UNREACHABLE"
done
```
Expected: all 6 lines print `OK`. Any `UNREACHABLE` means the service didn't start — check `docker compose logs <service>`.

- [ ] **Step 3: Verify service-to-service networking (SQL seen from Keycloak container)**

Run:
```bash
docker exec cce-keycloak sh -c "echo quit | timeout 2 bash -c 'exec 3<>/dev/tcp/sqlserver/1433 && echo connected' 2>&1 || echo 'NOT REACHABLE via sqlserver:1433'"
```
Expected: prints `connected`. If `NOT REACHABLE`, verify both services are on network `cce-net`: `docker network inspect cce_cce-net | grep -A2 Containers`.

- [ ] **Step 4: Update `README.md` — add a "Dev stack" section**

Edit `README.md`. Replace this line:

```markdown
> Full getting-started is added in Phase 18. Until then, follow the plan phases in order:
> `docs/superpowers/plans/2026-04-24-foundation/phase-XX-*.md`
```

with:

````markdown
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
````

- [ ] **Step 5: Verify README renders well**

Run: `sed -n '1,60p' README.md`
Expected: prints the file through the new "Local dev stack" section.

- [ ] **Step 6: Commit**

```bash
git add README.md
git -c commit.gpgsign=false commit -m "docs(phase-01): document local dev stack in README (ports, bootstrap, arch note)"
```

- [ ] **Step 7: (Optional) Tear down to save resources**

Run: `docker compose down`
Expected: stops all services; named volumes persist for next startup.

---

## Phase 01 — completion checklist

- [ ] `docker-compose.yml` contains services: `sqlserver`, `redis`, `keycloak`, `maildev`, `clamav` — no `papercut` (per preamble divergence 2).
- [ ] `docker-compose.override.yml` present with dev-only tweaks.
- [ ] `keycloak/realm-export.json` is a minimal placeholder (replaced in Phase 02).
- [ ] `keycloak/README.md` explains auto-import behavior.
- [ ] All five services pass healthchecks (`docker compose ps` shows `(healthy)`).
- [ ] All six host-exposed ports are reachable.
- [ ] SQL responds to `SELECT @@VERSION`.
- [ ] Redis responds to `PING` → `PONG`.
- [ ] Keycloak admin page returns HTTP 200/302.
- [ ] MailDev healthz returns 200.
- [ ] ClamAV responds to `PING` → `PONG` on TCP:3310.
- [ ] `README.md` documents the dev stack bootstrap.
- [ ] `git log --oneline | head -9` shows 8 new Phase-01 commits.
- [ ] `git status` shows clean working tree.

**If all boxes ticked, phase 01 is complete. Proceed to phase 02 (Keycloak realm export).**

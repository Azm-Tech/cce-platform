# Phase 01 — Production Docker images (Sub-10a)

> Parent: [`../2026-05-03-sub-10a.md`](../2026-05-03-sub-10a.md) · Spec: [`../../specs/2026-05-03-sub-10a-design.md`](../../specs/2026-05-03-sub-10a-design.md) §5 (components — Production Dockerfiles)

**Phase goal:** Ship production-quality multistage Docker images for the four apps (`Api.External`, `Api.Internal`, `web-portal`, `admin-cms`). Each image must build cleanly and pass a smoke probe (HTTP 200 on `/health` for backend, `/` for frontend). Add a `docker-build` job to the existing CI workflow that builds all four on PR using GHA layer cache. No deployment yet (Sub-10b).

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 00 closed (commit `2dd3828` or later); backend builds + 429 application tests passing; frontend 502 tests passing.

---

## Task 1.1: `Api.External` Dockerfile + `.dockerignore`

**Files:**
- Create: `backend/.dockerignore`.
- Create: `backend/src/CCE.Api.External/Dockerfile`.

**`.dockerignore`:** keeps the build context lean (~10x faster `docker build` first stage):

```
**/bin
**/obj
**/artifacts
**/.vs
**/.idea
**/*.user
**/TestResults
**/coverage
.git
.github
docs
tests
```

**`Dockerfile`** — multistage. First stage restores + publishes; second stage runs the published output on `aspnet:8.0`:

```dockerfile
# syntax=docker/dockerfile:1.7
# Build stage — restore + publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy props + global config first so package restore can cache.
COPY Directory.Packages.props Directory.Build.props NuGet.config* ./

# Copy every csproj before the rest of the source so `dotnet restore`
# layers are cached as long as csproj files don't change.
COPY src/CCE.Api.Common/CCE.Api.Common.csproj           src/CCE.Api.Common/
COPY src/CCE.Api.External/CCE.Api.External.csproj       src/CCE.Api.External/
COPY src/CCE.Application/CCE.Application.csproj         src/CCE.Application/
COPY src/CCE.Domain/CCE.Domain.csproj                   src/CCE.Domain/
COPY src/CCE.Domain.SourceGenerators/CCE.Domain.SourceGenerators.csproj src/CCE.Domain.SourceGenerators/
COPY src/CCE.Infrastructure/CCE.Infrastructure.csproj   src/CCE.Infrastructure/
COPY src/CCE.Integration/CCE.Integration.csproj         src/CCE.Integration/

RUN dotnet restore "src/CCE.Api.External/CCE.Api.External.csproj"

# Now copy the rest of the source and publish.
COPY src/ src/
RUN dotnet publish "src/CCE.Api.External/CCE.Api.External.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user for runtime (aspnet:8.0 ships an `app` user uid 1654).
USER app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CCE.Api.External.dll"]
```

- [ ] **Step 1:** Create `backend/.dockerignore` with the contents above.

- [ ] **Step 2:** Create `backend/src/CCE.Api.External/Dockerfile` with the contents above.

- [ ] **Step 3:** Build the image:
  ```bash
  cd /Users/m/CCE && docker build -t cce-api-external:dev -f backend/src/CCE.Api.External/Dockerfile backend/
  ```
  Expected: success. First build pulls SDK + ASP.NET base images (~700MB total).

- [ ] **Step 4:** Smoke-probe the runtime image. The host validates Keycloak +
  Infrastructure config eagerly at startup, so the probe must pass placeholder
  values for those — `/health` itself doesn't need them to resolve, just the
  host needs to start:
  ```bash
  docker run --rm -d --name cce-api-external-smoke -p 18080:8080 \
    -e Keycloak__Authority=http://localhost:8080/realms/cce \
    -e Keycloak__Audience=cce-api \
    -e Keycloak__RequireHttpsMetadata=false \
    -e Infrastructure__SqlConnectionString="Server=localhost;Database=CCE;Integrated Security=True" \
    -e Infrastructure__RedisConnectionString="localhost:6379" \
    cce-api-external:dev
  for i in $(seq 1 15); do
    sleep 2
    if curl -fsS http://localhost:18080/health > /dev/null 2>&1; then
      echo "PASS"; break
    fi
    [ $i -eq 15 ] && echo "FAIL"
  done
  docker rm -f cce-api-external-smoke
  ```
  Expected: `PASS`. (The placeholder values let the host start; deeper
  requests against SQL / Keycloak would fail, but `/health` is a liveness
  probe — it returns 200 once the host binds.)

- [ ] **Step 5:** Commit:
  ```bash
  git add backend/.dockerignore backend/src/CCE.Api.External/Dockerfile
  git -c commit.gpgsign=false commit -m "feat(api-external): production Dockerfile

  Multistage build (sdk:8.0 → aspnet:8.0). Restores from
  Directory.Packages.props with csproj-first caching. Runs as the
  ASP.NET base image's non-root 'app' user. Exposes 8080. /health
  HEALTHCHECK probe every 30s. .dockerignore excludes bin/obj/
  artifacts/tests/.git/docs to keep the build context small.

  Sub-10a Phase 01 Task 1.1.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 1.2: `Api.Internal` Dockerfile

**Files:**
- Create: `backend/src/CCE.Api.Internal/Dockerfile`.

Same shape as Task 1.1, just with `CCE.Api.Internal` as the entry assembly:

```dockerfile
# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Packages.props Directory.Build.props NuGet.config* ./

COPY src/CCE.Api.Common/CCE.Api.Common.csproj           src/CCE.Api.Common/
COPY src/CCE.Api.Internal/CCE.Api.Internal.csproj       src/CCE.Api.Internal/
COPY src/CCE.Application/CCE.Application.csproj         src/CCE.Application/
COPY src/CCE.Domain/CCE.Domain.csproj                   src/CCE.Domain/
COPY src/CCE.Domain.SourceGenerators/CCE.Domain.SourceGenerators.csproj src/CCE.Domain.SourceGenerators/
COPY src/CCE.Infrastructure/CCE.Infrastructure.csproj   src/CCE.Infrastructure/
COPY src/CCE.Integration/CCE.Integration.csproj         src/CCE.Integration/

RUN dotnet restore "src/CCE.Api.Internal/CCE.Api.Internal.csproj"

COPY src/ src/
RUN dotnet publish "src/CCE.Api.Internal/CCE.Api.Internal.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

USER app

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CCE.Api.Internal.dll"]
```

- [ ] **Step 1:** Create `backend/src/CCE.Api.Internal/Dockerfile` with the contents above.

- [ ] **Step 2:** Build:
  ```bash
  cd /Users/m/CCE && docker build -t cce-api-internal:dev -f backend/src/CCE.Api.Internal/Dockerfile backend/
  ```
  Expected: success. (Build cache from Task 1.1 applies up to the differing entry-assembly publish step.)

- [ ] **Step 3:** Smoke probe:
  ```bash
  docker run --rm -d --name cce-api-internal-smoke -p 18081:8080 cce-api-internal:dev
  for i in 1 2 3 4 5; do
    sleep 2
    if curl -fsS http://localhost:18081/health > /dev/null 2>&1; then
      echo "PASS"; break
    fi
    [ $i -eq 5 ] && echo "FAIL"
  done
  docker rm -f cce-api-internal-smoke
  ```
  Expected: `PASS`.

- [ ] **Step 4:** Commit:
  ```bash
  git add backend/src/CCE.Api.Internal/Dockerfile
  git -c commit.gpgsign=false commit -m "feat(api-internal): production Dockerfile

  Mirror of Api.External's multistage build with CCE.Api.Internal as
  the entry assembly. Same base images, non-root 'app' user, /health
  HEALTHCHECK, port 8080.

  Sub-10a Phase 01 Task 1.2.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 1.3: `web-portal` Dockerfile + nginx.conf

**Files:**
- Create: `frontend/.dockerignore`.
- Create: `frontend/apps/web-portal/Dockerfile`.
- Create: `frontend/apps/web-portal/nginx.conf`.

**`frontend/.dockerignore`:**
```
node_modules
.nx
dist
**/.nx
**/dist
**/coverage
**/test-output
.git
.github
docs
```

**`Dockerfile`** — multistage with pnpm fetch (the existing repo uses `pnpm-lock.yaml`):

```dockerfile
# syntax=docker/dockerfile:1.7
# Build stage — install deps + nx build production
FROM node:22-alpine AS build
WORKDIR /workspace

RUN corepack enable

# Copy lockfile + package.json first so `pnpm install` is cached.
COPY frontend/package.json frontend/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

# Copy the rest of the workspace and build.
COPY frontend/ ./
RUN ./node_modules/.bin/nx build web-portal --configuration=production

# Runtime stage — nginx serving the SPA bundle
FROM nginx:alpine AS runtime
COPY frontend/apps/web-portal/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /workspace/dist/apps/web-portal/browser /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget -q -O /dev/null http://localhost:8080/ || exit 1
```

**`nginx.conf`** — single `server` block, SPA fallback, gzip, cache headers:

```nginx
server {
  listen 8080;
  server_name _;
  root /usr/share/nginx/html;
  index index.html;

  # Compress text assets on the fly.
  gzip on;
  gzip_vary on;
  gzip_min_length 1024;
  gzip_types
    text/plain
    text/css
    text/javascript
    application/javascript
    application/json
    application/xml
    image/svg+xml;

  # SPA fallback — every unknown path serves index.html so client-side
  # routing works on refresh / direct deep-link.
  location / {
    try_files $uri $uri/ /index.html;
  }

  # Hashed-asset bundles get an immutable long-cache header.
  location ~* \.(?:js|css|woff2?|svg|png|jpg|jpeg|webp|ico)$ {
    expires 7d;
    add_header Cache-Control "public, immutable";
    try_files $uri =404;
  }

  # index.html itself is served fresh — never cache it.
  location = /index.html {
    add_header Cache-Control "no-cache";
  }
}
```

- [ ] **Step 1:** Create `frontend/.dockerignore` with the contents above.

- [ ] **Step 2:** Create `frontend/apps/web-portal/nginx.conf` with the contents above.

- [ ] **Step 3:** Create `frontend/apps/web-portal/Dockerfile` with the contents above.

- [ ] **Step 4:** Build (build context is the repo root because the Dockerfile references `frontend/...` paths):
  ```bash
  cd /Users/m/CCE && docker build -t cce-web-portal:dev -f frontend/apps/web-portal/Dockerfile .
  ```
  Expected: success. First build pulls node:22-alpine + nginx:alpine.

- [ ] **Step 5:** Smoke probe:
  ```bash
  docker run --rm -d --name cce-web-portal-smoke -p 18082:8080 cce-web-portal:dev
  for i in 1 2 3 4 5; do
    sleep 1
    if curl -fsS http://localhost:18082/ | grep -q "<html"; then
      echo "PASS"; break
    fi
    [ $i -eq 5 ] && echo "FAIL"
  done
  docker rm -f cce-web-portal-smoke
  ```
  Expected: `PASS`.

- [ ] **Step 6:** Commit:
  ```bash
  git add frontend/.dockerignore frontend/apps/web-portal/Dockerfile frontend/apps/web-portal/nginx.conf
  git -c commit.gpgsign=false commit -m "feat(web-portal): production Dockerfile + nginx config

  Multistage build (node:22-alpine → nginx:alpine). pnpm install with
  the existing pnpm-lock.yaml. Nx production build of web-portal.
  Nginx config serves dist/apps/web-portal/browser on port 8080 with
  SPA fallback (try_files \$uri \$uri/ /index.html), gzip on text
  assets, immutable cache on hashed bundles, no-cache on index.html.

  Sub-10a Phase 01 Task 1.3.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 1.4: `admin-cms` Dockerfile + nginx.conf

**Files:**
- Create: `frontend/apps/admin-cms/Dockerfile`.
- Create: `frontend/apps/admin-cms/nginx.conf`.

Same shape as Task 1.3 — only differences are the dist path (`dist/apps/admin-cms/browser`) and the nx target name (`admin-cms`):

**`Dockerfile`:**
```dockerfile
# syntax=docker/dockerfile:1.7
FROM node:22-alpine AS build
WORKDIR /workspace

RUN corepack enable

COPY frontend/package.json frontend/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY frontend/ ./
RUN ./node_modules/.bin/nx build admin-cms --configuration=production

FROM nginx:alpine AS runtime
COPY frontend/apps/admin-cms/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /workspace/dist/apps/admin-cms/browser /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget -q -O /dev/null http://localhost:8080/ || exit 1
```

**`nginx.conf`** — identical to web-portal's (the configs only differ in copy paths, which are in the Dockerfile, not the nginx.conf):

```nginx
server {
  listen 8080;
  server_name _;
  root /usr/share/nginx/html;
  index index.html;

  gzip on;
  gzip_vary on;
  gzip_min_length 1024;
  gzip_types
    text/plain
    text/css
    text/javascript
    application/javascript
    application/json
    application/xml
    image/svg+xml;

  location / {
    try_files $uri $uri/ /index.html;
  }

  location ~* \.(?:js|css|woff2?|svg|png|jpg|jpeg|webp|ico)$ {
    expires 7d;
    add_header Cache-Control "public, immutable";
    try_files $uri =404;
  }

  location = /index.html {
    add_header Cache-Control "no-cache";
  }
}
```

- [ ] **Step 1:** Create `frontend/apps/admin-cms/nginx.conf`.

- [ ] **Step 2:** Create `frontend/apps/admin-cms/Dockerfile`.

- [ ] **Step 3:** Build:
  ```bash
  cd /Users/m/CCE && docker build -t cce-admin-cms:dev -f frontend/apps/admin-cms/Dockerfile .
  ```
  Expected: success. (Build cache reuses the pnpm install layer from Task 1.3 — only the `nx build` step differs.)

- [ ] **Step 4:** Smoke probe:
  ```bash
  docker run --rm -d --name cce-admin-cms-smoke -p 18083:8080 cce-admin-cms:dev
  for i in 1 2 3 4 5; do
    sleep 1
    if curl -fsS http://localhost:18083/ | grep -q "<html"; then
      echo "PASS"; break
    fi
    [ $i -eq 5 ] && echo "FAIL"
  done
  docker rm -f cce-admin-cms-smoke
  ```
  Expected: `PASS`.

- [ ] **Step 5:** Commit:
  ```bash
  git add frontend/apps/admin-cms/Dockerfile frontend/apps/admin-cms/nginx.conf
  git -c commit.gpgsign=false commit -m "feat(admin-cms): production Dockerfile + nginx config

  Mirror of web-portal's multistage build with admin-cms as the nx
  build target. Same nginx.conf shape (SPA fallback, gzip, immutable
  cache, no-cache on index.html). Sub-10a Phase 01 Task 1.4.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 1.5: `docker-compose.prod.yml` + CI `docker-build` job

**Files:**
- Create: `docker-compose.prod.yml`.
- Modify: `.github/workflows/ci.yml` — append a new `docker-build` job.

**`docker-compose.prod.yml`** — wires the four images for local smoke testing. No SQL/Redis/Keycloak (those come from `docker-compose.yml` for dev or from prod hosts in 10b/10c):

```yaml
# Sub-10a — Production image smoke target.
# Boots all four production-built images locally for end-to-end probes.
# Does NOT include infra services (SQL, Redis, Keycloak) — supply via
# docker-compose.yml (dev) or external hosts (prod, Sub-10b/10c).

services:
  api-external:
    build:
      context: ./backend
      dockerfile: src/CCE.Api.External/Dockerfile
    image: cce-api-external:prod
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASSISTANT_PROVIDER: ${ASSISTANT_PROVIDER:-stub}
      ANTHROPIC_API_KEY: ${ANTHROPIC_API_KEY:-}
      SENTRY_DSN: ${SENTRY_DSN:-}
      LOG_LEVEL: ${LOG_LEVEL:-Information}
      ConnectionStrings__Default: ${CONN_DEFAULT:-Server=host.docker.internal,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=True;}
      ConnectionStrings__Redis: ${CONN_REDIS:-host.docker.internal:6379}
    ports:
      - "5001:8080"

  api-internal:
    build:
      context: ./backend
      dockerfile: src/CCE.Api.Internal/Dockerfile
    image: cce-api-internal:prod
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      SENTRY_DSN: ${SENTRY_DSN:-}
      LOG_LEVEL: ${LOG_LEVEL:-Information}
      ConnectionStrings__Default: ${CONN_DEFAULT:-Server=host.docker.internal,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=True;}
      ConnectionStrings__Redis: ${CONN_REDIS:-host.docker.internal:6379}
    ports:
      - "5002:8080"

  web-portal:
    build:
      context: .
      dockerfile: frontend/apps/web-portal/Dockerfile
    image: cce-web-portal:prod
    ports:
      - "4200:8080"

  admin-cms:
    build:
      context: .
      dockerfile: frontend/apps/admin-cms/Dockerfile
    image: cce-admin-cms:prod
    ports:
      - "4201:8080"
```

**`ci.yml` modification:** append a new job after the existing `backend` and `frontend` jobs (or wherever the file's structure puts them). Find the last `jobs:` entry and add:

```yaml
  docker-build:
    name: Production Docker images
    runs-on: ubuntu-latest
    needs: [backend, frontend]
    steps:
      - uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Build Api.External
        uses: docker/build-push-action@v6
        with:
          context: backend
          file: backend/src/CCE.Api.External/Dockerfile
          push: false
          load: true
          tags: cce-api-external:ci
          cache-from: type=gha,scope=api-external
          cache-to: type=gha,mode=max,scope=api-external

      - name: Build Api.Internal
        uses: docker/build-push-action@v6
        with:
          context: backend
          file: backend/src/CCE.Api.Internal/Dockerfile
          push: false
          load: true
          tags: cce-api-internal:ci
          cache-from: type=gha,scope=api-internal
          cache-to: type=gha,mode=max,scope=api-internal

      - name: Build web-portal
        uses: docker/build-push-action@v6
        with:
          context: .
          file: frontend/apps/web-portal/Dockerfile
          push: false
          load: true
          tags: cce-web-portal:ci
          cache-from: type=gha,scope=web-portal
          cache-to: type=gha,mode=max,scope=web-portal

      - name: Build admin-cms
        uses: docker/build-push-action@v6
        with:
          context: .
          file: frontend/apps/admin-cms/Dockerfile
          push: false
          load: true
          tags: cce-admin-cms:ci
          cache-from: type=gha,scope=admin-cms
          cache-to: type=gha,mode=max,scope=admin-cms

      - name: Smoke-probe all four runtime containers
        run: |
          set -euo pipefail
          probe() {
            local name=$1 image=$2 host_port=$3 url_path=$4
            docker run --rm -d --name "${name}" -p "${host_port}:8080" "${image}"
            for i in 1 2 3 4 5 6 7 8 9 10; do
              sleep 2
              if curl -fsS "http://localhost:${host_port}${url_path}" >/dev/null 2>&1; then
                echo "${name}: PASS"
                docker rm -f "${name}" >/dev/null
                return 0
              fi
            done
            echo "${name}: FAIL"
            docker logs "${name}" || true
            docker rm -f "${name}" >/dev/null
            return 1
          }

          probe ext     cce-api-external:ci   18080 /health
          probe int     cce-api-internal:ci   18081 /health
          probe portal  cce-web-portal:ci     18082 /
          probe cms     cce-admin-cms:ci      18083 /
```

- [ ] **Step 1:** Create `docker-compose.prod.yml`.

- [ ] **Step 2:** Verify the compose file parses:
  ```bash
  cd /Users/m/CCE && docker compose -f docker-compose.prod.yml config > /dev/null && echo "OK"
  ```
  Expected: `OK`.

- [ ] **Step 3:** Optional local smoke (skip if SQL/Redis aren't running locally — backend will fail to talk to its data plane but the containers themselves should start):
  ```bash
  docker compose -f docker-compose.prod.yml build
  docker compose -f docker-compose.prod.yml up -d web-portal admin-cms
  sleep 3
  curl -fsS http://localhost:4200/ | head -1
  curl -fsS http://localhost:4201/ | head -1
  docker compose -f docker-compose.prod.yml down
  ```

- [ ] **Step 4:** Modify `.github/workflows/ci.yml` to append the `docker-build` job.

- [ ] **Step 5:** Commit:
  ```bash
  git add docker-compose.prod.yml .github/workflows/ci.yml
  git -c commit.gpgsign=false commit -m "ci(docker): add production image build + smoke probe job

  docker-compose.prod.yml wires all four production images for local
  smoke testing. New CI job (docker-build) builds each image with GHA
  layer cache, runs runtime smoke probes (/health for backends, / for
  frontends), fails on any 5xx or container-not-ready timeout.

  No deployment yet — Sub-10b targets a real environment. Sub-10a
  Phase 01 Task 1.5.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Phase 01 close-out

- [ ] All four images built locally without errors.
- [ ] All four runtime containers respond to smoke probes.
- [ ] `docker-compose.prod.yml` parses cleanly.
- [ ] CI `docker-build` job is wired into `ci.yml`.
- [ ] 5 commits on `main`, each green.

**Phase 01 done when:**
- `docker build` succeeds for `Api.External`, `Api.Internal`, `web-portal`, `admin-cms`.
- Runtime smoke probes pass.
- Backend tests still green (`dotnet test tests/CCE.Application.Tests/` → 429).
- Frontend tests still green (502).
- Phase 02 plan to be written next: wire `UseCceSerilog` + `UseCcePrometheus` into both APIs and implement the `AnthropicSmartAssistantClient` + `CitationSearch`.

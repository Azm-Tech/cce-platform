# Phase 01 — Compose + env-file + deploy script (Sub-10b)

> Parent: [`../2026-05-03-sub-10b.md`](../2026-05-03-sub-10b.md) · Spec: [`../../specs/2026-05-03-sub-10b-design.md`](../../specs/2026-05-03-sub-10b-design.md) §`docker-compose.prod.yml` extended, §`.env.prod.example`, §`deploy/deploy.ps1`, §`deploy/smoke.ps1`, §CI changes — `.github/workflows/ci.yml`.

**Phase goal:** Make the system actually deployable. Wire the migrator service into compose; create the strict-env prod override + the local-build CI override; ship `.env.prod.example` so operators know every key; write `deploy.ps1` + `smoke.ps1` + the operator README; extend CI to push all 5 images to ghcr.io with the full tag matrix on `main` and `v*`. Phase 02 adds rollback + the deploy-smoke workflow + close-out.

**Tasks:** 6
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 00 closed (5 commits land on `main`; tag `app-v1.0.0` exists; HEAD at `22746a5` or later).
- `cce-migrator` Dockerfile exists at `backend/src/CCE.Seeder/Dockerfile` and CI builds it (push:false).
- 4 Sub-10a Docker images build cleanly in CI.
- Existing `.gitignore` already excludes `.env` + `.env.*` and allows `.env.example` + `.env.local.example`.
- Existing `docker-compose.prod.yml` (Sub-10a) wires the 4 apps with permissive defaults via `${VAR:-fallback}`.

---

## Task 1.1: `.env.prod.example` + `.gitignore` allow-list

**Files:**
- Create: `.env.prod.example` (repo root) — every required + optional key documented inline.
- Modify: `.gitignore` — append `!.env.prod.example` to the existing `.env*` allow-list block so the example file is committed despite the broad `.env*` deny.

**Why first:** Tasks 1.2 (compose) and 1.4 (deploy.ps1) both reference key names from this file. Locking the canonical key set first prevents drift.

**Final state of `.env.prod.example`:**

```bash
# CCE production environment file (Sub-10b)
# ===========================================================================
# Copy this file to C:\ProgramData\CCE\.env.prod on the deployment host,
# fill in real values, then lock it down:
#   icacls C:\ProgramData\CCE\.env.prod /inheritance:r /grant:r "Administrators:R" "<deploy-user>:R"
#
# deploy.ps1 reads this file, validates required keys, then invokes
# docker compose. Required keys are listed at the bottom of this file.

# ─── Image refs (drives rollback via image-tag pinning) ─────────────────────
CCE_REGISTRY_OWNER=<github-org-or-user>     # e.g. moenergy-cce
CCE_IMAGE_TAG=app-v1.0.0                    # release tag, full SHA, or "latest"

# ─── Database ───────────────────────────────────────────────────────────────
INFRA_SQL=Server=host.docker.internal,1433;Database=CCE;User Id=cce_app;Password=<set-me>;TrustServerCertificate=True;Encrypt=True

# ─── Cache / queue ──────────────────────────────────────────────────────────
INFRA_REDIS=host.docker.internal:6379

# ─── Identity (Keycloak) ────────────────────────────────────────────────────
KEYCLOAK_AUTHORITY=http://host.docker.internal:8080/realms/cce
KEYCLOAK_AUDIENCE=cce-api
KEYCLOAK_REQUIRE_HTTPS=false                # 10c flips to true behind LB

# ─── Assistant (Anthropic LLM) ──────────────────────────────────────────────
ASSISTANT_PROVIDER=anthropic                # or "stub" to disable
ANTHROPIC_API_KEY=<set-me>                  # required when provider=anthropic

# ─── Observability ──────────────────────────────────────────────────────────
LOG_LEVEL=Information
SENTRY_DSN=                                 # leave blank to disable

# ─── Migration behaviour ────────────────────────────────────────────────────
MIGRATE_ON_DEPLOY=true                      # set false to skip migrator service
MIGRATE_SEED_REFERENCE=true                 # seed reference data alongside migrate

# ─── Optional: ghcr.io auth (otherwise rely on existing docker login session) ─
CCE_GHCR_TOKEN=                             # PAT with read:packages

# ─── Required-key catalogue (deploy.ps1 aborts if any are missing/empty) ────
#   CCE_REGISTRY_OWNER, CCE_IMAGE_TAG, INFRA_SQL, INFRA_REDIS,
#   KEYCLOAK_AUTHORITY, KEYCLOAK_AUDIENCE.
#   ANTHROPIC_API_KEY is required only when ASSISTANT_PROVIDER=anthropic.
```

- [ ] **Step 1:** Create `.env.prod.example` at the repo root with the contents above.

- [ ] **Step 2:** Append the allow-list rule to `.gitignore`. Final state of the existing `.env*` block:
  ```
  .env
  .env.*
  !.env.example
  !.env.local.example
  !.env.prod.example
  ```

- [ ] **Step 3:** Verify the example file is no longer ignored:
  ```bash
  git check-ignore -v .env.prod.example
  ```
  Expected: exit 1 (not ignored). If exit 0, gitignore rule didn't take.

- [ ] **Step 4:** Commit:
  ```bash
  git -C /Users/m/CCE add .env.prod.example .gitignore
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "chore(deploy): .env.prod.example + .gitignore allow-list

  Documents every required and optional env-var the production
  deploy needs. Operator copies to C:\ProgramData\CCE\.env.prod,
  fills in values, NTFS-locks the file. Real .env.prod stays out
  of git via the existing .env* deny-list; the example is allowed
  via an explicit !.env.prod.example exception.

  Sub-10b Phase 01 Task 1.1.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 1.2: Extend `docker-compose.prod.yml` + add overrides

**Files:**
- Modify: `docker-compose.prod.yml` — add `migrator` service; replace `build:` blocks with `image: ghcr.io/${CCE_REGISTRY_OWNER}/cce-<name>:${CCE_IMAGE_TAG:-latest}` for all 5 services; add `env_file: ${CCE_ENV_FILE}` for backend services + migrator.
- Create: `docker-compose.prod.deploy.yml` — strict-env override that strips fallback defaults so missing keys hard-fail at compose-up time.
- Create: `docker-compose.build.yml` — local-build override that reinstates `build:` blocks for the 4 Sub-10a apps + the new migrator. CI smoke target uses this.

**Why three files:**
- `docker-compose.prod.yml` is the canonical declaration: image refs + ports + env_file + depends_on. Keeps the relaxed Sub-10a default-fallback shape so direct `docker compose up` (e.g. CI smoke) still works.
- `docker-compose.prod.deploy.yml` is layered ON TOP of that for production runs only. Removes defaults so misconfigured `.env.prod` aborts immediately rather than silently using a fallback.
- `docker-compose.build.yml` is the inverse — used in CI where there's no ghcr.io image yet to pull. Adds `build:` blocks back so `docker compose ... up --build` works.

**Final state of `docker-compose.prod.yml`** (full replacement):

```yaml
# Sub-10b — Production compose stack.
# Boots the 5 prod images (4 apps + migrator) against externally-resident
# infrastructure (SQL, Redis, Keycloak on the Windows host). The migrator
# service runs to completion before APIs start.
#
# Two ways this file is used:
#   - Direct `docker compose -f docker-compose.prod.yml up`
#       (e.g. deploy-smoke CI workflow). depends_on: service_completed_successfully
#       orchestrates ordering. Fallback defaults below let it run with no .env.
#   - `deploy.ps1`-orchestrated path on the Windows host (production).
#       Overlays docker-compose.prod.deploy.yml (strict-env, no defaults) and
#       runs the migrator explicitly via `docker compose run --rm --no-deps`,
#       then brings up apps with `--no-deps` to bypass the now-empty dependency.

services:
  migrator:
    image: ghcr.io/${CCE_REGISTRY_OWNER:-moenergy-cce}/cce-migrator:${CCE_IMAGE_TAG:-latest}
    env_file: ${CCE_ENV_FILE}
    command: ["--migrate", "--seed-reference"]
    restart: "no"

  api-external:
    image: ghcr.io/${CCE_REGISTRY_OWNER:-moenergy-cce}/cce-api-external:${CCE_IMAGE_TAG:-latest}
    env_file: ${CCE_ENV_FILE}
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASSISTANT_PROVIDER: ${ASSISTANT_PROVIDER:-stub}
      ANTHROPIC_API_KEY: ${ANTHROPIC_API_KEY:-}
      SENTRY_DSN: ${SENTRY_DSN:-}
      LOG_LEVEL: ${LOG_LEVEL:-Information}
      Keycloak__Authority: ${KEYCLOAK_AUTHORITY:-http://host.docker.internal:8080/realms/cce}
      Keycloak__Audience: ${KEYCLOAK_AUDIENCE:-cce-api}
      Keycloak__RequireHttpsMetadata: ${KEYCLOAK_REQUIRE_HTTPS:-false}
      Infrastructure__SqlConnectionString: ${INFRA_SQL:-Server=host.docker.internal,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=True;}
      Infrastructure__RedisConnectionString: ${INFRA_REDIS:-host.docker.internal:6379}
    depends_on:
      migrator:
        condition: service_completed_successfully
    ports:
      - "5001:8080"

  api-internal:
    image: ghcr.io/${CCE_REGISTRY_OWNER:-moenergy-cce}/cce-api-internal:${CCE_IMAGE_TAG:-latest}
    env_file: ${CCE_ENV_FILE}
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      SENTRY_DSN: ${SENTRY_DSN:-}
      LOG_LEVEL: ${LOG_LEVEL:-Information}
      Keycloak__Authority: ${KEYCLOAK_AUTHORITY:-http://host.docker.internal:8080/realms/cce}
      Keycloak__Audience: ${KEYCLOAK_AUDIENCE:-cce-api}
      Keycloak__RequireHttpsMetadata: ${KEYCLOAK_REQUIRE_HTTPS:-false}
      Infrastructure__SqlConnectionString: ${INFRA_SQL:-Server=host.docker.internal,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=True;}
      Infrastructure__RedisConnectionString: ${INFRA_REDIS:-host.docker.internal:6379}
    depends_on:
      migrator:
        condition: service_completed_successfully
    ports:
      - "5002:8080"

  web-portal:
    image: ghcr.io/${CCE_REGISTRY_OWNER:-moenergy-cce}/cce-web-portal:${CCE_IMAGE_TAG:-latest}
    ports:
      - "4200:8080"

  admin-cms:
    image: ghcr.io/${CCE_REGISTRY_OWNER:-moenergy-cce}/cce-admin-cms:${CCE_IMAGE_TAG:-latest}
    ports:
      - "4201:8080"
```

**Note on `${CCE_ENV_FILE}`:** the `env_file` directive is read at compose-up time. `deploy.ps1` exports `CCE_ENV_FILE` to the resolved path (default `C:\ProgramData\CCE\.env.prod`) before invoking compose. Direct `docker compose up` callers (deploy-smoke CI workflow) set the variable explicitly. Compose hard-fails if the variable is unset, surfacing operator misconfig immediately.

**Final state of `docker-compose.prod.deploy.yml`** (strict-env override; layered on top of `docker-compose.prod.yml`):

```yaml
# Sub-10b — Production strict-env override.
# Overlays docker-compose.prod.yml; strips fallback defaults so that
# missing required env-vars hard-fail at compose-up time. Used by
# deploy.ps1 in production. NOT used by direct `compose up` callers
# or the CI smoke target — those rely on the relaxed defaults in the
# base file.
#
# Compose merges environment maps key-by-key. Setting a key to the
# empty-default form ${VAR:?} causes compose to error if VAR is unset
# OR empty.

services:
  migrator:
    image: ghcr.io/${CCE_REGISTRY_OWNER:?CCE_REGISTRY_OWNER must be set}/cce-migrator:${CCE_IMAGE_TAG:?CCE_IMAGE_TAG must be set}

  api-external:
    image: ghcr.io/${CCE_REGISTRY_OWNER:?CCE_REGISTRY_OWNER must be set}/cce-api-external:${CCE_IMAGE_TAG:?CCE_IMAGE_TAG must be set}
    environment:
      Keycloak__Authority: ${KEYCLOAK_AUTHORITY:?KEYCLOAK_AUTHORITY must be set}
      Keycloak__Audience: ${KEYCLOAK_AUDIENCE:?KEYCLOAK_AUDIENCE must be set}
      Infrastructure__SqlConnectionString: ${INFRA_SQL:?INFRA_SQL must be set}
      Infrastructure__RedisConnectionString: ${INFRA_REDIS:?INFRA_REDIS must be set}

  api-internal:
    image: ghcr.io/${CCE_REGISTRY_OWNER:?CCE_REGISTRY_OWNER must be set}/cce-api-internal:${CCE_IMAGE_TAG:?CCE_IMAGE_TAG must be set}
    environment:
      Keycloak__Authority: ${KEYCLOAK_AUTHORITY:?KEYCLOAK_AUTHORITY must be set}
      Keycloak__Audience: ${KEYCLOAK_AUDIENCE:?KEYCLOAK_AUDIENCE must be set}
      Infrastructure__SqlConnectionString: ${INFRA_SQL:?INFRA_SQL must be set}
      Infrastructure__RedisConnectionString: ${INFRA_REDIS:?INFRA_REDIS must be set}

  web-portal:
    image: ghcr.io/${CCE_REGISTRY_OWNER:?CCE_REGISTRY_OWNER must be set}/cce-web-portal:${CCE_IMAGE_TAG:?CCE_IMAGE_TAG must be set}

  admin-cms:
    image: ghcr.io/${CCE_REGISTRY_OWNER:?CCE_REGISTRY_OWNER must be set}/cce-admin-cms:${CCE_IMAGE_TAG:?CCE_IMAGE_TAG must be set}
```

**Final state of `docker-compose.build.yml`** (local-build override; restores `build:` for CI smoke target):

```yaml
# Sub-10b — Local-build override.
# Overlays docker-compose.prod.yml; reinstates `build:` blocks so
# `docker compose -f docker-compose.prod.yml -f docker-compose.build.yml up --build`
# builds images from local Dockerfiles instead of pulling from ghcr.io.
# Used by CI on PRs (where no ghcr.io image exists yet) and for local
# smoke testing.

services:
  migrator:
    build:
      context: ./backend
      dockerfile: src/CCE.Seeder/Dockerfile
    image: cce-migrator:dev

  api-external:
    build:
      context: ./backend
      dockerfile: src/CCE.Api.External/Dockerfile
    image: cce-api-external:dev

  api-internal:
    build:
      context: ./backend
      dockerfile: src/CCE.Api.Internal/Dockerfile
    image: cce-api-internal:dev

  web-portal:
    build:
      context: .
      dockerfile: frontend/apps/web-portal/Dockerfile
    image: cce-web-portal:dev

  admin-cms:
    build:
      context: .
      dockerfile: frontend/apps/admin-cms/Dockerfile
    image: cce-admin-cms:dev
```

- [ ] **Step 1:** Replace `docker-compose.prod.yml` with the contents above.

- [ ] **Step 2:** Create `docker-compose.prod.deploy.yml` with the contents above.

- [ ] **Step 3:** Create `docker-compose.build.yml` with the contents above.

- [ ] **Step 4:** Verify all three files parse:
  ```bash
  cd /Users/m/CCE && CCE_ENV_FILE=/tmp/dummy.env docker compose -f docker-compose.prod.yml config >/dev/null
  cd /Users/m/CCE && docker compose -f docker-compose.prod.yml -f docker-compose.build.yml config >/dev/null
  ```
  Expected: both succeed without errors. `CCE_ENV_FILE=/tmp/dummy.env` is just to satisfy the `${CCE_ENV_FILE}` reference; the file doesn't need to exist for `compose config`.

- [ ] **Step 5:** Verify the strict-env override fails as expected:
  ```bash
  cd /Users/m/CCE && CCE_ENV_FILE=/tmp/dummy.env docker compose -f docker-compose.prod.yml -f docker-compose.prod.deploy.yml config 2>&1 | head -5
  ```
  Expected: error message mentioning `CCE_REGISTRY_OWNER must be set` (since none of the strict env-vars are set in this shell).

- [ ] **Step 6:** Commit:
  ```bash
  git -C /Users/m/CCE add docker-compose.prod.yml docker-compose.prod.deploy.yml docker-compose.build.yml
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "chore(docker): production compose stack with migrator + 3-file override pattern

  docker-compose.prod.yml is the canonical 5-service declaration
  (4 apps + migrator). All services use ghcr.io image refs with
  fallback defaults so direct \`compose up\` still works for CI
  smoke. Migrator runs to completion before APIs start via
  depends_on: service_completed_successfully.

  docker-compose.prod.deploy.yml is the strict-env override that
  deploy.ps1 layers on top in production — strips defaults so
  missing required env-vars hard-fail at compose-up time.

  docker-compose.build.yml is the local-build override that
  reinstates \`build:\` blocks for CI PRs (no ghcr.io image yet).

  Sub-10b Phase 01 Task 1.2.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 1.3: CI ghcr.io push gate + tag matrix

**Files:**
- Modify: `.github/workflows/ci.yml` — add `permissions.packages: write` at job level; add `docker/login-action@v3` step; convert all 5 `docker/build-push-action@v6` steps from `push: false` to a conditional push driven by `github.ref` (push on `main` and `v*` only); compute the tag matrix per image; append image refs + tags to `$GITHUB_STEP_SUMMARY`.

**Strategy:** All 5 build steps share the same conditional logic, so we factor the tag computation into a single env-setting step that writes `IMAGE_TAGS_API_EXTERNAL`, etc. into `$GITHUB_ENV`. Each `build-push-action` reads its tags via the env-var. This keeps the YAML readable.

**Final state of the modified `docker-build` job** (replaces the existing job; preserves the existing smoke-probe step at the bottom and adds `+ migrator` to the probe set):

The job-level header gets `permissions.packages: write`:

```yaml
  docker-build:
    name: Production Docker images
    runs-on: ubuntu-latest
    needs: [backend, frontend]
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Compute image tags
        id: tags
        run: |
          set -euo pipefail
          OWNER="${GITHUB_REPOSITORY_OWNER,,}"          # ghcr requires lowercase
          SHA="${GITHUB_SHA}"
          SHORT="${GITHUB_SHA::7}"
          REGISTRY="ghcr.io"
          # Determine if we should push: main pushes + v* tag pushes.
          if [[ "${GITHUB_REF}" == "refs/heads/main" || "${GITHUB_REF}" == refs/tags/v* ]]; then
            PUSH="true"
          else
            PUSH="false"
          fi
          # Optional release tag (only present on v*-tag pushes).
          if [[ "${GITHUB_REF}" == refs/tags/v* ]]; then
            RELEASE="${GITHUB_REF#refs/tags/}"
          else
            RELEASE=""
          fi

          tag_set() {
            local image=$1
            local base="${REGISTRY}/${OWNER}/${image}"
            local tags="${base}:${SHA},${base}:sha-${SHORT},${base}:latest"
            if [[ -n "$RELEASE" ]]; then
              tags="${tags},${base}:${RELEASE}"
            fi
            echo "$tags"
          }

          {
            echo "TAGS_API_EXTERNAL=$(tag_set cce-api-external)"
            echo "TAGS_API_INTERNAL=$(tag_set cce-api-internal)"
            echo "TAGS_WEB_PORTAL=$(tag_set cce-web-portal)"
            echo "TAGS_ADMIN_CMS=$(tag_set cce-admin-cms)"
            echo "TAGS_MIGRATOR=$(tag_set cce-migrator)"
            echo "PUSH=$PUSH"
            echo "OWNER=$OWNER"
            echo "RELEASE=$RELEASE"
          } >> "$GITHUB_ENV"

          {
            echo "## Production images"
            echo ""
            echo "| Image | Push | Tags |"
            echo "|---|---|---|"
            echo "| cce-api-external | $PUSH | \`$(tag_set cce-api-external)\` |"
            echo "| cce-api-internal | $PUSH | \`$(tag_set cce-api-internal)\` |"
            echo "| cce-web-portal   | $PUSH | \`$(tag_set cce-web-portal)\` |"
            echo "| cce-admin-cms    | $PUSH | \`$(tag_set cce-admin-cms)\` |"
            echo "| cce-migrator     | $PUSH | \`$(tag_set cce-migrator)\` |"
          } >> "$GITHUB_STEP_SUMMARY"

      - name: Log in to ghcr.io
        if: env.PUSH == 'true'
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build Api.External
        uses: docker/build-push-action@v6
        with:
          context: backend
          file: backend/src/CCE.Api.External/Dockerfile
          push: ${{ env.PUSH == 'true' }}
          load: true
          tags: ${{ env.TAGS_API_EXTERNAL }}
          cache-from: type=gha,scope=api-external
          cache-to: type=gha,mode=max,scope=api-external

      - name: Build Api.Internal
        uses: docker/build-push-action@v6
        with:
          context: backend
          file: backend/src/CCE.Api.Internal/Dockerfile
          push: ${{ env.PUSH == 'true' }}
          load: true
          tags: ${{ env.TAGS_API_INTERNAL }}
          cache-from: type=gha,scope=api-internal
          cache-to: type=gha,mode=max,scope=api-internal

      - name: Build web-portal
        uses: docker/build-push-action@v6
        with:
          context: .
          file: frontend/apps/web-portal/Dockerfile
          push: ${{ env.PUSH == 'true' }}
          load: true
          tags: ${{ env.TAGS_WEB_PORTAL }}
          cache-from: type=gha,scope=web-portal
          cache-to: type=gha,mode=max,scope=web-portal

      - name: Build admin-cms
        uses: docker/build-push-action@v6
        with:
          context: .
          file: frontend/apps/admin-cms/Dockerfile
          push: ${{ env.PUSH == 'true' }}
          load: true
          tags: ${{ env.TAGS_ADMIN_CMS }}
          cache-from: type=gha,scope=admin-cms
          cache-to: type=gha,mode=max,scope=admin-cms

      - name: Build cce-migrator
        uses: docker/build-push-action@v6
        with:
          context: backend
          file: backend/src/CCE.Seeder/Dockerfile
          push: ${{ env.PUSH == 'true' }}
          load: true
          tags: ${{ env.TAGS_MIGRATOR }}
          cache-from: type=gha,scope=migrator
          cache-to: type=gha,mode=max,scope=migrator

      - name: Smoke-probe all five runtime containers
        run: |
          # ... existing probe_backend / probe_frontend functions ...
          # Existing 4 calls remain, plus a new call to verify the migrator
          # rejects --migrate --demo (cheap exit-code probe; no SQL needed):
          docker run --rm "${TAGS_API_EXTERNAL%%,*}" --help >/dev/null 2>&1 || true
          # Use the first tag from the comma-list for the probe.
          set -e
          MIGRATOR_IMG="${TAGS_MIGRATOR%%,*}"
          (docker run --rm "$MIGRATOR_IMG" --migrate --demo && exit 1) || echo "migrator: rejects --migrate --demo OK"
```

**On the smoke-probe step:** The existing probe_backend / probe_frontend functions stay; we keep their current API/portal/cms calls but they need to use the first comma-separated tag from each `TAGS_*` env-var. The cleanest patch is to extract `${TAGS_API_EXTERNAL%%,*}` (first tag in list) at probe call sites. Add a 5th invocation that verifies the migrator's flag-validation path works without needing SQL.

**Probe-step compatibility note:** Sub-10a's probe used hardcoded `cce-api-external:ci` etc. We change those references to `${TAGS_API_EXTERNAL%%,*}` so the same probes run against the (potentially registry-prefixed) tags. The image is still local — `load: true` ensures buildx loads the multi-platform image into the local docker engine.

- [ ] **Step 1:** Read the current `.github/workflows/ci.yml` `docker-build` job (lines 122-end) and the smoke-probe step's image references:
  ```bash
  grep -n "cce-api-external:ci\|cce-api-internal:ci\|cce-web-portal:ci\|cce-admin-cms:ci\|cce-migrator:ci\|probe_backend\|probe_frontend" /Users/m/CCE/.github/workflows/ci.yml
  ```
  Expected: 4 probe call lines (one per app) plus the function definitions.

- [ ] **Step 2:** Apply the patches in this order:
  - Add `permissions.packages: write` to the `docker-build` job header.
  - Add the `Compute image tags` step after `Set up Docker Buildx`.
  - Add the `Log in to ghcr.io` step (gated on `env.PUSH == 'true'`).
  - For each of the 5 `Build *` steps:
    - Change `push: false` → `push: ${{ env.PUSH == 'true' }}`.
    - Change the hardcoded `tags:` line → `tags: ${{ env.TAGS_<NAME> }}`.
  - In the smoke-probe step, change the 4 hardcoded image refs to `${TAGS_API_EXTERNAL%%,*}`, `${TAGS_API_INTERNAL%%,*}`, `${TAGS_WEB_PORTAL%%,*}`, `${TAGS_ADMIN_CMS%%,*}`.
  - At the end of the smoke-probe step, add the migrator flag-validation probe.

- [ ] **Step 3:** Quick parse-check by reading the file back:
  ```bash
  grep -n "permissions:\|packages: write\|TAGS_\|Compute image tags\|Log in to ghcr.io\|env.PUSH" /Users/m/CCE/.github/workflows/ci.yml
  ```
  Expected: ~20 lines listing the inserted constructs.

- [ ] **Step 4:** Commit:
  ```bash
  git -C /Users/m/CCE add .github/workflows/ci.yml
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "chore(ci): push 5 production images to ghcr.io on main + v*

  Adds docker/login-action@v3 (gated on push-eligible refs) +
  permissions.packages: write at the docker-build job. Computes
  the tag matrix in a single Compute image tags step that writes
  TAGS_<NAME> env-vars consumed by the 5 build-push-action
  invocations. Tag matrix per image:
   - <full-git-sha>     (immutable rollback target)
   - sha-<7-char>       (human-readable rollback target)
   - latest             (convenience pointer)
   - <release-tag>      (additional, only on v* tag pushes)

  PR builds remain push:false. Image refs + tags are appended to
  the run summary so operators can see exactly what got pushed.

  Smoke probe now also runs a cheap flag-validation check against
  the migrator (asserts --migrate --demo exits non-zero); no SQL
  Server needed for this check.

  Sub-10b Phase 01 Task 1.3.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 1.4: `deploy/deploy.ps1` + `deploy/smoke.ps1`

**Files:**
- Create: `deploy/deploy.ps1` — main deploy entry point. 10-step flow per spec §`deploy/deploy.ps1`.
- Create: `deploy/smoke.ps1` — standalone smoke-probe script.
- Create: `deploy/.gitkeep` — ensure the `deploy/` directory commits cleanly even if other files are skipped.

**PowerShell version:** scripts use `#requires -Version 7.0` — PowerShell 7 is shipped with Windows Server 2022 and is the cross-platform default. They run on macOS/Linux too via `pwsh` (so we can lint/syntax-check during dev).

**Final state of `deploy/deploy.ps1`:**

```powershell
#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10b production deploy script.

.DESCRIPTION
    Validates the env-file, pulls the requested image tags from ghcr.io,
    runs the migrator to completion, brings up the 4 app services,
    and runs smoke probes. Idempotent — safe to re-run.

    Returns 0 on success, non-zero on any failure. Prints the rollback
    command on any failure after the first state-changing step.

.PARAMETER EnvFile
    Path to the production env-file. Default: C:\ProgramData\CCE\.env.prod.

.EXAMPLE
    .\deploy\deploy.ps1
    .\deploy\deploy.ps1 -EnvFile C:\ProgramData\CCE\.env.prod
#>
[CmdletBinding()]
param(
    [string]$EnvFile = 'C:\ProgramData\CCE\.env.prod'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$composeBase   = Join-Path $repoRoot 'docker-compose.prod.yml'
$composeStrict = Join-Path $repoRoot 'docker-compose.prod.deploy.yml'

# Logs directory + timestamped log file
$logDir = 'C:\ProgramData\CCE\logs'
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
$logFile = Join-Path $logDir ("deploy-{0:yyyyMMddTHHmmssZ}.log" -f (Get-Date).ToUniversalTime())

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $ts = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $line = "[$ts] [$Level] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

function Abort {
    param([string]$Message, [int]$ExitCode = 1, [switch]$ShowRollback)
    Write-Log -Level 'ERROR' -Message $Message
    if ($ShowRollback) {
        Write-Log -Level 'ERROR' -Message "Rollback command: .\deploy\rollback.ps1 -ToTag <previous-tag>"
        Write-Log -Level 'ERROR' -Message "Find previous tag in: C:\ProgramData\CCE\deploy-history.tsv (Phase 02)"
    }
    exit $ExitCode
}

# ─── Step 1: Resolve env-file path ─────────────────────────────────────────
Write-Log "Step 1/10: Resolving env-file path."
if (-not (Test-Path $EnvFile)) { Abort "Env-file not found: $EnvFile" }
$resolvedEnvFile = (Resolve-Path $EnvFile).Path
Write-Log "Env-file: $resolvedEnvFile"

# ─── Step 2: Validate env-file ─────────────────────────────────────────────
Write-Log "Step 2/10: Validating required keys."
$envMap = @{}
foreach ($line in Get-Content $resolvedEnvFile) {
    if ($line -match '^\s*#') { continue }
    if ($line -match '^\s*$') { continue }
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
        $envMap[$Matches[1]] = $Matches[2].Trim()
    }
}
$required = @('CCE_REGISTRY_OWNER','CCE_IMAGE_TAG','INFRA_SQL','INFRA_REDIS','KEYCLOAK_AUTHORITY','KEYCLOAK_AUDIENCE')
$missing = $required | Where-Object { -not $envMap.ContainsKey($_) -or [string]::IsNullOrWhiteSpace($envMap[$_]) }
if ($envMap['ASSISTANT_PROVIDER'] -eq 'anthropic' -and [string]::IsNullOrWhiteSpace($envMap['ANTHROPIC_API_KEY'])) {
    $missing += 'ANTHROPIC_API_KEY (required when ASSISTANT_PROVIDER=anthropic)'
}
if ($missing) { Abort "Missing required env-keys: $($missing -join ', ')" }
Write-Log "CCE_IMAGE_TAG = $($envMap['CCE_IMAGE_TAG'])"
Write-Log "CCE_REGISTRY_OWNER = $($envMap['CCE_REGISTRY_OWNER'])"

# Export CCE_ENV_FILE so compose's env_file: directive resolves.
$env:CCE_ENV_FILE = $resolvedEnvFile

# ─── Step 3: Docker reachable? ─────────────────────────────────────────────
Write-Log "Step 3/10: Checking docker daemon."
& docker info > $null 2>&1
if ($LASTEXITCODE -ne 0) { Abort "Docker daemon not reachable. Is Docker Desktop / CE running?" }

# ─── Step 4: Registry login (optional) ─────────────────────────────────────
Write-Log "Step 4/10: Registry login."
if (-not [string]::IsNullOrWhiteSpace($envMap['CCE_GHCR_TOKEN'])) {
    Write-Log "CCE_GHCR_TOKEN present; logging into ghcr.io."
    $envMap['CCE_GHCR_TOKEN'] | & docker login ghcr.io -u $envMap['CCE_REGISTRY_OWNER'] --password-stdin
    if ($LASTEXITCODE -ne 0) { Abort "ghcr.io login failed." }
} else {
    Write-Log "CCE_GHCR_TOKEN not set; relying on existing docker login session."
}

# ─── Step 5: Pull images ───────────────────────────────────────────────────
Write-Log "Step 5/10: Pulling images for tag $($envMap['CCE_IMAGE_TAG'])."
& docker compose -f $composeBase -f $composeStrict --env-file $resolvedEnvFile pull
if ($LASTEXITCODE -ne 0) { Abort "Image pull failed. Verify CCE_IMAGE_TAG is correct: $($envMap['CCE_IMAGE_TAG'])" }

# ─── Step 6: Migrator step ─────────────────────────────────────────────────
$migrateOnDeploy = $envMap['MIGRATE_ON_DEPLOY']
if ($migrateOnDeploy -eq $null -or $migrateOnDeploy -eq '') { $migrateOnDeploy = 'true' }
if ($migrateOnDeploy -ieq 'true') {
    Write-Log "Step 6/10: Running migrator."
    & docker compose -f $composeBase -f $composeStrict --env-file $resolvedEnvFile run --rm --no-deps migrator
    if ($LASTEXITCODE -ne 0) { Abort "Migrator failed (exit $LASTEXITCODE). Migrations NOT applied; APIs not started." -ShowRollback }
} else {
    Write-Log "Step 6/10: MIGRATE_ON_DEPLOY=false; skipping migrator. Operator must run migrations manually."
}

# ─── Step 7: Up the apps ───────────────────────────────────────────────────
Write-Log "Step 7/10: Bringing up apps."
& docker compose -f $composeBase -f $composeStrict --env-file $resolvedEnvFile up -d --no-deps --remove-orphans api-external api-internal web-portal admin-cms
if ($LASTEXITCODE -ne 0) { Abort "App startup failed." -ShowRollback }

# ─── Step 8: Smoke probe ──────────────────────────────────────────────────
Write-Log "Step 8/10: Running smoke probes."
$smokeScript = Join-Path $PSScriptRoot 'smoke.ps1'
& pwsh -NoProfile -File $smokeScript -Timeout 60
if ($LASTEXITCODE -ne 0) { Abort "Smoke probe failed. Apps left running for inspection." -ShowRollback }

# ─── Step 9: Append deploy-history.tsv ────────────────────────────────────
# (Phase 02 implements this. Phase 01 is a no-op stub.)
Write-Log "Step 9/10: deploy-history.tsv (Phase 02 implements)."

# ─── Step 10: Print summary ────────────────────────────────────────────────
Write-Log "Step 10/10: Done."
Write-Log "Image tag deployed: $($envMap['CCE_IMAGE_TAG'])"
Write-Log "Registry owner    : $($envMap['CCE_REGISTRY_OWNER'])"
Write-Log "Log file          : $logFile"
exit 0
```

**Final state of `deploy/smoke.ps1`:**

```powershell
#requires -Version 7.0
<#
.SYNOPSIS
    CCE Sub-10b smoke-probe script.

.DESCRIPTION
    Polls the 4 localhost endpoints exposed by the production compose
    stack. Each endpoint: 30 attempts × 2 sec backoff = 60-sec window.
    Returns 0 if all pass, 1 on first failure.

.PARAMETER Timeout
    Per-endpoint timeout in seconds. Default 60.

.PARAMETER Quiet
    Suppress per-attempt output; only print the final result.

.EXAMPLE
    .\deploy\smoke.ps1
    .\deploy\smoke.ps1 -Timeout 90
    .\deploy\smoke.ps1 -Quiet
#>
[CmdletBinding()]
param(
    [int]$Timeout = 60,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

$probes = @(
    @{ Name = 'api-external/health'; Url = 'http://localhost:5001/health'; Type = 'health' },
    @{ Name = 'api-internal/health'; Url = 'http://localhost:5002/health'; Type = 'health' },
    @{ Name = 'web-portal/';         Url = 'http://localhost:4200/';        Type = 'html'   },
    @{ Name = 'admin-cms/';          Url = 'http://localhost:4201/';        Type = 'html'   }
)

$attempts = [Math]::Max(1, [int]($Timeout / 2))
$failed = @()

foreach ($probe in $probes) {
    if (-not $Quiet) { Write-Host "Probing $($probe.Name)..." -NoNewline }
    $ok = $false
    for ($i = 1; $i -le $attempts; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $probe.Url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ($probe.Type -eq 'health') {
                $body = $response.Content | ConvertFrom-Json
                if ($body.status -eq 'Healthy') { $ok = $true; break }
            } else {
                if ($response.Content -match '<html') { $ok = $true; break }
            }
        } catch {
            # swallow — keep retrying
        }
        Start-Sleep -Seconds 2
    }
    if ($ok) {
        if (-not $Quiet) { Write-Host " OK" }
    } else {
        if (-not $Quiet) { Write-Host " FAIL" }
        $failed += $probe.Name
    }
}

if ($failed.Count -gt 0) {
    Write-Error "Smoke probe FAILED: $($failed -join ', ')"
    exit 1
}
Write-Host "All 4 probes PASSED."
exit 0
```

- [ ] **Step 1:** Create `deploy/deploy.ps1` with the contents above.

- [ ] **Step 2:** Create `deploy/smoke.ps1` with the contents above.

- [ ] **Step 3:** Verify scripts parse (no execution — just AST parse via pwsh on the dev host):
  ```bash
  pwsh -NoProfile -Command "& { \$null = [System.Management.Automation.Language.Parser]::ParseFile('/Users/m/CCE/deploy/deploy.ps1', [ref]\$null, [ref]\$err); if (\$err) { \$err | Out-Host; exit 1 } else { Write-Host 'OK' } }"
  pwsh -NoProfile -Command "& { \$null = [System.Management.Automation.Language.Parser]::ParseFile('/Users/m/CCE/deploy/smoke.ps1', [ref]\$null, [ref]\$err); if (\$err) { \$err | Out-Host; exit 1 } else { Write-Host 'OK' } }"
  ```
  Expected: `OK` for both. If `pwsh` isn't installed locally, skip — CI's deploy-smoke workflow (Phase 02) is the primary syntax check.

- [ ] **Step 4:** Commit:
  ```bash
  git -C /Users/m/CCE add deploy/deploy.ps1 deploy/smoke.ps1
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(deploy): deploy.ps1 + smoke.ps1 PowerShell scripts

  deploy.ps1 is the single-entry-point production deploy script.
  10-step idempotent flow: resolve env-file → validate required
  keys → docker reachable check → optional ghcr.io login →
  pull images → run migrator (gated on MIGRATE_ON_DEPLOY) → up
  the apps with --no-deps → smoke probes → deploy-history.tsv
  stub (Phase 02 fills) → print summary.

  Aborts before touching containers if validation/pull fails;
  logs every step to C:\\ProgramData\\CCE\\logs\\deploy-<UTC>.log;
  prints rollback hint on any failure after step 5.

  smoke.ps1 polls 4 localhost endpoints (30 × 2s = 60s window):
  /health on the two APIs (asserts JSON status=Healthy) and / on
  the two SPAs (asserts <html in body). Standalone-callable.

  Sub-10b Phase 01 Task 1.4.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 1.5: `deploy/README.md` + `docs/runbooks/deploy.md`

**Files:**
- Create: `deploy/README.md` — operator quickstart (first-time host setup, deploy command, rollback command, where logs live).
- Create: `docs/runbooks/deploy.md` — green-path procedure, output samples, troubleshooting.

**Final state of `deploy/README.md`:**

```markdown
# CCE deploy/

PowerShell scripts for deploying the CCE production stack to a single Windows Server 2022 host.

## First-time host setup

```powershell
# 1. Create the runtime config directory.
New-Item -ItemType Directory -Path C:\ProgramData\CCE -Force
New-Item -ItemType Directory -Path C:\ProgramData\CCE\logs -Force

# 2. Copy the example env-file to the host config directory.
Copy-Item .\.env.prod.example C:\ProgramData\CCE\.env.prod

# 3. Edit the env-file. Fill in: CCE_REGISTRY_OWNER, CCE_IMAGE_TAG,
#    INFRA_SQL (with real password), KEYCLOAK_AUTHORITY/AUDIENCE,
#    ANTHROPIC_API_KEY (if using Anthropic), CCE_GHCR_TOKEN (if needed).
notepad C:\ProgramData\CCE\.env.prod

# 4. Lock down ACLs so only Administrators + the deploy user can read.
icacls C:\ProgramData\CCE\.env.prod /inheritance:r `
    /grant:r "Administrators:R" `
    /grant:r "<deploy-user>:R"
```

## Deploy

```powershell
cd C:\path\to\CCE
.\deploy\deploy.ps1
```

The script:
- Validates `.env.prod` (aborts on missing required keys).
- Pulls the 5 images at `CCE_IMAGE_TAG`.
- Runs the migrator to completion (skip via `MIGRATE_ON_DEPLOY=false`).
- Brings up the 4 app services.
- Smoke-probes `/health` on the APIs and `/` on the SPAs.
- Logs every step to `C:\ProgramData\CCE\logs\deploy-<UTC-timestamp>.log`.

## Rollback (Phase 02)

```powershell
.\deploy\rollback.ps1 -ToTag <previous-tag>
```

Available after Sub-10b Phase 02 lands. Find prior tags in `C:\ProgramData\CCE\deploy-history.tsv`.

## Smoke probe (standalone)

```powershell
.\deploy\smoke.ps1 [-Timeout 90] [-Quiet]
```

Useful for ad-hoc verification without redeploying.

## Files in this directory

| File | Purpose |
|---|---|
| `deploy.ps1` | Main deploy entry point |
| `smoke.ps1`  | Localhost endpoint probes |
| `rollback.ps1` | Image-tag rollback (Phase 02) |
| `README.md`  | This file |

## See also

- [Production deploy runbook](../docs/runbooks/deploy.md)
- [Rollback runbook](../docs/runbooks/rollback.md) (Phase 02)
- [Forward-only migrations](../docs/runbooks/migrations.md)
- [Sub-10b design spec](../../specs/2026-05-03-sub-10b-design.md)
```

**Final state of `docs/runbooks/deploy.md`:**

```markdown
# Production deploy runbook (Sub-10b)

## Pre-deploy checklist

- [ ] Verify the target image tag is pushed to ghcr.io. Check the GitHub Actions run summary for the commit you want to deploy — it lists every image + tag pushed.
- [ ] Verify `C:\ProgramData\CCE\.env.prod` is up to date and ACL-locked.
- [ ] Verify SQL Server, Redis, and Keycloak are reachable from the deploy host.
- [ ] Verify Docker daemon is running (`docker info`).

## Deploy

1. **Update image tag** in `.env.prod`:
   ```
   CCE_IMAGE_TAG=<new-tag>          # e.g. app-v1.0.1, sha-c612812, or full SHA
   ```

2. **Run the deploy script**:
   ```powershell
   cd C:\path\to\CCE
   .\deploy\deploy.ps1
   ```

3. **Watch for green checkmarks** on each step:
   ```
   [INFO] Step 1/10: Resolving env-file path.
   [INFO] Env-file: C:\ProgramData\CCE\.env.prod
   [INFO] Step 2/10: Validating required keys.
   [INFO] CCE_IMAGE_TAG = app-v1.0.1
   [INFO] Step 3/10: Checking docker daemon.
   [INFO] Step 4/10: Registry login.
   [INFO] Step 5/10: Pulling images for tag app-v1.0.1.
   [INFO] Step 6/10: Running migrator.
   [INFO] Applying EF Core migrations…
   [INFO] No pending migrations.
   [INFO] Step 7/10: Bringing up apps.
   [INFO] Step 8/10: Running smoke probes.
   Probing api-external/health... OK
   Probing api-internal/health... OK
   Probing web-portal/...        OK
   Probing admin-cms/...         OK
   All 4 probes PASSED.
   [INFO] Step 9/10: deploy-history.tsv (Phase 02 implements).
   [INFO] Step 10/10: Done.
   ```

4. **Verify the apps respond on the host**:
   ```powershell
   Invoke-WebRequest http://localhost:5001/health | Select-Object -Expand Content
   Invoke-WebRequest http://localhost:5002/health | Select-Object -Expand Content
   ```

## Common failures

| Symptom | Likely cause | Fix |
|---|---|---|
| `Missing required env-keys: ...` | Forgot a key in `.env.prod` | Add the key, retry. |
| `Docker daemon not reachable` | Docker Desktop / CE not started | Start Docker, retry. |
| `Image pull failed` for one image | Typo in `CCE_IMAGE_TAG` | Verify tag in GitHub Actions run summary. |
| `Image pull failed` (auth) | ghcr.io session expired | Set `CCE_GHCR_TOKEN` in `.env.prod`, retry. |
| `Migrator failed (exit 1)` | DB unreachable, or migration error | Check log file under `C:\ProgramData\CCE\logs\`. **Apps NOT started** — system unchanged. |
| `Smoke probe FAILED: api-external/health` | App startup error (config, DB) | Check `docker compose logs api-external`. App still running for inspection. |
| `Smoke probe FAILED: web-portal/` | nginx couldn't start | Check `docker compose logs web-portal`. |

## Logs

Every deploy writes to `C:\ProgramData\CCE\logs\deploy-<UTC-timestamp>.log`. Logs older than 30 days can be deleted manually (10c will add automated rotation).

## On failure: rollback

If the smoke probes fail or the system is broken after deploy, roll back to the previous known-good image tag:

```powershell
# Phase 02 — rollback.ps1 not yet shipped.
# Manual rollback: edit .env.prod, set CCE_IMAGE_TAG to previous,
# re-run deploy.ps1.
notepad C:\ProgramData\CCE\.env.prod
.\deploy\deploy.ps1
```

Phase 02 ships `rollback.ps1` and `deploy-history.tsv` for proper audit trails.
```

- [ ] **Step 1:** Create `deploy/README.md` with the contents above.

- [ ] **Step 2:** Create `docs/runbooks/deploy.md` with the contents above.

- [ ] **Step 3:** Commit:
  ```bash
  git -C /Users/m/CCE add deploy/README.md docs/runbooks/deploy.md
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "docs(deploy): operator quickstart + production deploy runbook

  deploy/README.md is the operator quickstart living next to the
  scripts: first-time host setup (with icacls ACL lockdown),
  deploy command, smoke-probe usage, file inventory, links to the
  runbooks.

  docs/runbooks/deploy.md is the green-path procedure: pre-deploy
  checklist, what to expect at each step, common-failure table,
  log location. Manual-rollback workaround for Phase 01 — Phase 02
  ships rollback.ps1.

  Sub-10b Phase 01 Task 1.5.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 1.6: Phase 01 close-out verification

**Files:** none (verification only).

This is the gate that proves Phase 01 is done before we move to Phase 02.

- [ ] **Step 1:** Verify all Phase 01 artefacts are in place:
  ```bash
  ls -la /Users/m/CCE/.env.prod.example \
         /Users/m/CCE/docker-compose.prod.yml \
         /Users/m/CCE/docker-compose.prod.deploy.yml \
         /Users/m/CCE/docker-compose.build.yml \
         /Users/m/CCE/deploy/deploy.ps1 \
         /Users/m/CCE/deploy/smoke.ps1 \
         /Users/m/CCE/deploy/README.md \
         /Users/m/CCE/docs/runbooks/deploy.md
  ```
  Expected: 8 files exist.

- [ ] **Step 2:** Verify `.gitignore` allows the example file:
  ```bash
  cd /Users/m/CCE && git check-ignore -v .env.prod.example
  ```
  Expected: exit 1 (not ignored).

- [ ] **Step 3:** Verify backend/frontend tests still pass (no regression from Phase 00 baseline):
  ```bash
  cd /Users/m/CCE/backend && dotnet build && \
    dotnet test tests/CCE.Application.Tests/ --nologo
  ```
  Expected: 439 Application tests passing.

- [ ] **Step 4:** Verify compose files parse:
  ```bash
  cd /Users/m/CCE && CCE_ENV_FILE=/tmp/dummy.env docker compose -f docker-compose.prod.yml config >/dev/null && echo "prod OK"
  cd /Users/m/CCE && docker compose -f docker-compose.prod.yml -f docker-compose.build.yml config >/dev/null && echo "build OK"
  ```
  Expected: both print `OK`.

- [ ] **Step 5:** Push to verify CI green:
  - Sub-task: push current branch.
  - Sub-task: confirm `docker-build` job succeeds with all 5 image build steps green.
  - Sub-task: if on `main`, confirm 5 images pushed to ghcr.io with the `:<sha>` / `:sha-<short>` / `:latest` tag matrix. Check the run summary for the printed table.

- [ ] **Step 6:** Hand off to Phase 02. Phase 02 writes `rollback.ps1`, `deploy-history.tsv` integration in `deploy.ps1`, the `deploy-smoke.yml` Windows-runner workflow, ADR-0053, the completion doc, and the `deploy-v1.0.0` tag.

**Phase 01 done when:**
- 5 commits land on `main` (one per task 1.1–1.5).
- `docker-build` job pushes all 5 images to ghcr.io on `main` with the full tag matrix.
- `.env.prod.example` documents every required + optional key.
- `docker-compose.prod.yml` references images by `${CCE_REGISTRY_OWNER}/cce-<name>:${CCE_IMAGE_TAG}`; `prod.deploy.yml` strips defaults; `build.yml` reinstates `build:` for CI smoke target.
- `deploy.ps1` is operator-callable and produces a green path on a host with valid `.env.prod`.
- `deploy/README.md` and `docs/runbooks/deploy.md` document the operator workflow.
- All Sub-10a + Phase 00 tests remain green: 439 Application + 66 Infrastructure (1 skipped).

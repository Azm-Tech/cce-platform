# Phase 16 — CI Workflows

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Wire GitHub Actions workflows that gate every PR + main push: backend build/test, frontend build/test/lint, contracts drift check, CodeQL static analysis, OWASP ZAP nightly, optional k6 load test, GitHub Dependency Review. Each is a separate file under `.github/workflows/`. Foundation doesn't push to GitHub yet — the workflows are validated locally for YAML correctness; they activate when the repo gains a remote.

**Tasks in this phase:** 7
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 15 complete; all 6 phase-15 commits on `main`.

---

## Pre-execution sanity checks

1. `git status` clean.
2. `test ! -d .github/workflows && echo OK` → `OK` (no existing workflows dir).
3. `which yamllint || brew install yamllint` for local YAML validation.

---

## Task 16.1: Main CI workflow — backend + frontend + contracts

**Files:**

- Create: `.github/workflows/ci.yml`

**Rationale:** Single workflow runs three parallel jobs on every PR + push to main. Each job is fail-fast for its own scope; cross-job dependencies are minimal because the OpenAPI contract bridge is already established.

- [ ] **Step 1: Write `.github/workflows/ci.yml`**

```yaml
name: CI

on:
  pull_request:
    branches: [main]
  push:
    branches: [main]
  workflow_dispatch:

concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read

jobs:
  backend:
    name: Backend (.NET)
    runs-on: ubuntu-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/azure-sql-edge:1.0.7
        env:
          ACCEPT_EULA: "Y"
          MSSQL_SA_PASSWORD: Strong!Passw0rd
          MSSQL_PID: Developer
        ports:
          - 1433:1433
        options: >-
          --health-cmd "timeout 3 bash -c 'exec 3<>/dev/tcp/localhost/1433' 2>/dev/null"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 10
          --health-start-period 30s
      redis:
        image: redis:7-alpine
        ports:
          - 6379:6379
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 3s
          --health-retries 5
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - name: Restore
        run: dotnet restore backend/CCE.sln
      - name: Build
        run: dotnet build backend/CCE.sln --no-restore --nologo -c Debug
      - name: Apply EF migrations
        env:
          CCE_DESIGN_SQL_CONN: "Server=localhost,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=true;"
        run: |
          dotnet tool install --global dotnet-ef --version 8.0.10
          export PATH="$PATH:$HOME/.dotnet/tools"
          dotnet ef database update \
            --project backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj \
            --context CceDbContext \
            --msbuildprojectextensionspath backend/artifacts/obj/CCE.Infrastructure
      - name: Test
        env:
          ASPNETCORE_ENVIRONMENT: Development
          Infrastructure__SqlConnectionString: "Server=localhost,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=true;"
          Infrastructure__RedisConnectionString: "localhost:6379"
        run: dotnet test backend/CCE.sln --no-build --nologo --logger "trx;LogFileName=test-results.trx"
      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: backend-test-results
          path: backend/**/TestResults/*.trx

  frontend:
    name: Frontend (Nx + Angular)
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0 # Nx affected commands need git history
      - uses: actions/setup-node@v4
        with:
          node-version: 20.18.1
      - name: Enable pnpm
        run: corepack enable && corepack prepare pnpm@9.15.4 --activate
      - name: Install
        working-directory: frontend
        run: pnpm install --frozen-lockfile
      - name: Lint
        working-directory: frontend
        run: pnpm nx run-many -t lint
      - name: Test
        working-directory: frontend
        run: pnpm nx run-many -t test --watch=false --passWithNoTests
      - name: Build
        working-directory: frontend
        run: pnpm nx run-many -t build

  contracts:
    name: Contract drift
    runs-on: ubuntu-latest
    needs: [backend, frontend]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - uses: actions/setup-node@v4
        with:
          node-version: 20.18.1
      - name: Enable pnpm
        run: corepack enable && corepack prepare pnpm@9.15.4 --activate
      - name: Install frontend deps
        working-directory: frontend
        run: pnpm install --frozen-lockfile
      - name: Run drift check
        run: ./scripts/check-contracts-clean.sh
```

- [ ] **Step 2: Lint the YAML**

```bash
yamllint -d "{extends: default, rules: {line-length: {max: 200}, document-start: disable, truthy: {check-keys: false}}}" .github/workflows/ci.yml
```

Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/ci.yml
git -c commit.gpgsign=false commit -m "ci(phase-16): main CI workflow (backend + frontend + contract drift) gating PRs to main"
```

---

## Task 16.2: CodeQL static analysis

**Files:**

- Create: `.github/workflows/codeql.yml`

- [ ] **Step 1: Write the workflow**

```yaml
name: CodeQL

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  schedule:
    - cron: "23 4 * * 1" # weekly Monday 04:23 UTC
  workflow_dispatch:

permissions:
  actions: read
  contents: read
  security-events: write

jobs:
  analyze:
    name: Analyze ${{ matrix.language }}
    runs-on: ubuntu-latest
    timeout-minutes: 60
    strategy:
      fail-fast: false
      matrix:
        include:
          - language: csharp
            build-mode: manual
          - language: javascript-typescript
            build-mode: none
    steps:
      - uses: actions/checkout@v4
      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
          languages: ${{ matrix.language }}
          build-mode: ${{ matrix.build-mode }}
          queries: security-and-quality
      - name: Set up .NET
        if: matrix.language == 'csharp'
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - name: Build C#
        if: matrix.language == 'csharp'
        run: dotnet build backend/CCE.sln --nologo -c Release
      - name: Analyze
        uses: github/codeql-action/analyze@v3
        with:
          category: "/language:${{ matrix.language }}"
```

- [ ] **Step 2: Lint + commit**

```bash
yamllint -d "{extends: default, rules: {line-length: {max: 200}, document-start: disable, truthy: {check-keys: false}}}" .github/workflows/codeql.yml
git add .github/workflows/codeql.yml
git -c commit.gpgsign=false commit -m "ci(phase-16): CodeQL static analysis (C# + TypeScript) — PR gate + weekly scan"
```

---

## Task 16.3: OWASP ZAP nightly scan

**Files:**

- Create: `.github/workflows/zap-nightly.yml`

**Rationale:** Brings the docker-compose stack up, runs ZAP baseline scan against the External API + web-portal, fails on high-severity findings.

- [ ] **Step 1: Write the workflow**

```yaml
name: ZAP Nightly

on:
  schedule:
    - cron: "0 2 * * *" # nightly 02:00 UTC
  workflow_dispatch:

permissions:
  contents: read
  issues: write # to file findings as issues (optional)

jobs:
  zap:
    name: ZAP baseline
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - name: Start backend API
        env:
          Infrastructure__SqlConnectionString: "Server=localhost,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=true;"
          Infrastructure__RedisConnectionString: "localhost:6379"
          Keycloak__Authority: "http://localhost:8080/realms/cce-external"
          Keycloak__Audience: "cce-web-portal"
          Keycloak__RequireHttpsMetadata: "false"
        run: |
          dotnet run --project backend/src/CCE.Api.External --urls http://localhost:5001 > /tmp/api.log 2>&1 &
          for i in $(seq 1 30); do
            curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5001/health | grep -q 200 && break
            sleep 2
          done
      - name: ZAP baseline scan
        uses: zaproxy/action-baseline@v0.13.0
        with:
          target: http://localhost:5001
          fail_action: false
          rules_file_name: security/zap-rules.tsv
          allow_issue_writing: false
      - name: Upload ZAP report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: zap-baseline-report
          path: |
            report_html.html
            report_md.md
            report_json.json
```

- [ ] **Step 2: Create the rules file**

```bash
mkdir -p security
```

`security/zap-rules.tsv`:

```
# ZAP baseline scan rule overrides — TSV (rule_id\tlevel\turl_or_*).
# Foundation suppresses well-known infra noise; tighten in sub-project 8.
10049	IGNORE	*	# CSP: Wildcard Directive — already restrictive; ZAP 0day rule too aggressive
```

- [ ] **Step 3: Commit**

```bash
yamllint -d "{extends: default, rules: {line-length: {max: 200}, document-start: disable, truthy: {check-keys: false}}}" .github/workflows/zap-nightly.yml
git add .github/workflows/zap-nightly.yml security/zap-rules.tsv
git -c commit.gpgsign=false commit -m "ci(phase-16): nightly OWASP ZAP baseline scan against External API"
```

---

## Task 16.4: k6 load test workflow (manual dispatch)

**Files:**

- Create: `.github/workflows/loadtest.yml`

- [ ] **Step 1: Write the workflow**

```yaml
name: Load test (k6)

on:
  workflow_dispatch:
    inputs:
      scenario:
        description: "Which scenario to run"
        required: true
        default: "health-anonymous"
        type: choice
        options:
          - health-anonymous
          - health-authenticated

permissions:
  contents: read

jobs:
  loadtest:
    name: ${{ github.event.inputs.scenario }}
    runs-on: ubuntu-latest
    timeout-minutes: 15
    services:
      sqlserver:
        image: mcr.microsoft.com/azure-sql-edge:1.0.7
        env:
          ACCEPT_EULA: "Y"
          MSSQL_SA_PASSWORD: Strong!Passw0rd
        ports: [1433:1433]
      redis:
        image: redis:7-alpine
        ports: [6379:6379]
      keycloak:
        image: quay.io/keycloak/keycloak:25.0.6
        env:
          KEYCLOAK_ADMIN: admin
          KEYCLOAK_ADMIN_PASSWORD: admin
          KC_HTTP_ENABLED: "true"
        ports: [8080:8080]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - name: Start API
        env:
          RateLimiter__PermitLimit: "10000000"
          Infrastructure__SqlConnectionString: "Server=localhost,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=true;"
          Infrastructure__RedisConnectionString: "localhost:6379"
          Keycloak__Authority: "http://localhost:8080/realms/cce-external"
          Keycloak__Audience: "cce-web-portal"
          Keycloak__RequireHttpsMetadata: "false"
        run: |
          dotnet run --project backend/src/CCE.Api.External --urls http://localhost:5001 > /tmp/api.log 2>&1 &
          for i in $(seq 1 30); do
            curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5001/health | grep -q 200 && break
            sleep 2
          done
      - name: Run k6
        uses: grafana/k6-action@v0.3.1
        with:
          filename: loadtest/scenarios/${{ github.event.inputs.scenario }}.js
        env:
          API_BASE_URL: http://localhost:5001
          KEYCLOAK_URL: http://localhost:8080
          OIDC_CLIENT_SECRET: dev-internal-secret-change-me
```

- [ ] **Step 2: Lint + commit**

```bash
yamllint -d "{extends: default, rules: {line-length: {max: 200}, document-start: disable, truthy: {check-keys: false}}}" .github/workflows/loadtest.yml
git add .github/workflows/loadtest.yml
git -c commit.gpgsign=false commit -m "ci(phase-16): k6 load test workflow (manual dispatch with scenario picker)"
```

---

## Task 16.5: Dependency Review on PRs

**Files:**

- Create: `.github/workflows/dep-review.yml`

- [ ] **Step 1: Write the workflow**

```yaml
name: Dependency Review

on:
  pull_request:
    branches: [main]

permissions:
  contents: read
  pull-requests: write

jobs:
  review:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/dependency-review-action@v4
        with:
          fail-on-severity: high
          deny-licenses: GPL-3.0, AGPL-3.0
          comment-summary-in-pr: on-failure
```

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/dep-review.yml
git -c commit.gpgsign=false commit -m "ci(phase-16): GitHub dependency-review-action on PRs (deny GPL/AGPL, fail on high CVEs)"
```

---

## Task 16.6: Dependabot config

**Files:**

- Create: `.github/dependabot.yml`

- [ ] **Step 1: Write `.github/dependabot.yml`**

```yaml
version: 2

updates:
  - package-ecosystem: "nuget"
    directory: "/backend"
    schedule:
      interval: "weekly"
      day: "monday"
    open-pull-requests-limit: 10
    groups:
      microsoft-ext:
        patterns:
          - "Microsoft.Extensions.*"
          - "Microsoft.AspNetCore.*"
      ef-core:
        patterns:
          - "Microsoft.EntityFrameworkCore*"
          - "EFCore.*"
      observability:
        patterns:
          - "Serilog*"
          - "Sentry*"
          - "prometheus-net*"

  - package-ecosystem: "npm"
    directory: "/frontend"
    schedule:
      interval: "weekly"
      day: "monday"
    open-pull-requests-limit: 10
    groups:
      angular:
        patterns:
          - "@angular/*"
          - "@nx/*"
      hey-api:
        patterns:
          - "@hey-api/*"
      ngx-translate:
        patterns:
          - "@ngx-translate/*"
      playwright:
        patterns:
          - "@playwright/*"
          - "playwright"
          - "@axe-core/*"

  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
```

- [ ] **Step 2: Commit**

```bash
git add .github/dependabot.yml
git -c commit.gpgsign=false commit -m "ci(phase-16): Dependabot config (NuGet + npm + Actions, weekly grouped PRs)"
```

---

## Task 16.7: PR template + final lint sweep

**Files:**

- Create: `.github/pull_request_template.md`

- [ ] **Step 1: Write the template**

```markdown
## Summary

<!-- 1–3 bullet points: what changed and why -->

## Test plan

- [ ] `dotnet test backend/CCE.sln` green
- [ ] `pnpm nx run-many -t lint,test` green
- [ ] If API surface changed: `./scripts/check-contracts-clean.sh` green
- [ ] If UI changed: `pnpm nx run-many -t e2e` (web-portal-e2e + admin-cms-e2e) green
- [ ] Manual smoke notes (if any):

## Security checklist

- [ ] No new secrets / credentials in code
- [ ] AuthN / AuthZ impact considered
- [ ] Input validation on new endpoints
- [ ] Audit-log entry for new state-changing operations

## BRD traceability

<!-- List BRD section IDs covered, or "n/a" -->

## Screenshots / output (optional)
```

- [ ] **Step 2: Final lint sweep across every workflow file**

```bash
for f in .github/workflows/*.yml; do
  echo "=== $f"
  yamllint -d "{extends: default, rules: {line-length: {max: 200}, document-start: disable, truthy: {check-keys: false}}}" "$f"
done
yamllint -d "{extends: default, rules: {line-length: {max: 200}, document-start: disable, truthy: {check-keys: false}}}" .github/dependabot.yml
```

Expected: no errors anywhere.

- [ ] **Step 3: Commit**

```bash
git add .github/pull_request_template.md
git -c commit.gpgsign=false commit -m "ci(phase-16): add PR template with test plan + security checklist + BRD traceability"
```

---

## Phase 16 — completion checklist

- [ ] `.github/workflows/ci.yml` — backend + frontend + contracts.
- [ ] `.github/workflows/codeql.yml` — C# + TS scans.
- [ ] `.github/workflows/zap-nightly.yml` — OWASP ZAP baseline.
- [ ] `.github/workflows/loadtest.yml` — k6 manual dispatch.
- [ ] `.github/workflows/dep-review.yml` — PR-time dep review.
- [ ] `.github/dependabot.yml` — weekly grouped updates.
- [ ] `.github/pull_request_template.md` — PR checklist.
- [ ] All YAML files lint clean.
- [ ] `git status` clean.
- [ ] ~7 new commits.

**If all boxes ticked, phase 16 is complete. Proceed to phase 17 (security scanning configs).**

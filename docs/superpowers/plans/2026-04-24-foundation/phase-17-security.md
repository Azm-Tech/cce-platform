# Phase 17 — Security Scanning Configs

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Add the security scanning tools spec §9.6 calls for, beyond Gitleaks (Phase 00) and CodeQL (Phase 16): Semgrep, Trivy, SonarCloud, OWASP Dependency-Check, CycloneDX SBOM, Gitleaks CI workflow (full-history scan), and a security/README documenting the layered defenses.

**Tasks in this phase:** 7
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 16 complete; `.github/workflows/` has 5 workflow files.

---

## Pre-execution sanity checks

1. `git status` clean.
2. `ls .github/workflows/` shows 5 yml files (ci, codeql, zap-nightly, loadtest, dep-review).
3. `which yamllint` available.

---

## Task 17.1: Semgrep workflow + ruleset

**Files:**
- Create: `.github/workflows/semgrep.yml`
- Create: `security/semgrep.yml`

- [ ] **Step 1: Write the workflow**

```yaml
name: Semgrep

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  schedule:
    - cron: "17 5 * * 2"   # weekly Tue 05:17 UTC
  workflow_dispatch:

permissions:
  contents: read
  security-events: write

jobs:
  semgrep:
    name: Semgrep scan
    runs-on: ubuntu-latest
    container:
      image: semgrep/semgrep:1.92.0
    if: (github.actor != 'dependabot[bot]')
    steps:
      - uses: actions/checkout@v4
      - name: Run Semgrep
        run: semgrep ci --config=p/owasp-top-ten --config=p/csharp --config=p/typescript --config=p/security-audit --config=security/semgrep.yml --sarif --output=semgrep-results.sarif
        env:
          SEMGREP_RULES: "p/security-audit"
      - name: Upload SARIF
        if: always()
        uses: github/codeql-action/upload-sarif@v3
        with:
          sarif_file: semgrep-results.sarif
          category: semgrep
```

- [ ] **Step 2: Write `security/semgrep.yml` (project-specific rules)**

```yaml
rules:
  - id: cce-no-direct-datetime
    pattern: DateTimeOffset.UtcNow
    message: |
      Use ISystemClock from the DI container instead of DateTimeOffset.UtcNow directly.
      This makes time-dependent code testable via FakeSystemClock.
    languages: [csharp]
    severity: WARNING
    paths:
      include:
        - "backend/src/CCE.Domain/**"
        - "backend/src/CCE.Application/**"
      exclude:
        - "backend/src/CCE.Infrastructure/SystemClock.cs"

  - id: cce-no-string-secrets
    pattern-either:
      - pattern: var $X = "$SECRET";
      - pattern: const string $X = "$SECRET";
    message: |
      Possible hardcoded secret. Move to .env.local or appsettings.Development.json (gitignored)
      or read from configuration.
    languages: [csharp]
    severity: WARNING
    metavariable-pattern:
      metavariable: $SECRET
      pattern-regex: "^[A-Za-z0-9]{32,}$"

  - id: cce-no-console-log-in-production
    pattern-either:
      - pattern: console.log(...)
      - pattern: console.debug(...)
    message: |
      Production code should use a structured logger or Sentry.captureException.
      console.* output is lost in deployed Angular apps.
    languages: [typescript]
    severity: INFO
    paths:
      include:
        - "frontend/apps/**/src/**"
        - "frontend/libs/**/src/**"
      exclude:
        - "**/*.spec.ts"
        - "**/test/**"
        - "**/*.test.ts"
```

- [ ] **Step 3: Lint + commit**

```bash
yamllint -d "{extends: default, rules: {line-length: {max: 200}, document-start: disable, truthy: {check-keys: false}}}" .github/workflows/semgrep.yml security/semgrep.yml
git add .github/workflows/semgrep.yml security/semgrep.yml
git -c commit.gpgsign=false commit -m "feat(phase-17): Semgrep CI workflow + project-specific rules (DateTime/secrets/console.log)"
```

---

## Task 17.2: Trivy filesystem + container scan

**Files:**
- Create: `.github/workflows/trivy.yml`
- Create: `security/trivyignore` (ignore list)

- [ ] **Step 1: Write the workflow**

```yaml
name: Trivy

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  schedule:
    - cron: "44 6 * * 3"   # weekly Wed 06:44 UTC
  workflow_dispatch:

permissions:
  contents: read
  security-events: write

jobs:
  fs-scan:
    name: Filesystem scan
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Trivy fs scan
        uses: aquasecurity/trivy-action@0.28.0
        with:
          scan-type: fs
          scan-ref: .
          ignore-unfixed: true
          severity: HIGH,CRITICAL
          format: sarif
          output: trivy-fs.sarif
          trivyignores: security/trivyignore
      - name: Upload SARIF
        if: always()
        uses: github/codeql-action/upload-sarif@v3
        with:
          sarif_file: trivy-fs.sarif
          category: trivy-fs

  config-scan:
    name: IaC / Config scan
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Trivy config scan
        uses: aquasecurity/trivy-action@0.28.0
        with:
          scan-type: config
          scan-ref: .
          severity: HIGH,CRITICAL
          format: sarif
          output: trivy-config.sarif
      - name: Upload SARIF
        if: always()
        uses: github/codeql-action/upload-sarif@v3
        with:
          sarif_file: trivy-config.sarif
          category: trivy-config
```

- [ ] **Step 2: Write `security/trivyignore`**

```
# CCE — Trivy ignore list. Each entry: CVE-id with comment + expiry date.
# Empty for Foundation; sub-projects add as real CVEs surface.
```

- [ ] **Step 3: Lint + commit**

```bash
yamllint -d "{extends: default, rules: {line-length: {max: 200}, document-start: disable, truthy: {check-keys: false}}}" .github/workflows/trivy.yml
git add .github/workflows/trivy.yml security/trivyignore
git -c commit.gpgsign=false commit -m "feat(phase-17): Trivy filesystem + IaC config scan workflows (HIGH/CRITICAL)"
```

---

## Task 17.3: SonarCloud config

**Files:**
- Create: `sonar-project.properties`
- Create: `.github/workflows/sonarcloud.yml`

**Rationale:** SonarCloud is hosted SaaS — Foundation ships the config; activation requires a SonarCloud account + `SONAR_TOKEN` repo secret. Workflow guards on the secret being present so it's a no-op when running in a fork without the token.

- [ ] **Step 1: Write `sonar-project.properties`**

```properties
# CCE — SonarCloud configuration
# Activate by creating a SonarCloud project + adding SONAR_TOKEN repo secret.

sonar.projectKey=cce-knowledge-center
sonar.organization=ministry-of-energy
sonar.projectName=CCE Knowledge Center
sonar.projectVersion=0.1.0

# Sources
sonar.sources=backend/src,frontend/apps,frontend/libs
sonar.tests=backend/tests,frontend/apps,frontend/libs
sonar.test.inclusions=**/*Tests*.cs,**/*Spec*.cs,**/*.spec.ts,**/*.test.ts

# Languages
sonar.cs.opencover.reportsPaths=backend/coverage/**/coverage.opencover.xml
sonar.javascript.lcov.reportPaths=frontend/coverage/**/lcov.info

# Exclusions — generated, vendor, scaffolding noise
sonar.exclusions=**/bin/**,**/obj/**,**/dist/**,**/node_modules/**,**/.angular/**,**/.nx/**,**/Migrations/**,**/generated/**,frontend/apps/**/public/**,backend/artifacts/**
sonar.coverage.exclusions=**/Migrations/**,**/Program.cs,**/DependencyInjection.cs,**/*.gen.ts,**/generated/**,**/*.config.ts

# Quality gate — adopt SonarCloud's "Sonar way" by default
sonar.qualitygate.wait=true
```

- [ ] **Step 2: Write `.github/workflows/sonarcloud.yml`**

```yaml
name: SonarCloud

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:

permissions:
  contents: read
  pull-requests: read

jobs:
  sonarcloud:
    name: SonarCloud Scan
    runs-on: ubuntu-latest
    if: ${{ env.SONAR_TOKEN_PRESENT == 'true' || github.event_name == 'workflow_dispatch' }}
    env:
      SONAR_TOKEN_PRESENT: ${{ secrets.SONAR_TOKEN != '' }}
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - uses: actions/setup-java@v4
        with:
          distribution: temurin
          java-version: 17
      - name: Install SonarScanner
        run: dotnet tool install --global dotnet-sonarscanner
      - name: Begin SonarCloud
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        run: |
          export PATH="$PATH:$HOME/.dotnet/tools"
          dotnet sonarscanner begin /k:"cce-knowledge-center" /o:"ministry-of-energy" \
            /d:sonar.token="$SONAR_TOKEN" /d:sonar.host.url="https://sonarcloud.io"
      - name: Build + test
        run: |
          dotnet build backend/CCE.sln --nologo -c Debug
          dotnet test backend/CCE.sln --no-build --nologo --collect:"XPlat Code Coverage"
      - name: End SonarCloud
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        run: |
          export PATH="$PATH:$HOME/.dotnet/tools"
          dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
```

- [ ] **Step 3: Lint + commit**

```bash
yamllint -d "{extends: default, rules: {line-length: {max: 200}, document-start: disable, truthy: {check-keys: false}}}" .github/workflows/sonarcloud.yml
git add sonar-project.properties .github/workflows/sonarcloud.yml
git -c commit.gpgsign=false commit -m "feat(phase-17): SonarCloud config + workflow (gated on SONAR_TOKEN secret)"
```

---

## Task 17.4: OWASP Dependency-Check workflow

**Files:**
- Create: `.github/workflows/dep-check.yml`
- Create: `security/dependency-check-suppression.xml`

- [ ] **Step 1: Write the workflow**

```yaml
name: Dependency-Check

on:
  schedule:
    - cron: "0 7 * * 4"   # weekly Thu 07:00 UTC
  workflow_dispatch:

permissions:
  contents: read
  security-events: write

jobs:
  dep-check:
    name: OWASP Dependency-Check
    runs-on: ubuntu-latest
    timeout-minutes: 45
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - name: Restore .NET
        run: dotnet restore backend/CCE.sln
      - name: Run Dependency-Check
        uses: dependency-check/Dependency-Check_Action@main
        with:
          project: cce-knowledge-center
          path: ./backend
          format: SARIF
          out: ./dep-check-reports
          args: >
            --suppression security/dependency-check-suppression.xml
            --enableRetired
            --failOnCVSS 7
      - name: Upload SARIF
        if: always()
        uses: github/codeql-action/upload-sarif@v3
        with:
          sarif_file: dep-check-reports/dependency-check-report.sarif
          category: owasp-dependency-check
      - name: Upload report artifact
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: dependency-check-report
          path: dep-check-reports/
```

- [ ] **Step 2: Write `security/dependency-check-suppression.xml`**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<suppressions xmlns="https://jeremylong.github.io/DependencyCheck/dependency-suppression.1.3.xsd">
  <!-- Empty for Foundation. Each suppression must include a CVE id, justification,
       and a notBefore date so it expires and forces re-review. -->
</suppressions>
```

- [ ] **Step 3: Lint + commit**

```bash
yamllint -d "{extends: default, rules: {line-length: {max: 200}, document-start: disable, truthy: {check-keys: false}}}" .github/workflows/dep-check.yml
git add .github/workflows/dep-check.yml security/dependency-check-suppression.xml
git -c commit.gpgsign=false commit -m "feat(phase-17): OWASP Dependency-Check weekly workflow with suppression file scaffold"
```

---

## Task 17.5: CycloneDX SBOM generation

**Files:**
- Create: `.github/workflows/sbom.yml`

- [ ] **Step 1: Write the workflow**

```yaml
name: SBOM

on:
  push:
    branches: [main]
    tags: ["v*"]
  workflow_dispatch:

permissions:
  contents: read

jobs:
  backend-sbom:
    name: .NET SBOM
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - name: Install CycloneDX tool
        run: dotnet tool install --global CycloneDX --version 4.4.0
      - name: Generate
        run: |
          export PATH="$PATH:$HOME/.dotnet/tools"
          mkdir -p sbom
          dotnet-CycloneDX backend/CCE.sln -o sbom/ -j -fn cce-backend.cdx.json
      - uses: actions/upload-artifact@v4
        with:
          name: backend-sbom
          path: sbom/cce-backend.cdx.json

  frontend-sbom:
    name: npm SBOM
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 20.18.1
      - name: Enable pnpm
        run: corepack enable && corepack prepare pnpm@9.15.4 --activate
      - name: Install
        working-directory: frontend
        run: pnpm install --frozen-lockfile
      - name: Generate SBOM
        working-directory: frontend
        run: |
          npx --yes @cyclonedx/cyclonedx-npm@2.0.0 --output-file ../sbom/cce-frontend.cdx.json --output-format JSON
      - uses: actions/upload-artifact@v4
        with:
          name: frontend-sbom
          path: sbom/cce-frontend.cdx.json
```

- [ ] **Step 2: Lint + commit**

```bash
yamllint -d "{extends: default, rules: {line-length: {max: 200}, document-start: disable, truthy: {check-keys: false}}}" .github/workflows/sbom.yml
git add .github/workflows/sbom.yml
git -c commit.gpgsign=false commit -m "feat(phase-17): CycloneDX SBOM workflow (.NET + npm) on main + tags"
```

---

## Task 17.6: Gitleaks CI workflow (full-history scan)

**Files:**
- Create: `.github/workflows/gitleaks.yml`

**Rationale:** Phase 00 wired pre-commit Gitleaks; this adds a CI version that scans ALL of git history (catches secrets that may have slipped into older commits). Runs on push to main + weekly.

- [ ] **Step 1: Write the workflow**

```yaml
name: Gitleaks

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  schedule:
    - cron: "11 8 * * 5"   # weekly Fri 08:11 UTC
  workflow_dispatch:

permissions:
  contents: read

jobs:
  gitleaks:
    name: Gitleaks history scan
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0   # full history
      - name: Run Gitleaks
        uses: gitleaks/gitleaks-action@v2
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          GITLEAKS_CONFIG: security/gitleaks.toml
          GITLEAKS_ENABLE_UPLOAD_ARTIFACT: true
          GITLEAKS_ENABLE_SUMMARY: true
```

- [ ] **Step 2: Lint + commit**

```bash
yamllint -d "{extends: default, rules: {line-length: {max: 200}, document-start: disable, truthy: {check-keys: false}}}" .github/workflows/gitleaks.yml
git add .github/workflows/gitleaks.yml
git -c commit.gpgsign=false commit -m "feat(phase-17): Gitleaks CI workflow (full-history scan; complements Phase 00 pre-commit)"
```

---

## Task 17.7: Security overview README

**Files:**
- Create: `security/README.md`

- [ ] **Step 1: Write the README**

```markdown
# CCE — Security Tooling

## Layered defenses

| Layer | Tool | When |
|---|---|---|
| Pre-commit | Gitleaks (Phase 00) | every commit, fast-path |
| PR gate | CI (build/test/lint), CodeQL, Semgrep, Trivy fs+config, Dependency Review, Gitleaks history | every PR |
| Nightly / weekly | OWASP ZAP baseline (nightly), Dependency-Check (weekly), Semgrep / Trivy / Gitleaks (weekly cron) | scheduled |
| Per-release | CycloneDX SBOM | tag push |
| Quality | SonarCloud | every PR (gated on `SONAR_TOKEN` secret) |

## Files in this directory

- `gitleaks.toml` — Gitleaks config + allowlist (Phase 00).
- `semgrep.yml` — project-specific Semgrep rules.
- `trivyignore` — Trivy CVE suppression list.
- `zap-rules.tsv` — ZAP baseline rule overrides.
- `dependency-check-suppression.xml` — OWASP DC suppressions.
- `README.md` — this file.

## How to add a CVE suppression

Each suppression must be **time-bounded** and **explained**.

1. Open the right file (`trivyignore` for Trivy, `dependency-check-suppression.xml` for OWASP DC, etc.).
2. Add the CVE id with a comment containing:
   - CVE id
   - Why suppression is justified (false positive / inapplicable / mitigated elsewhere)
   - Expiry date (max 90 days)
   - Issue link tracking the upstream fix
3. Open a PR — the security reviewer signs off before merge.

## Manual security review

Before merging any PR that touches:
- AuthN/AuthZ code paths
- File upload paths
- External integration code
- Cryptography
- Persistence layer

run `./scripts/check-contracts-clean.sh` AND a manual review against `docs/threat-model.md` (added in Phase 18).
```

- [ ] **Step 2: Commit**

```bash
git add security/README.md
git -c commit.gpgsign=false commit -m "docs(phase-17): security/README.md documenting layered defenses + suppression policy"
```

---

## Phase 17 — completion checklist

- [ ] `.github/workflows/semgrep.yml` + `security/semgrep.yml`.
- [ ] `.github/workflows/trivy.yml` + `security/trivyignore`.
- [ ] `.github/workflows/sonarcloud.yml` + `sonar-project.properties`.
- [ ] `.github/workflows/dep-check.yml` + `security/dependency-check-suppression.xml`.
- [ ] `.github/workflows/sbom.yml`.
- [ ] `.github/workflows/gitleaks.yml`.
- [ ] `security/README.md` documenting layered defenses.
- [ ] All YAML files lint clean.
- [ ] `git status` clean.
- [ ] ~7 new commits.

**If all boxes ticked, phase 17 is complete. Proceed to phase 18 (the big docs phase — 15 ADRs + roadmap + briefs + traceability).**

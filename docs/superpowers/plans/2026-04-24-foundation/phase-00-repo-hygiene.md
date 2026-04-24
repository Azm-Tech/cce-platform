# Phase 00 — Repo Hygiene

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Establish baseline repository hygiene so every subsequent phase commits cleanly: consistent line endings and encoding, a known-good `.gitignore`, a scrubbed `.env.example`, pre-commit gates for secrets and formatting, and a placeholder root `README.md`.

**Tasks in this phase:** 6
**Working directory:** `/Users/m/CCE/` (assumed for all commands).
**Preconditions:** Git repo already initialized, one commit exists (`Add Foundation design spec`), one additional commit from the plan index.

---

## Task 0.1: Add `.editorconfig`

**Files:**
- Create: `.editorconfig`

- [ ] **Step 1: Write `.editorconfig`**

```editorconfig
# CCE — EditorConfig (editorconfig.org)
# Enforced by IDE + .NET SDK + ESLint + Prettier pipelines

root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space
indent_size = 2

# C# / .NET
[*.{cs,csx,vb,vbx}]
indent_size = 4
# Additional C# style rules enforced via .globalconfig per project

# TypeScript / JavaScript / JSON / HTML / SCSS
[*.{ts,tsx,js,jsx,mjs,cjs,json,jsonc,html,scss,css}]
indent_size = 2

# YAML
[*.{yml,yaml}]
indent_size = 2

# Markdown — preserve trailing whitespace (hard line breaks)
[*.md]
trim_trailing_whitespace = false
max_line_length = off

# Makefile, shell — tabs required or idiomatic
[Makefile]
indent_style = tab
[*.sh]
indent_size = 2

# Windows scripts
[*.{bat,cmd}]
end_of_line = crlf
```

- [ ] **Step 2: Verify file exists and is readable**

Run: `cat .editorconfig | head -5`
Expected: prints the first 5 lines starting with `# CCE — EditorConfig`.

- [ ] **Step 3: Commit**

```bash
git add .editorconfig
git -c commit.gpgsign=false commit -m "chore(phase-00): add .editorconfig with LF + UTF-8 defaults"
```

---

## Task 0.2: Add `.gitattributes`

**Files:**
- Create: `.gitattributes`

- [ ] **Step 1: Write `.gitattributes`**

```gitattributes
# CCE — Git attributes
# Enforces consistent line endings across macOS/Linux/Windows checkouts

* text=auto eol=lf

# Explicit text files
*.cs        text eol=lf diff=csharp
*.csproj    text eol=lf
*.sln       text eol=lf
*.props     text eol=lf
*.targets   text eol=lf
*.ts        text eol=lf
*.tsx       text eol=lf
*.js        text eol=lf
*.jsx       text eol=lf
*.json      text eol=lf
*.jsonc     text eol=lf
*.yml       text eol=lf
*.yaml      text eol=lf
*.md        text eol=lf
*.sh        text eol=lf
*.html      text eol=lf
*.scss      text eol=lf
*.css       text eol=lf
*.editorconfig text eol=lf
Dockerfile  text eol=lf
.env.example text eol=lf
.gitignore  text eol=lf
.gitattributes text eol=lf

# Windows-only scripts keep CRLF
*.bat       text eol=crlf
*.cmd       text eol=crlf
*.ps1       text eol=crlf

# Binary — prevent line-ending munging
*.png       binary
*.jpg       binary
*.jpeg      binary
*.gif       binary
*.webp      binary
*.svg       text eol=lf
*.ico       binary
*.pdf       binary
*.zip       binary
*.gz        binary
*.tar       binary
*.woff      binary
*.woff2     binary
*.ttf       binary
*.otf       binary
*.eot       binary

# Generated / lockfiles — mark as generated so GitHub PR diff collapses them
pnpm-lock.yaml    -diff linguist-generated=true
package-lock.json -diff linguist-generated=true
yarn.lock         -diff linguist-generated=true
contracts/*.json  linguist-generated=true
```

- [ ] **Step 2: Verify**

Run: `git check-attr -a README.md`
Expected output contains: `README.md: text: auto` and `README.md: eol: lf`.

- [ ] **Step 3: Commit**

```bash
git add .gitattributes
git -c commit.gpgsign=false commit -m "chore(phase-00): add .gitattributes with LF normalization + binary markers"
```

---

## Task 0.3: Add `.gitignore`

**Files:**
- Create: `.gitignore`

- [ ] **Step 1: Write `.gitignore`**

```gitignore
# CCE — root .gitignore
# Applies repo-wide; per-tool subdirs may add their own .gitignore later

######################
# Secrets / env
######################
.env
.env.*
!.env.example
!.env.local.example
*.pem
*.key
*.crt
*.pfx
*.p12
mkcert/

######################
# .NET
######################
bin/
obj/
out/
artifacts/
*.user
*.suo
*.userprefs
*.VisualState.xml
*.nuget.props
*.nuget.targets
project.lock.json
TestResults/
*.trx
coverage/
*.coverage
*.coveragexml
*.lcov
*.opencover.xml
BenchmarkDotNet.Artifacts/

######################
# Node / Angular / Nx
######################
node_modules/
.pnpm-store/
.npm/
.yarn/
.nx/cache/
.nx/workspace-data/
dist/
tmp/
.angular/
.turbo/
*.tsbuildinfo
.cache/
.parcel-cache/

######################
# IDE / OS
######################
.idea/
.vs/
.vscode/*
!.vscode/extensions.json
!.vscode/settings.recommended.json
*.swp
*.swo
.DS_Store
Thumbs.db
ehthumbs.db
Desktop.ini
$RECYCLE.BIN/

######################
# Logs
######################
logs/
*.log
npm-debug.log*
pnpm-debug.log*
yarn-debug.log*
yarn-error.log*
lerna-debug.log*

######################
# Test artifacts
######################
playwright-report/
playwright/.cache/
test-results/
e2e-results/

######################
# Docker volumes (bind-mounted)
######################
.docker-data/
volumes/

######################
# Coverage / security reports
######################
coverage-reports/
sbom/
trivy-report.json
zap-report.*
dependency-check-report.*

######################
# Generated OpenAPI clients (regenerated; exception per path below)
######################
frontend/libs/api-client/src/lib/generated/
# But keep generated folder in repo if committed intentionally post-CI
!frontend/libs/api-client/src/lib/generated/.gitkeep
```

- [ ] **Step 2: Verify**

Run: `git check-ignore -v .env`
Expected: prints a line matching `.gitignore:<line>:.env` showing the rule that ignores it.

Run: `git check-ignore -v node_modules/foo` (no such dir needed; dry-check)
Expected: prints a line matching the `node_modules/` rule.

- [ ] **Step 3: Commit**

```bash
git add .gitignore
git -c commit.gpgsign=false commit -m "chore(phase-00): add .gitignore covering .NET, Node/Nx, IDE, secrets, coverage, Docker volumes"
```

---

## Task 0.4: Add `.env.example` and `.env.local.example`

**Files:**
- Create: `.env.example`
- Create: `.env.local.example`

**Rationale:** Two files so CI/deploy pipelines consume `.env.example` (shared config shape) while developers copy `.env.local.example` → `.env.local` (local overrides, gitignored by Task 0.3). No real secret values ever checked in.

- [ ] **Step 1: Write `.env.example`**

```dotenv
# CCE — baseline environment variables (safe defaults, no secrets)
# Copy to .env for docker-compose overrides, or use in CI.
# Actual secrets belong in .env.local (gitignored).

########################################
# Environment name
########################################
CCE_ENV=development

########################################
# SQL Server (docker-compose service: sqlserver)
########################################
SQL_HOST=sqlserver
SQL_PORT=1433
SQL_DATABASE=CCE
SQL_USER=sa
# SQL_PASSWORD set in .env.local (example: Strong!Passw0rd)

########################################
# Redis (docker-compose service: redis)
########################################
REDIS_HOST=redis
REDIS_PORT=6379
# REDIS_PASSWORD empty for local dev (optional in prod)

########################################
# Keycloak (docker-compose service: keycloak)
########################################
KEYCLOAK_URL=http://keycloak:8080
KEYCLOAK_REALM_INTERNAL=cce-internal
KEYCLOAK_REALM_EXTERNAL=cce-external
KEYCLOAK_CLIENT_ID_INTERNAL=cce-admin-cms
KEYCLOAK_CLIENT_ID_EXTERNAL=cce-web-portal
# KEYCLOAK_CLIENT_SECRET_* set in .env.local

########################################
# API endpoints (used by Angular apps at runtime via /assets/env.json)
########################################
API_EXTERNAL_URL=http://localhost:5001
API_INTERNAL_URL=http://localhost:5002

########################################
# Frontend app origins (used by API CORS)
########################################
WEB_PORTAL_ORIGIN=http://localhost:4200
ADMIN_CMS_ORIGIN=http://localhost:4201

########################################
# SMTP (docker-compose service: maildev, dev only)
########################################
SMTP_HOST=maildev
SMTP_PORT=1025
SMTP_FROM=no-reply@cce.local
# SMTP_USER / SMTP_PASSWORD empty for MailDev

########################################
# SIEM sink (docker-compose service: papercut, dev stub)
########################################
SIEM_SINK_URL=http://papercut:37408

########################################
# ClamAV (docker-compose service: clamav, dev stub)
########################################
CLAMAV_HOST=clamav
CLAMAV_PORT=3310

########################################
# Sentry (empty DSN = SDK no-ops in dev)
########################################
SENTRY_DSN=
SENTRY_ENVIRONMENT=development
SENTRY_TRACES_SAMPLE_RATE=0.0

########################################
# Logging
########################################
LOG_LEVEL=Information
LOG_FORMAT=compact

########################################
# Feature flags (Foundation)
########################################
FEATURE_HSTS=false
FEATURE_MFA=false
```

- [ ] **Step 2: Write `.env.local.example`**

```dotenv
# CCE — local-only overrides (copy to .env.local, which is gitignored)
# DO NOT commit real secrets here. This is a template of the shape .env.local should take.

# SQL Server — local dev password (change to anything 8+ chars with upper/lower/digit/symbol)
SQL_PASSWORD=Strong!Passw0rd

# Redis — leave empty for local dev
REDIS_PASSWORD=

# Keycloak client secrets (regenerated on every `docker compose up` via realm config, but for
# .NET config to pick them up statically in local dev, paste the dev values from
# http://localhost:8080/admin/ after first startup into .env.local)
KEYCLOAK_CLIENT_SECRET_INTERNAL=dev-internal-secret-change-me
KEYCLOAK_CLIENT_SECRET_EXTERNAL=dev-external-secret-change-me

# Sentry — leave empty for local; paste a personal dev DSN if you want to test Sentry integration
SENTRY_DSN=
```

- [ ] **Step 3: Verify neither file contains secret-looking patterns that would trip Gitleaks**

Run:
```bash
grep -E '(password|secret|key)\s*=\s*[^[:space:]].{4,}' .env.example .env.local.example || echo "OK — no inline secret values found"
```
Expected: prints `OK — no inline secret values found` (values in `.env.local.example` are explicitly placeholder strings like `dev-internal-secret-change-me` that Gitleaks won't treat as real; confirmed in Task 0.5).

- [ ] **Step 4: Commit**

```bash
git add .env.example .env.local.example
git -c commit.gpgsign=false commit -m "chore(phase-00): add .env.example and .env.local.example templates (no real secrets)"
```

---

## Task 0.5: Install Gitleaks pre-commit hook

**Files:**
- Create: `security/gitleaks.toml`
- Create: `.husky/pre-commit`
- Create: `.husky/_/.gitignore`
- Modify: `package.json` (created in this task if absent — root placeholder)

**Rationale:** Gitleaks scans staged changes on every commit. Catches accidental secret leaks at the earliest possible point. Husky is the de-facto hook manager; even though we don't have a Node project at repo root yet, we install Husky at root with a minimal `package.json`.

- [ ] **Step 1: Ensure Gitleaks is installed locally**

Run:
```bash
which gitleaks || brew install gitleaks
gitleaks version
```
Expected: prints a version like `v8.x.x` or higher.

- [ ] **Step 2: Write `security/gitleaks.toml`**

```bash
mkdir -p security
```

File `security/gitleaks.toml`:

```toml
# CCE Gitleaks config — extends the default ruleset with project-specific allowlist
# Ref: https://github.com/gitleaks/gitleaks/blob/master/config/gitleaks.toml

title = "CCE Gitleaks config"

# Use Gitleaks' bundled default rules as the base
[extend]
useDefault = true

# Allow-list for known-safe patterns in our repo
[allowlist]
description = "CCE allowlist — placeholder values and generated files"
paths = [
  '''contracts/.*\.json$''',                       # generated OpenAPI specs
  '''frontend/libs/api-client/src/lib/generated/''',
  '''docs/.*''',                                   # documentation may contain example tokens
  '''keycloak/realm-export.json$''',               # dev-only Keycloak realm with placeholder client secrets
]
regexes = [
  '''dev-internal-secret-change-me''',
  '''dev-external-secret-change-me''',
  '''Strong!Passw0rd''',
  '''Admin123!''',
  '''no-reply@cce\.local''',
]

# Project rules (in addition to defaults)
[[rules]]
id = "cce-generic-client-secret"
description = "CCE — generic client secret shape (non-placeholder)"
regex = '''(?i)(client[_-]?secret|api[_-]?key|token)\s*[:=]\s*['"]?([A-Za-z0-9/+=_-]{32,})['"]?'''
keywords = ["client_secret", "api_key", "token"]
```

- [ ] **Step 3: Write root `package.json` (minimal)**

```json
{
  "name": "cce-root",
  "private": true,
  "version": "0.0.0",
  "description": "CCE Knowledge Center Phase 2 — monorepo root (husky + tooling only)",
  "scripts": {
    "prepare": "husky install"
  },
  "devDependencies": {
    "husky": "^9.1.6"
  }
}
```

- [ ] **Step 4: Install Husky**

Run:
```bash
# Use npm at repo root (pnpm workspace lives under frontend/)
npm install
```
Expected: `husky` installed under `node_modules/`, `.husky/` directory exists.

Note: add `node_modules/` to `.gitignore` already covered in Task 0.3.

- [ ] **Step 5: Write `.husky/pre-commit`**

```bash
#!/usr/bin/env sh
. "$(dirname -- "$0")/_/husky.sh"

# Abort commit if Gitleaks finds any staged secret.
# --staged limits scan to staged changes only (fast).
# --redact hides matched values in output.
# Config: security/gitleaks.toml

gitleaks protect \
  --staged \
  --redact \
  --config security/gitleaks.toml \
  --verbose
```

Make it executable:
```bash
chmod +x .husky/pre-commit
```

- [ ] **Step 6: Smoke-test the hook with a fake secret**

Run:
```bash
echo 'AWS_SECRET_ACCESS_KEY="AKIAIOSFODNN7EXAMPLE0"' > /tmp/fake-secret.txt
cp /tmp/fake-secret.txt ./fake-secret.txt
git add fake-secret.txt
git -c commit.gpgsign=false commit -m "chore: test gitleaks" || echo "HOOK BLOCKED COMMIT — EXPECTED"
# Clean up
git reset HEAD fake-secret.txt
rm fake-secret.txt /tmp/fake-secret.txt
```
Expected: final line prints `HOOK BLOCKED COMMIT — EXPECTED`. Gitleaks report shows the leak detected.

- [ ] **Step 7: Commit the hook setup**

```bash
git add security/gitleaks.toml package.json package-lock.json .husky/pre-commit
git -c commit.gpgsign=false commit -m "chore(phase-00): install Gitleaks pre-commit hook via Husky"
```

---

## Task 0.6: Add placeholder root `README.md`

**Files:**
- Create: `README.md`

**Rationale:** Root README exists from Task 0.1 onward so GitHub shows a project landing page. Full getting-started content is written in Phase 18 after every tool is wired; this task writes only a stub.

- [ ] **Step 1: Write `README.md` stub**

````markdown
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
````

- [ ] **Step 2: Verify renders**

Run: `head -20 README.md`
Expected: first 20 lines print including the `# CCE — Circular Carbon Economy Knowledge Center (Phase 2)` heading.

- [ ] **Step 3: Commit**

```bash
git add README.md
git -c commit.gpgsign=false commit -m "docs(phase-00): add placeholder root README (full content in phase 18)"
```

---

## Phase 00 — completion checklist

- [ ] `.editorconfig` committed.
- [ ] `.gitattributes` committed with LF normalization and binary markers.
- [ ] `.gitignore` committed covering .NET, Node/Nx, IDE, secrets, Docker, coverage.
- [ ] `.env.example` and `.env.local.example` committed — zero real secrets.
- [ ] `security/gitleaks.toml` and `.husky/pre-commit` committed.
- [ ] Husky installed — `.husky/pre-commit` is executable and blocks a fake-secret commit.
- [ ] `README.md` stub committed.
- [ ] `git log --oneline | head -10` shows 6 new atomic commits from this phase.
- [ ] `git status` shows clean working tree.

**If all boxes ticked, phase 00 is complete. Proceed to phase 01.**

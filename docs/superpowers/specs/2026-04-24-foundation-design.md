# CCE Platform — Foundation Sub-Project — Design Spec

**Project:** Circular Carbon Economy (CCE) Knowledge Center Platform — Phase 2
**Client:** Saudi Ministry of Energy — Sustainability & Climate Change Agency
**Sub-project:** 01 — Foundation (scaffolding only)
**Spec owner:** CCE build team
**Date:** 2026-04-24
**Status:** Draft — awaiting user review

---

## 1. Purpose

Establish the scaffolding that all nine CCE sub-projects build on. Foundation delivers no business features. It delivers the frame: repository layout, local Docker Compose dev environment, empty Angular and .NET shells wired together through a regenerated OpenAPI contract, authentication skeleton (Keycloak standing in for ADFS), observability, error handling, CI gates, and the project roadmap mapping every BRD requirement to its owning sub-project.

Foundation ends when:

- `docker compose up` brings all services healthy on a clean machine within 90 seconds.
- Both Angular apps render their empty shells in Arabic (default) with English toggle.
- Admin CMS can authenticate against Keycloak and display JWT claims.
- Both APIs respond to `/health` and `/health/ready` correctly.
- All CI gates (tests, coverage, lint, security scans, accessibility, load-test thresholds) are green.
- Roadmap and per-sub-project briefs are committed so nothing from the BRD is lost.

---

## 2. Context (from BRD + HLD)

The CCE Knowledge Center Phase 2 is a bilingual (Arabic primary, RTL — English secondary) platform with seven independently deployable containers per HLD §3.2:

1. External Web Portal (Angular)
2. Admin/CMS Portal (Angular)
3. External API (.NET Core)
4. Internal API (.NET Core)
5. Flutter mobile app (WebView shell, deferred to sub-project 9)
6. Integration Gateway (external services — KAPSARC, AD/ADFS, SIEM, Email, SMS, iCal)
7. Data layer (SQL Server 2022 HA + Redis)

Compliance: Saudi **DGA** (Digital Government Authority) UX and accessibility standards — WCAG 2.1 AA, Arabic-first typography and layout, prescribed color/typography tokens.

Foundation is the first of nine sub-projects; see §10 and the project roadmap for the full decomposition.

---

## 3. Locked Decisions

These are ratified — ADRs committed as part of Foundation.

| #        | Decision                                                                                                                              |
| -------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| ADR-0001 | Decomposition into 9 sub-projects, Foundation first                                                                                   |
| ADR-0002 | Angular 18 chosen over React                                                                                                          |
| ADR-0003 | Angular Material for components + Bootstrap 5 grid/utilities only (no Bootstrap components or theme) + DGA tokens layered on top      |
| ADR-0004 | Single git repo, `backend/` .NET 8 solution + `frontend/` Nx 20 workspace                                                             |
| ADR-0005 | Local-first Docker Compose dev; production hosting target deferred                                                                    |
| ADR-0006 | Keycloak as ADFS stand-in via OIDC for internal SSO; ASP.NET Identity for external users                                              |
| ADR-0007 | Strict TDD for backend Domain/Application/Infrastructure/API; test-after for Angular UI                                               |
| ADR-0008 | .NET 8 LTS, Angular 18.2, ngx-translate, Signals + services (no NgRx in Foundation)                                                   |
| ADR-0009 | OpenAPI as single contract source; TypeScript clients auto-generated                                                                  |
| ADR-0010 | Sentry for error tracking; no self-hosted Sentry in Foundation                                                                        |
| ADR-0011 | Security pipeline: CodeQL + Semgrep + SonarCloud + ZAP + Trivy + Gitleaks + Dependency Review                                         |
| ADR-0012 | a11y gate via axe-core in Playwright + ESLint a11y rules; k6 for load testing                                                         |
| ADR-0013 | Permissions as source-generated enum from `permissions.yaml`, single source of truth for backend policies + OpenAPI + frontend guards |
| ADR-0014 | Clean Architecture layering in backend (Domain / Application / Infrastructure / Api)                                                  |
| ADR-0015 | OIDC code-flow + PKCE; refresh tokens in httpOnly SameSite=Strict secure cookies (BFF pattern)                                        |

---

## 4. Architecture

### 4.1 Local runtime (Docker Compose)

```
┌───────────────────────────────────────────────────────────────────────┐
│                       Docker Compose (local dev)                       │
│                                                                        │
│  web-portal:4200 ────────────┐          ┌──────── admin-cms:4201       │
│   (Angular 18, ar/en/RTL)    │          │         (Angular 18)         │
│                              ▼          ▼                              │
│                      api-external:5001   api-internal:5002             │
│                          (.NET 8)           (.NET 8)                   │
│                              │  │             │  │                     │
│              ┌───────────────┘  └──────┬──────┘  └─────────┐           │
│              ▼                         ▼                   ▼           │
│         keycloak:8080             sqlserver:1433       redis:6379      │
│         (OIDC, realms:            (primary instance    (cache,         │
│          cce-internal,             with shadow         session,        │
│          cce-external)             database for tests) rate limit)    │
│                                                                        │
│     maildev:1080    papercut:25    clamav:3310    k6 (profile)         │
│     (SMTP stub)     (SIEM stub)    (virus stub)   (load test runner)   │
└───────────────────────────────────────────────────────────────────────┘
```

### 4.2 Backend layers (Clean Architecture)

```
CCE.Api.External     CCE.Api.Internal     CCE.Integration
       │                    │                    │
       └────────┬───────────┘                    │
                ▼                                │
          CCE.Application ◀──────────────────────┘
                │
                ▼
            CCE.Domain (no dependencies)
                ▲
                │
          CCE.Infrastructure
          (EF Core, Redis, OIDC, Serilog, SMTP, SMS, SIEM clients)
```

- **`CCE.Domain`** — entities, value objects, aggregate roots, domain events, permission enum (source-generated from `permissions.yaml`). Zero external dependencies.
- **`CCE.Application`** — MediatR handlers (use cases), FluentValidation validators, DTOs, interfaces for infrastructure.
- **`CCE.Infrastructure`** — EF Core 8 with SQL Server, Redis via StackExchange.Redis, Keycloak OIDC, Serilog → console/file/Papercut sinks, SMTP client, SMS client interface (stub), Sentry SDK, ClamAV client interface (stub).
- **`CCE.Api.External`** — public API; ASP.NET Core minimal APIs + Swashbuckle. Emits `openapi.json` at build time.
- **`CCE.Api.Internal`** — admin API; OIDC challenge configured for Keycloak `cce-internal` realm.
- **`CCE.Integration`** — empty placeholder project; fills in sub-project 8.

### 4.3 Frontend workspace (Nx)

```
frontend/
├── apps/
│   ├── web-portal/              # external Angular app (anonymous + registered users)
│   │   └── src/app/             # shell: header, locale switcher, router, health page
│   └── admin-cms/               # internal Angular app (Keycloak login)
│       └── src/app/             # shell: header, locale switcher, router, profile echo
└── libs/
    ├── ui-kit/                  # Material theme + DGA tokens + shared shell component
    ├── i18n/                    # ngx-translate setup, ar.json, en.json, RTL service
    ├── api-client/              # auto-generated TS clients from OpenAPI
    ├── auth/                    # OIDC config, guards, interceptors
    └── contracts/               # hand-written TS types for non-OpenAPI shapes (env config)
```

### 4.4 Contract bridge

1. Backend builds both APIs → each emits `openapi.json` via Swashbuckle.
2. A post-build MSBuild target copies both specs into `contracts/` at the repo root.
3. Nx target `nx run api-client:generate` regenerates TypeScript clients from those specs using `openapi-typescript-codegen`.
4. CI job `contracts` runs the generator and fails if the working tree has a diff — keeps clients in sync with APIs, prevents drift.

### 4.5 Repository layout

```
cce/
├── backend/
│   ├── CCE.sln
│   ├── Directory.Packages.props           # central package management
│   ├── Directory.Build.props              # shared MSBuild settings
│   ├── src/
│   │   ├── CCE.Domain/
│   │   ├── CCE.Application/
│   │   ├── CCE.Infrastructure/
│   │   ├── CCE.Api.External/
│   │   ├── CCE.Api.Internal/
│   │   └── CCE.Integration/
│   └── tests/
│       ├── CCE.Domain.Tests/
│       ├── CCE.Application.Tests/
│       ├── CCE.Infrastructure.Tests/
│       └── CCE.Api.IntegrationTests/
├── frontend/
│   ├── nx.json
│   ├── package.json
│   ├── pnpm-lock.yaml
│   ├── apps/
│   │   ├── web-portal/
│   │   ├── web-portal-e2e/              # Playwright + axe-core
│   │   ├── admin-cms/
│   │   └── admin-cms-e2e/
│   └── libs/
│       ├── ui-kit/
│       ├── i18n/
│       ├── api-client/
│       ├── auth/
│       └── contracts/
├── contracts/                              # checked-in OpenAPI specs
│   ├── openapi.external.json
│   └── openapi.internal.json
├── keycloak/
│   ├── realm-export.json
│   └── README.md
├── loadtest/
│   ├── scenarios/
│   │   ├── health-anonymous.js
│   │   └── health-authenticated.js
│   └── k6.Dockerfile
├── security/
│   ├── zap-ignore.md
│   ├── semgrep.yml
│   └── gitleaks.toml
├── scripts/
│   ├── dev-reset.sh
│   ├── generate-openapi.sh
│   └── audit-replay.sh
├── docs/
│   ├── adr/                                # 15 ADRs
│   ├── subprojects/                        # 9 briefs
│   ├── roadmap.md
│   ├── requirements-trace.csv
│   ├── threat-model.md
│   ├── a11y-checklist.md
│   └── superpowers/
│       ├── specs/
│       │   └── 2026-04-24-foundation-design.md
│       └── plans/
├── .github/
│   └── workflows/
│       ├── ci.yml
│       ├── codeql.yml
│       ├── zap-nightly.yml
│       └── loadtest.yml
├── .editorconfig
├── .gitattributes
├── .gitignore
├── .env.example
├── docker-compose.yml
├── docker-compose.override.yml
├── README.md
├── CONTRIBUTING.md
└── LICENSE
```

---

## 5. Components — what Foundation scaffolds

### 5.1 Backend (stubs only)

| Component                        | Contents in Foundation                                                                                                                                                                                                                                                     |
| -------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CCE.Domain`                     | Base classes `AggregateRoot`, `Entity<TId>`, `ValueObject`, `DomainEvent`. Permission enum generated from `permissions.yaml` (seed: `System.Health.Read`). No business entities.                                                                                           |
| `CCE.Application`                | MediatR registered. `HealthQuery` + handler. `AuthenticatedHealthQuery` + handler. FluentValidation wired.                                                                                                                                                                 |
| `CCE.Infrastructure`             | `CceDbContext` with zero business entities + initial migration creating only `__EFMigrationsHistory` and `AuditEvents`. Redis connection factory. OIDC/JWT config bound from `appsettings`. Serilog + Sentry + Papercut sink. Stubs for Email/SMS/SIEM/ClamAV.             |
| `CCE.Api.External`               | `/health`, `/health/ready`, `/health/authenticated` (requires JWT from `cce-external` realm). Swagger UI. CORS for `localhost:4200`. ProblemDetails middleware. Correlation ID middleware. Rate limiting middleware. Localization middleware. Security headers middleware. |
| `CCE.Api.Internal`               | Same as External but CORS for `localhost:4201`. OIDC challenge configured for Keycloak `cce-internal` realm.                                                                                                                                                               |
| `CCE.Integration`                | Empty project placeholder.                                                                                                                                                                                                                                                 |
| `tests/CCE.Domain.Tests`         | xUnit + FluentAssertions. One green test proving permission enum source-generation works.                                                                                                                                                                                  |
| `tests/CCE.Application.Tests`    | xUnit + NSubstitute. Green tests for both health handlers.                                                                                                                                                                                                                 |
| `tests/CCE.Infrastructure.Tests` | xUnit + Testcontainers (SQL Server 2022, Redis). Green tests for DbContext, Redis connection, OIDC token validation.                                                                                                                                                       |
| `tests/CCE.Api.IntegrationTests` | xUnit + `WebApplicationFactory` + Testcontainers. Green tests: `/health` 200 anonymous, `/health/authenticated` 401 without token, 200 with valid token, 403 with missing permission, 400 with invalid Accept-Language.                                                    |

### 5.2 Frontend (stubs only)

| Component         | Contents in Foundation                                                                                                                                                                                                        |
| ----------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `apps/web-portal` | `<app-root>` with top bar (logo + locale switcher ar/en + sign-in button), router outlet, one route `/` showing localized welcome message, one route `/health` calling external API. RTL toggles `dir="rtl"` on Arabic.       |
| `apps/admin-cms`  | Same shell, but calls internal API. Sign-in button triggers Keycloak OIDC flow. Post-login route `/profile` echoes JWT claims.                                                                                                |
| `libs/ui-kit`     | Angular Material 18 theme module; DGA tokens (palette, typography IBM Plex Sans Arabic + Frutiger, spacing scale); imports `bootstrap-grid.min.css` + `bootstrap-utilities.min.css` **only**; shared `<app-shell>` component. |
| `libs/i18n`       | ngx-translate init; `ar.json` + `en.json` with Foundation strings; `LocaleService` (persistence, `dir` attribute update).                                                                                                     |
| `libs/api-client` | Auto-generated from `contracts/openapi.external.json` + `openapi.internal.json` via `openapi-typescript-codegen`. Nx target `generate`.                                                                                       |
| `libs/auth`       | `angular-auth-oidc-client` setup, `AuthGuard`, `BearerInterceptor`, `RefreshInterceptor`, `LoginService`, OIDC callback handler.                                                                                              |
| `libs/contracts`  | Hand-written TS for env config loader (`/assets/env.json`).                                                                                                                                                                   |

### 5.3 Infrastructure root

| File/Dir                                        | Purpose                                                                                                                                                  |
| ----------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `docker-compose.yml`                            | SQL Server 2022, Redis 7, Keycloak 25 (realm pre-imported), MailDev, Papercut, ClamAV, both APIs, both Angular dev servers, k6 (via `loadtest` profile). |
| `docker-compose.override.yml`                   | Local-only overrides (volumes, exposed ports, dev certs).                                                                                                |
| `keycloak/realm-export.json`                    | Pre-baked `cce-internal` + `cce-external` realms. Seeded admin user `admin@cce.local / Admin123!` with `SuperAdmin` role.                                |
| `.github/workflows/ci.yml`                      | Three jobs: `backend`, `frontend`, `contracts`.                                                                                                          |
| `.github/workflows/codeql.yml`                  | CodeQL for C# + TypeScript on PR.                                                                                                                        |
| `.github/workflows/zap-nightly.yml`             | OWASP ZAP baseline nightly, full scan on main.                                                                                                           |
| `.github/workflows/loadtest.yml`                | k6 on manual dispatch.                                                                                                                                   |
| `.editorconfig`, `.gitignore`, `.gitattributes` | Standard hygiene.                                                                                                                                        |
| `README.md`                                     | Getting-started for macOS/Linux/Windows.                                                                                                                 |
| `CONTRIBUTING.md`                               | Branch model, commit conventions, PR checklist.                                                                                                          |
| `docs/adr/`                                     | 15 ADRs (§3).                                                                                                                                            |

---

## 6. Data flow (Foundation only)

Two flows prove the scaffold end-to-end. Both are covered by integration + E2E tests and must stay green forever.

### 6.1 Flow A — Anonymous health (external app)

```
Browser ── GET localhost:4200 ──▶ web-portal (Angular)
                                      │
                                      │ user visits /health page
                                      ▼
                   GET localhost:5001/health (Accept-Language: ar)
                                      │
                                      ▼
                           CCE.Api.External
                             │
                             ├─▶ MediatR → HealthQuery → { status, version, locale }
                             └─▶ Serilog logs request (correlation id in header + body)
                                      │
                                      ▼
                            200 { status: "ok", ... }
                                      │
                                      ▼
                          Angular renders, Playwright + axe-core asserts
```

Proves: CORS, logging, correlation-id propagation, i18n header handling, MediatR wiring, Swagger generation, OpenAPI→TS client regeneration, Angular HTTP interceptor chain, axe a11y gate.

### 6.2 Flow B — Authenticated health (admin CMS)

```
Browser ── GET localhost:4201 ──▶ admin-cms (Angular)
                                      │
                                      │ unauthenticated → AuthGuard redirects
                                      ▼
                 302 ──▶ keycloak:8080/realms/cce-internal/.../auth
                                      │ user logs in as admin@cce.local
                                      ▼
                 302 back to localhost:4201/auth/callback?code=...
                                      │ lib exchanges code for tokens (PKCE)
                                      ▼
                 GET localhost:5002/health/authenticated
                 Authorization: Bearer <jwt>
                                      │
                                      ▼
                           CCE.Api.Internal
                             │
                             ├─▶ JWT middleware validates against Keycloak JWKS
                             ├─▶ Policy requires claim role: SuperAdmin
                             ├─▶ Redis GET session:{sub} (miss) → SET TTL 15min
                             ├─▶ MediatR → AuthenticatedHealthQuery → claims echo
                             └─▶ Serilog logs (user id + correlation id)
                                      │
                                      ▼
                            200 { status: "ok", user: {...}, claims: [...] }
```

Proves: OIDC code flow + PKCE, JWT validation via JWKS, policy mapping, Redis session, refresh-token interceptor path, RTL layout under authenticated shell, Sentry user-scope population, structured logging with user context.

### 6.3 Cross-cutting

- **Correlation IDs** — Angular HTTP interceptor generates `X-Correlation-Id` (UUIDv7) on every request if absent. Backend reads or creates, sets on Serilog scope + `Activity.Current`, echoes in response header. Frontend logs it on errors; backend Serilog indexes on it.
- **Locale** — Angular sends `Accept-Language`. Backend reads via `IRequestCultureFeature`, applies `CultureInfo` to current thread, FluentValidation resource lookups respect it, ProblemDetails `title` + `detail` localized.

---

## 7. Error handling & observability

### 7.1 Backend middleware order

```
[0] RequestLoggingMiddleware     — assigns/echoes X-Correlation-Id, starts Serilog scope
[1] ExceptionHandlingMiddleware  — unhandled exception → ProblemDetails (500)
[2] ValidationExceptionFilter    — FluentValidation failure → 400 with field errors
[3] SecurityHeadersMiddleware    — CSP, HSTS (prod), Referrer-Policy, Permissions-Policy
[4] AuthenticationMiddleware     — JWT bearer or OIDC
[5] AuthorizationMiddleware      — permission policies
[6] RateLimitingMiddleware       — Redis fixed-window, anon stricter than authed
[7] LocalizationMiddleware       — sets CultureInfo from Accept-Language
```

Every error response returns RFC 7807 ProblemDetails with `correlationId` in body + header. PII never in messages. Stack traces never leave the server.

### 7.2 Frontend `HttpErrorInterceptor`

| Status  | Behavior                                                                        |
| ------- | ------------------------------------------------------------------------------- |
| 400     | Maps field errors to Angular `FormControl` via `setErrors`; form displays them. |
| 401     | Attempts silent refresh once; on failure, clears session, redirects to login.   |
| 403     | Routes to `/forbidden` page showing attempted permission (ar/en).               |
| 404     | Toast `common.notFound` translation key.                                        |
| 5xx     | Toast `common.serverError` + "copy correlation id" button; logs to console.     |
| Network | Toast `common.networkError`; retries idempotent GETs once with 500 ms backoff.  |

All toasts use Material `MatSnackBar` with DGA colors and RTL-aware positioning.

### 7.3 Observability

| Signal                | Tool                                                                          | Location              |
| --------------------- | ----------------------------------------------------------------------------- | --------------------- |
| Structured logs       | Serilog → console + file + Papercut sink                                      | Both APIs             |
| Request tracing       | `Activity` with correlation id as trace id                                    | Both APIs             |
| Angular client errors | Override `ErrorHandler` → console + Sentry + POST `/api/client-errors` (stub) | Both apps             |
| Health                | `/health` (liveness), `/health/ready` (SQL + Redis + JWKS)                    | Both APIs             |
| Metrics               | `prometheus-net` → `/metrics` (not scraped in Foundation, just wired)         | Both APIs             |
| Error tracking        | Sentry SDK in both stacks, DSN from env (no-op when empty)                    | Both APIs + both apps |

### 7.4 Security headers (baseline)

- `Content-Security-Policy` strict-dynamic, no `unsafe-inline`. Angular dev server uses nonces.
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Strict-Transport-Security` prod only (env-gated)
- `Permissions-Policy` denying camera/mic/geolocation by default
- `Cross-Origin-Opener-Policy: same-origin`, `Cross-Origin-Embedder-Policy: require-corp`

---

## 8. Testing strategy

### 8.1 Test-first (strict TDD)

| Layer                                 | Framework                                        | Scope                                                                |
| ------------------------------------- | ------------------------------------------------ | -------------------------------------------------------------------- |
| `CCE.Domain`                          | xUnit + FluentAssertions                         | All entity invariants, value objects, permission enum generation     |
| `CCE.Application`                     | xUnit + NSubstitute + FluentAssertions           | All handlers, validators, policies                                   |
| `CCE.Infrastructure` (critical paths) | xUnit + Testcontainers                           | EF mappings against real SQL, Redis cache ops, OIDC token validation |
| `CCE.Api.*`                           | xUnit + `WebApplicationFactory` + Testcontainers | One integration test per endpoint: happy path + 401 + 403 + 400      |

### 8.2 Test-after

| Layer                         | Framework                 | Scope                                                                            |
| ----------------------------- | ------------------------- | -------------------------------------------------------------------------------- |
| Angular services              | Jest (via `@nx/jest`)     | HTTP interceptors, guards, i18n, error handler, state services                   |
| Angular components with logic | Jest + `@angular/testing` | Only components with conditional rendering, form logic, computed signals         |
| Angular pure templates        | —                         | Not tested in isolation                                                          |
| E2E                           | Playwright                | One smoke test per Foundation flow + axe-core a11y + locale switch + 401 refresh |

### 8.3 Coverage gates (CI-enforced)

- `CCE.Domain` + `CCE.Application`: ≥ **90%** line.
- `CCE.Infrastructure` + `CCE.Api.*`: ≥ **70%** line.
- Angular libs + apps: ≥ **60%** line overall, no per-file gate.
- Playwright: no coverage gate; suite must pass.
- a11y: zero `critical`/`serious` violations per axe-core.
- k6 Foundation thresholds: p95 `/health` < 100 ms, p95 `/health/authenticated` < 200 ms at 100 / 50 VUs respectively for 60 s.

### 8.4 Determinism

- No mocked SQL/Redis in backend tests; Testcontainers per test-class.
- No `Thread.Sleep`; clock abstracted via `ISystemClock`.
- Angular tests use MSW to intercept HTTP.
- Playwright seeds DB via SQL fixtures, resets per spec file.

---

## 9. Security posture (Foundation baseline)

### 9.1 Transport & headers

- HTTPS in dev via mkcert certs mounted into docker-compose.
- Strict CSP, HSTS prod-only, X-Content-Type-Options, Referrer-Policy, Permissions-Policy.
- CORS allowlist per API; no wildcards.

### 9.2 AuthN / AuthZ

- OIDC code flow + PKCE. No implicit flow. No password grant.
- JWT validation via JWKS, clock skew 5 min, `aud` + `iss` enforced.
- Refresh tokens rotated per use; httpOnly SameSite=Strict secure cookies (BFF pattern).
- Session timeout 15 min idle, 8 hr absolute; sliding refresh on API activity.
- MFA hook scaffolded on external API (stub provider, real in sub-project 8).
- Source-generated permission enum; policies decorated via `[RequirePermission(...)]`.

### 9.3 Data protection

- SQL connections encrypted (`Encrypt=True`) even locally.
- External user passwords hashed with PBKDF2 at OWASP 2026 iteration count.
- `[Encrypted]` attribute + EF `ValueConverter` for PII; AES-256, keys via `DataProtection` API.
- `AuditEvents` table append-only (enforced via SQL trigger + domain event).

### 9.4 Input & output

- FluentValidation on every DTO. No anonymous DTOs crossing the API boundary.
- AntiForgery tokens on admin CMS (cookie auth surface).
- File upload endpoints scaffolded with max size, MIME sniffing, ClamAV stub.

### 9.5 Secrets & supply chain

- `.env.example` checked in, `.env.local` gitignored.
- Gitleaks + detect-secrets pre-commit and CI.
- Dotnet: `dotnet list package --vulnerable` CI gate. OWASP Dependency-Check CI gate.
- Frontend: pnpm audit CI gate.
- Docker: base images pinned by digest, Trivy scan.
- Renovate committed config; grouped PRs weekly.
- CycloneDX SBOM emitted per build.

### 9.6 Security scanning pipeline

| Tool                                                   | Trigger                        | Gate                                                         |
| ------------------------------------------------------ | ------------------------------ | ------------------------------------------------------------ |
| CodeQL (C# + TS)                                       | PR                             | Blocks on high/critical                                      |
| Semgrep (OWASP Top Ten + CSharp + TS + security-audit) | PR                             | Blocks on high/critical                                      |
| SonarCloud                                             | PR                             | Quality gate: 0 new bugs, 0 new vulns, 0 unreviewed hotspots |
| OWASP ZAP baseline                                     | Nightly on `docker compose up` | Artifact, no block                                           |
| OWASP ZAP full scan                                    | Post-merge on `main`           | Auto-opens issues                                            |
| Trivy (container + fs)                                 | PR                             | Blocks on high+ CVEs                                         |
| Gitleaks                                               | Pre-commit + PR                | Blocks on any hit                                            |
| Dependency Review                                      | PR                             | Blocks on known CVEs or GPL/unknown license                  |
| OWASP Dependency-Check                                 | PR                             | Blocks on high+                                              |
| `superpowers:security-review`                          | Pre-merge ritual               | Advisory report                                              |

### 9.7 Threat model

`docs/threat-model.md` v1 in Foundation using STRIDE against the HLD architecture. Each sub-project updates it as new surfaces land.

---

## 10. Project roadmap — BRD traceability

This section is **new and critical** — it ensures every BRD requirement is owned by a sub-project. Authoritative file: `docs/roadmap.md`. Machine-readable counterpart: `docs/requirements-trace.csv`.

### 10.1 Sub-project sequence

| #   | Sub-project                                       | Key deliverables                                                                                                                                                                                                                                                                 | BRD sections                                                     | Depends on |
| --- | ------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------- | ---------- |
| 1   | **Foundation** (this spec)                        | Scaffolding, CI, OIDC stub, roadmap                                                                                                                                                                                                                                              | 4.1.32 (NFR baseline)                                            | —          |
| 2   | **Data & Domain**                                 | All EF entities, migrations, seed data, permission matrix, audit log fully modeled                                                                                                                                                                                               | 4.1.31 (permissions), 4.1.32                                     | 1          |
| 3   | **Internal API**                                  | Admin endpoints: user mgmt, content mgmt, news/events, resources, community moderation, country-profile admin, expert registration review, reports                                                                                                                               | 4.1.19–4.1.29, 6.2.37–6.2.63, 6.4.1–6.4.9                        | 2          |
| 4   | **External API**                                  | Public endpoints: browse homepage, platform info, resources, knowledge maps data, interactive city data, news/events, country profiles, user profile, ratings, personalized suggestions, smart-assistant search, community posts, policies, account/login/forgot-password/logout | 4.1.1–4.1.18, 6.2.1–6.2.36                                       | 2          |
| 5   | **Admin / CMS Portal**                            | Angular admin app — dashboards + all admin screens hitting Internal API, reports UI                                                                                                                                                                                              | 4.1.19–4.1.29, 6.2.37–6.2.63, 6.3.9–6.3.16, 6.4                  | 3          |
| 6   | **External Web Portal**                           | Angular public app — all user-facing pages hitting External API                                                                                                                                                                                                                  | 4.1.1–4.1.18, 6.2.1–6.2.36, 6.3.1–6.3.8                          | 4          |
| 7   | **Feature Modules** (each its own sub-brainstorm) | Knowledge Maps visualization, Interactive City 3D simulation, Smart Assistant (NL search + retrieval), rich Knowledge Community                                                                                                                                                  | 4.1.4, 4.1.5, 4.1.11, 4.1.12, 4.1.13, 6.2.6–6.2.9, 6.2.19–6.2.31 | 6          |
| 8   | **Integration Gateway**                           | KAPSARC connector, ADFS federation (replace Keycloak in non-dev), real Email (SMTP), real SMS, SIEM shipper, iCal generator, MFA provider                                                                                                                                        | 6.5, 7.1, 7.2, §3.1.2–3.1.8 HLD                                  | 3, 4       |
| 9   | **Mobile (Flutter)**                              | WebView shell for iOS/Android/Huawei app stores, push notifications, session passthrough                                                                                                                                                                                         | HLD §3.2.2                                                       | 6          |

### 10.2 Per-sub-project briefs

Each `docs/subprojects/0N-*.md` file contains:

- **Goal** — one paragraph outcome statement.
- **BRD references** — explicit list of sections/user-stories covered.
- **Dependencies** — upstream sub-projects that must be complete.
- **Rough estimate** — T-shirt size only (S/M/L/XL).
- **DoD skeleton** — placeholder items to be refined at that sub-project's own brainstorm cycle.

Full specs and plans are written **at each sub-project's own brainstorm cycle**, not now. The briefs only ensure nothing gets lost.

### 10.3 Requirements trace CSV

`docs/requirements-trace.csv` columns: `brd_ref, title_ar, title_en, subproject_id, subproject_name, dod_anchor, status`. Every BRD functional requirement (4.1.1–4.1.29), every user story (6.2.1–6.2.63), every report (6.4.1–6.4.9), every integration requirement (6.5), every message/alert (7.1, 7.2), and every NFR (4.1.32) has a row. Foundation seeds the CSV; future sub-projects update `status` as items complete.

---

## 11. Definition of Done

Foundation is complete **only when all items below are green**.

### Functional

1. `docker compose up` brings every service to healthy within 90 seconds on a clean machine (macOS arm64, Linux amd64, Windows/WSL2).
2. `http://localhost:4200/` renders the web-portal shell with Arabic default, working locale toggle to English, `dir="rtl"` flipping correctly, and a reachable `/health` page.
3. `http://localhost:4201/` redirects unauthenticated users to Keycloak, logs in as `admin@cce.local`, returns to `/profile` showing decoded JWT claims.
4. External API `/health` returns 200; `/health/ready` returns 200 only when SQL + Redis + Keycloak JWKS all healthy; otherwise 503.
5. Internal API `/health` returns 200; `/health/authenticated` requires JWT and enforces the `SuperAdmin` policy.
6. Both APIs' `/swagger` renders. `openapi.external.json` and `openapi.internal.json` exported to `contracts/`. `libs/api-client` regenerates without diff.

### Quality

7. `dotnet test` — all green. Coverage gates met (≥90% domain/app, ≥70% infra/api).
8. `pnpm nx run-many -t test` — all green. Coverage ≥60% overall.
9. `pnpm nx run-many -t lint` — zero warnings, a11y ESLint rules enforced.
10. `pnpm nx e2e web-portal-e2e` and `admin-cms-e2e` — Playwright smoke + axe-core a11y green.
11. `k6 run loadtest/scenarios/health-anonymous.js` — p95 < 100 ms at 100 VUs × 60 s.
12. `k6 run loadtest/scenarios/health-authenticated.js` — p95 < 200 ms at 50 VUs × 60 s.

### Security

13. All CI security scans green on `main`: CodeQL, Semgrep, SonarCloud gate, Trivy, Gitleaks, Dependency Review, OWASP Dependency-Check.
14. `docs/threat-model.md` v1 committed.
15. `.env.example` present, `.env.local` gitignored, no secret in any file.

### Documentation

16. 15 ADRs (§3) committed to `docs/adr/`.
17. `docs/roadmap.md` and `docs/subprojects/02-…09-*.md` briefs committed.
18. `docs/requirements-trace.csv` seeded with every BRD entry.
19. `README.md` — getting-started that runs successfully on macOS, Linux, Windows/WSL2.
20. `CONTRIBUTING.md` — branch model, commit conventions, PR checklist.
21. `docs/a11y-checklist.md` committed (manual a11y items beyond tooling).

### Release

22. Tag `foundation-v0.1.0` exists on `main`.
23. CI workflow fully green on `main` at that tag.

---

## 12. Explicitly NOT in Foundation

Moved into their owning sub-projects per §10. Nothing is dropped — everything BRD-mandated lives in `docs/roadmap.md` + `docs/requirements-trace.csv` with an explicit sub-project owner.

- Business entities beyond `AuditEvents` — sub-project 2.
- Real API endpoints beyond health — sub-projects 3, 4.
- Real UI pages beyond shell + health + login callback + profile echo — sub-projects 5, 6.
- KAPSARC, SMS, real SMTP, SIEM, iCal, ADFS integrations — sub-project 8.
- Reports UI and generation — sub-projects 3, 5.
- Knowledge Maps, Interactive City, Smart Assistant, Knowledge Community rich features — sub-project 7.
- Flutter mobile — sub-project 9.
- Prod-only security items (HSTS enabled, real Key Vault, production TLS chain) — sub-project 8 + deployment epic.
- External user self-registration flow — sub-project 4.
- Content management (news/events/resources CRUD, country profile workflow) — sub-projects 3, 5.
- Permission matrix beyond `System.Health.Read` — sub-project 2.

---

## 13. Risks

| #   | Risk                                                                            | Mitigation                                                                                                                                                                                    |
| --- | ------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| R1  | Keycloak OIDC config diverges subtly from real ADFS (claim names, group claims) | Match claim names explicitly in `realm-export.json` to ADFS conventions (`upn`, `groups`, `preferred_username`); document mapping in ADR-0006. Sub-project 8 includes ADFS parity smoke test. |
| R2  | DGA UX/typography tokens change before Foundation ships                         | Tokens isolated to `libs/ui-kit/tokens`; any change is a one-file PR.                                                                                                                         |
| R3  | Nx + Angular 18 + Material 18 + ngx-translate version conflicts                 | Pin all versions; Renovate config groups major upgrades for coordinated PRs.                                                                                                                  |
| R4  | SQL Server container licensing for CI                                           | Use `mcr.microsoft.com/mssql/server:2022-latest` dev edition (free, adequate for CI).                                                                                                         |
| R5  | Sentry cloud costs if DSN committed accidentally                                | DSN always from env var; Gitleaks pattern catches any DSN checked in.                                                                                                                         |
| R6  | Solo-developer cadence: Foundation drags past reasonable time                   | Keep scope strictly to §5; any temptation to add "one more thing" goes to a sub-project brief instead.                                                                                        |
| R7  | RTL bugs only discovered in real screens (Foundation has almost none)           | `libs/i18n` includes an RTL smoke page listing every Material component at locale switch; Playwright+axe run it.                                                                              |

---

## 14. Open decisions (none)

All decisions through Section 7 are locked. The next brainstorm cycle (sub-project 2: Data & Domain) will surface new decisions.

---

## 15. Next step

After user approves this spec, transition to `superpowers:writing-plans` skill to produce the Foundation implementation plan.

— End of spec —

# Changelog

All notable changes to the CCE Knowledge Center project are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [data-domain-v0.1.0] — 2026-04-28

The Data & Domain sub-project — full entity model, persistence layer, migration, seeders, architecture invariants.

### Highlights

- 36 domain entities across 8 bounded contexts (Identity, Content, Country, Community, KnowledgeMaps, InteractiveCity, Notifications, Surveys)
- ASP.NET Identity tables coexisting with CCE entities (one DbContext, one transaction)
- 41 permissions × 6 roles wired through the Roslyn source generator (extended in Phase 01 from Foundation's flat schema)
- Soft-delete via `ISoftDeletable` + reflection-based global query filter
- `AuditingInterceptor` writing `AuditEvent` for every `[Audited]` entity change in same transaction
- `DomainEventDispatcher` publishing events via MediatR `IPublisher` post-commit
- `DbExceptionMapper` translating SQL 2601/2627 + concurrency errors to domain exceptions
- `DataDomainInitial` migration: 40 tables + 55 indexes + RowVersion columns + filtered unique indexes; 804-line DDL snapshot
- 4 idempotent seeders (`RolesAndPermissionsSeeder`, `ReferenceDataSeeder`, `KnowledgeMapSeeder`, `DemoDataSeeder`) using deterministic SHA-256 GUIDs
- 12 NetArchTest architecture rules enforcing Clean Architecture layering + sealed aggregates + `[Audited]` coverage

### Tests

- Domain: 284, Application: 12, Infrastructure: 30 (+ 1 skipped), Architecture: 12, Source-gen: 10, Api-integration: 28
- **Cumulative backend: 376 + 1 skipped**
- (Frontend test counts unchanged — sub-project 2 is backend-only.)

### Documentation

- 8 new ADRs (0019-0026) covering DbContext design, soft-delete, auditing, domain events, migration strategy, Identity coexistence, deterministic GUIDs, architecture tests
- `docs/data-domain-completion.md` — DoD verification report
- `docs/subprojects/02-data-domain-progress.md` — phase tracker (11/11 phases ✅)

### Tooling

- `dotnet-ef 8.0.10` pinned in `.config/dotnet-tools.json`
- New CPM packages: `Microsoft.Extensions.Identity.Stores`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `MediatR`, `Microsoft.EntityFrameworkCore.InMemory`, `NetArchTest.Rules`
- New `NoWarn` entries: CA1056, CA1054, CA1002, CA1308 (URL/list patterns), CA1861 (test-only)

## [foundation-v0.1.0] — 2026-04-24

The Foundation sub-project — scaffolding for all subsequent sub-projects.

### Highlights

- Backend (.NET 8 Clean Architecture): Domain + Application + Infrastructure + 2 APIs + Integration placeholder + 4 test projects
- Frontend (Nx 20 + Angular 18.2): web-portal + admin-cms + 5 shared libs (ui-kit, i18n, auth, api-client, contracts)
- Local Docker Compose dev environment: SQL (Azure SQL Edge for arm64), Redis, Keycloak (with seeded `cce-internal` + `cce-external` realms), MailDev, ClamAV
- Auth: Keycloak as ADFS stand-in via OIDC; service-account flow tested end-to-end
- Permissions: source-generated `Permissions` enum from `permissions.yaml`
- Persistence: EF Core 8 with snake_case naming, append-only `audit_events` table with SQL trigger
- API middleware: correlation IDs, ProblemDetails (RFC 7807), security headers, rate limiting, localization (ar default, en, RTL flip), JWT bearer
- Endpoints: `/`, `/health`, `/health/ready`, `/health/authenticated` (admin), `/auth/echo` (test only), `/swagger/v1/swagger.json`
- OpenAPI contract pipeline: backend → `contracts/*.json` → frontend `api-client` lib (drift-checked in CI)
- E2E: Playwright + axe-core (WCAG 2.1 AA gate); 15 tests across 3 browsers
- Load: k6 health-anonymous (p95 11.1ms < 100ms target), health-authenticated (p95 1.39ms < 200ms target)
- CI: 11 workflow files (CI, CodeQL, ZAP, loadtest, dep-review, dependabot, semgrep, trivy, sonarcloud, dep-check, sbom, gitleaks)
- Security: Gitleaks pre-commit hook, layered scanners, SBOM generation
- Documentation: 18 ADRs, roadmap with 9 sub-projects, 9 sub-project briefs, requirements traceability CSV (203 BRD rows), threat model (STRIDE), a11y checklist

### Test totals at this tag

- Backend: 62
- Frontend unit: 41
- E2E: 15
- **Total: 118**

### Known follow-ups

See `docs/foundation-completion.md` for the full list.

### Phase summary

The Foundation was built across 20 phases (00-19). Each phase has its own plan under
`docs/superpowers/plans/2026-04-24-foundation/phase-NN-*.md`. ~30 cross-phase patches
were captured during execution as real tooling quirks; each is documented in its
commit message.

[foundation-v0.1.0]: <will-be-tagged-by-Phase-19-Task-19.4>

### Commit history (latest 50)

| SHA | Subject |
|---|---|
| `e3d0950` | style(phase-19): apply Prettier formatting across all markdown |
| `41f8c04` | docs(phase-19): foundation completion report (DoD verification across 23 items) |
| `3bc1881` | docs(plan): add Phase 19 (DoD verification + release tag) detailed plan |
| `af1931b` | docs(phase-18): add 9 sub-project briefs + traceability CSV + threat model + a11y checklist + full README + CONTRIBUTING |
| `082f14b` | docs(phase-18): add docs/roadmap.md with 9-sub-project map + BRD references |
| `1241df9` | docs(phase-18): add ADR-0018 clamav-debian image for arm64 multi-arch |
| `cd3bf51` | docs(phase-18): add ADR-0017 Serilog file sink as dev SIEM stub (drop Papercut) |
| `57f3a69` | docs(phase-18): add ADR-0016 Azure SQL Edge for arm64 dev (prod unchanged) |
| `1c5041b` | docs(phase-18): add ADR-0015 OIDC code-flow + PKCE + BFF cookie pattern |
| `494f6cd` | docs(phase-18): add ADR-0014 Clean Architecture layering |
| `5653472` | docs(phase-18): add ADR-0013 permissions source-generated from permissions.yaml |
| `2eefd31` | docs(phase-18): add ADR-0012 a11y gate (axe-core) + k6 load thresholds |
| `864bd81` | docs(phase-18): add ADR-0011 security scanning pipeline (CodeQL/Semgrep/SonarCloud/ZAP/Trivy/...) |
| `6aabfdc` | docs(phase-18): add ADR-0010 Sentry for error tracking (no self-hosted in Foundation) |
| `8222898` | docs(phase-18): add ADR-0009 OpenAPI as single contract source with drift check |
| `da84adc` | docs(phase-18): add ADR-0008 version pins (.NET 8, Angular 18.2, ngx-translate, Signals) |
| `e21b8c0` | docs(phase-18): add ADR-0007 TDD policy (strict backend, test-after Angular UI) |
| `bd4ab36` | docs(phase-18): add ADR-0006 Keycloak as ADFS stand-in via OIDC |
| `b8a017e` | docs(phase-18): add ADR-0005 local-first Docker Compose dev environment |
| `199a90d` | docs(phase-18): add ADR-0004 single repo with backend + frontend workspaces |
| `8769ee9` | docs(phase-18): add ADR-0003 Material components + Bootstrap grid + DGA tokens |
| `0329a66` | docs(phase-18): add ADR-0002 Angular over React |
| `e1a89e9` | docs(phase-18): add ADR-0001 decomposition into 9 sub-projects |
| `b462bb6` | docs(plan): add Phase 18 (docs) detailed plan |
| `01f450b` | docs(phase-17): security/README.md documenting layered defenses + suppression policy |
| `7d70034` | feat(phase-17): Gitleaks CI workflow (full-history scan; complements Phase 00 pre-commit) |
| `6e6ff88` | feat(phase-17): CycloneDX SBOM workflow (.NET + npm) on main + tags |
| `677c541` | feat(phase-17): OWASP Dependency-Check weekly workflow with suppression file scaffold |
| `1cc1f6c` | feat(phase-17): SonarCloud config + workflow (gated on SONAR_TOKEN secret) |
| `15c6b5b` | feat(phase-17): Trivy filesystem + IaC config scan workflows (HIGH/CRITICAL) |
| `1249de9` | feat(phase-17): Semgrep CI workflow + project-specific rules (DateTime/secrets/console.log) |
| `fd66954` | docs(plan): add Phase 17 (security scanning configs) detailed plan |
| `fe1907e` | ci(phase-16): add PR template with test plan + security checklist + BRD traceability |
| `705be82` | ci(phase-16): Dependabot config (NuGet + npm + Actions, weekly grouped PRs) |
| `5c81f44` | ci(phase-16): GitHub dependency-review-action on PRs (deny GPL/AGPL, fail on high CVEs) |
| `7d70f1f` | ci(phase-16): k6 load test workflow (manual dispatch with scenario picker) |
| `e782db9` | ci(phase-16): nightly OWASP ZAP baseline scan against External API |
| `8890081` | ci(phase-16): CodeQL static analysis (C# + TypeScript) — PR gate + weekly scan |
| `2f02f57` | ci(phase-16): main CI workflow (backend + frontend + contract drift) gating PRs to main |
| `bead00a` | docs(plan): add Phase 16 (CI workflows) detailed plan |
| `4152f64` | feat(phase-15): accept additional Keycloak issuer URLs (host.docker.internal in dev) |
| `86661e9` | feat(phase-15): make rate limiter config-driven via RateLimiter:PermitLimit |
| `4b8d6fb` | feat(phase-15): add k6 service to docker-compose under 'loadtest' profile (host.docker.internal pass-through) |
| `e3b36d4` | feat(phase-15): add k6 scenarios for /health (anonymous) + /auth/echo (authenticated) with thresholds |
| `068c551` | docs(plan): add Phase 15 (k6 load tests) detailed plan |
| `a3fc4fb` | fix(phase-14): make admin-cms smoke spec robust against auto-login race + Keycloak scope rejection |
| `90269c7` | fix(phase-14): move inject() calls before await in provideAppInitializer |
| `af70704` | feat(phase-14): admin-cms-e2e smoke specs (shell render, sign-in → Keycloak redirect) with axe-core |
| `75bb665` | feat(phase-14): web-portal-e2e smoke specs (root, locale switch, health page) with axe-core a11y |
| `6dc7a1d` | feat(phase-14): install @axe-core/playwright + per-app a11y helper (WCAG 2.1 AA gate) |

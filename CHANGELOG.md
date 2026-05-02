# Changelog

All notable changes to the CCE Knowledge Center project are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [web-portal-v0.2.0] — 2026-05-02

### Added
- Sub-7 Knowledge Maps full UX, layered on top of the Sub-6 Phase 9 skeleton at `apps/web-portal/src/app/features/knowledge-maps/`. ~38 tasks across 9 phases. Public users open a map at `/knowledge-maps/:id`, see its nodes laid out as an interactive Cytoscape graph (server-driven `LayoutX/Y` positions), click nodes to read details in a side panel, search and filter to focus on concepts, hold multiple maps open in tabs, switch between graph and list view for accessibility, and export selections in PDF / PNG / SVG / JSON.
- `MapViewerStore` signal-driven state container provided per-route (10 actions + computed `openTabs`, `activeTab`, `selectedNode`, `matchedIds`, `dimmedIds`, `notFound`).
- `GraphCanvasComponent` Cytoscape wrapper: preset layout from server `LayoutX/Y`, click + box-select events, locale-mirror effect with viewport preservation, three reactive effects for elements / selectedId / dimmedIds.
- `NodeDetailPanelComponent` CSS-driven drawer (right rail desktop / bottom sheet mobile via 720px breakpoint) with click-to-re-select outbound edges + ESC keyboard shortcut.
- `SearchAndFiltersComponent` with 200ms debounced input + NodeType chip toggles. Highlight + dim semantics; non-matching nodes drop to 0.3 opacity.
- `TabsBarComponent` horizontal scroll-x strip with active underline + close ×; multi-map workflow with `?open=` URL hydration; last-tab-close routes back to `/knowledge-maps`.
- 4 export serializers (PNG / SVG / JSON / PDF) with `cytoscape-svg` and `jspdf` lazy-imported only when the user picks SVG or PDF; rubber-band selection feeds export-the-selection-subgraph (JSON keeps closure: only edges where both endpoints are in the selection).
- `ListViewComponent` accessible `<ul>` tree grouped by NodeType with focusable button rows + `aria-current` + outbound-edge counts. View-mode toggle (graph ↔ list) preserves selection + filter state because both views bind to the same store signals.
- URL state captures `?q=` (search) + `?type=` (filter) + `?open=` (other tabs) + `?view=` (graph|list) + `?node=` (selection); deep-linkable in any combination.
- 4 new ADRs (0043–0046): server-driven graph layout, RTL x-mirror strategy, lazy-loaded heavy graph deps, dual-view a11y.
- 3 new packages (lazy-loaded via cytoscape-loader): `cytoscape@^3.30`, `cytoscape-svg@^0.4`, `jspdf@^2.5`. Plus `@types/cytoscape@^3.21` (devDep). Initial bundle untouched.
- 85 new web-portal Jest tests (362 total, was 277). admin-cms unchanged at 218/218. ui-kit unchanged at 27/27. Total Jest suite: 607 across 127 suites.

### Notes
- Knowledge Maps lazy chunk grows ~400KB on first navigation (cytoscape only). SVG plugin (+20KB) and jsPDF (+150KB) only load on actual export. Initial bundle stays within the 1mb / 1.5mb budget.
- Polish backlog (6 items) captured in `docs/sub-7-knowledge-maps-completion.md`: related maps suggestions, side-by-side comparison, algorithmic layout reset, vector PDF export, admin-cms node-position curation UI, Lighthouse audit deferred to deployment verification.
- The original Sub-7 brainstorm scoped Maps + City + Assistant under one roof. Decomposed during brainstorming into Sub-7 (Maps, this release), Sub-8 (City), Sub-9 (Assistant), Sub-10 (Deployment / Infra). Each gets its own brainstorm → spec → plan → execution cycle.

## [web-portal-v0.1.0] — 2026-05-01

### Added
- Public-facing Angular SPA at `apps/web-portal` consuming the Sub-4 External API. ~62 tasks across 9 phases. Anonymous-first browsing across Home, Knowledge Center, News, Events, Country profiles, Search, Community; authenticated flows for account (register, /me/profile read+edit, expert request, service rating), notifications drawer + bell badge with 60s unread-count poll, follows page + `[cceFollow]` directive backed by signal-cached `FollowsRegistryService`, community write (compose post dialog, inline reply form, 1-5 star rating, mark-as-answer for post authors).
- BFF cookie auth without `angular-auth-oidc-client`. AuthService bootstraps via `/api/me`, tolerates 401 silently for anonymous users; signIn calls `window.location.assign('/auth/login?returnUrl=...')`. Production-grade `authGuard` with one-time cold-start refresh.
- Three same-origin scoped HTTP interceptors (correlation-id, bff-credentials, server-error) — codified from day 1, with `isInternalUrl()` guard so cross-origin requests pass through untouched (mid-Sub-5 lesson absorbed).
- Hybrid layout: `PortalShellComponent` with top horizontal nav (Header) + collapsible left filter rail per browse page. Bell-icon notifications dialog (right-aligned). RTL flips automatically.
- Sub-7 placeholder entry-points: `/knowledge-maps`, `/interactive-city`, `/assistant` consume real endpoints today (`GET /api/knowledge-maps`, `GET /api/interactive-city/technologies`, `POST /api/assistant/query`) with "Coming in Sub-7" notices for the deferred UX.
- Anonymous-friendly community write affordances: `cce-sign-in-cta` block replaces compose / reply / rate / mark-answer controls when not authenticated, with return-URL preservation.
- Single-locale community content per post/reply (`content` + `locale`) with "in {{locale}}" badge when locale ≠ active LocaleService — cross-language threads are a feature.
- 4 new ADRs (0039–0042): BFF cookie auth anonymous-first, hybrid layout, same-origin interceptors, anonymous write affordances.
- 265 web-portal Jest tests across 60 suites; Playwright + axe-core smoke specs at `apps/web-portal-e2e/src/` (smoke, layout, knowledge-center, news-events, countries, search, account, notifications-follows, community).
- Promoted from `apps/admin-cms` to `libs/ui-kit` in Phase 0.6: paged-table, error-formatter (`toFeatureError`), feedback (ToastService, ConfirmDialogService, ConfirmDialogComponent). Both apps now share the same primitives.
- `i18n/{en,ar}.json` extended with `nav.*`, `header.*`, `footer.*`, `filter.*`, `search.*`, `searchType.*`, `errors.*` (added `retry`), `resources.*`, `news.*`, `events.*`, `countries.*` (public sub-keys merged with admin's existing block), `kapsarc.*`, `account.*`, `notifications.*` (renamed admin's `notifications.title` → `notifications.templatesTitle` to free the public-portal slot), `follows.*`, `community.*`, `knowledgeMaps.*`, `interactiveCity.*`, `assistant.*`. Full ar mirroring throughout.

### Notes
- Maps / City / Assistant ship as skeletons consuming real list endpoints; the full graph view, scenario builder, and conversational threading defer to Sub-7.
- Phase 9 polish backlog (8 items) documented in `docs/web-portal-completion.md`: profile concurrency token, search hit linking for News/Pages, follow-chip + community author hydration, threaded replies, edit-own-reply, topic tree, real-time notification push, Lighthouse audit deferred to deployment verification.
- Bundle-size budget bumped from 500kb/1mb to 1mb/1.5mb in Phase 7.6 after the MatDialog dependency for the notifications drawer pushed initial above the prior 1mb cap.

## [admin-cms-v0.1.0] — 2026-04-30

### Added
- ~30 admin screens in `apps/admin-cms` covering BRD §4.1.19–§4.1.29: identity (users + roles + state-rep assignments), expert workflow (requests + approve/reject + profiles), content (resources CRUD + asset upload + publish, country-resource-requests by-id), content publishing (news + events + pages + homepage sections), taxonomies (resource categories + topics), community moderation (soft-delete by-id), country admin (list + detail + profile editor), notification templates (list + create + edit), reports (8 streaming-CSV downloads), audit log query.
- Cross-cutting: 3 functional `HttpInterceptorFn` (auth, server-error, correlation-id), `AuthService` (signals + `/api/me` bootstrap), `permissionGuard` (`CanMatchFn`), `*ccePermission` structural directive, `ToastService` + `ConfirmDialogService` + `ErrorFormatter` (`toFeatureError` mapping), `<cce-shell>` layout with `mat-sidenav-container` + `<cce-side-nav>` (14 role-gated nav items), generic `<cce-paged-table>`.
- 4 new ADRs (0035–0038): standalone components + signals-first, hybrid HTTP error handling, permission gating, by-ID power-user forms for missing list endpoints.
- 238 admin-cms Jest tests; Playwright + `@axe-core/playwright` smoke + layout regression spec; lint clean; production build clean.
- `contracts/openapi.{internal,external}.json` regenerated (Sub-3/Sub-4 had not re-run `generate-openapi.sh` after shipping endpoints); `libs/api-client` regenerated to expose 75+ internal operations + 90+ external operations.

### Notes
- The auth model deviates from the spec (BFF cookies were specified; Foundation shipped `angular-auth-oidc-client` directly). Sub-5 layers `AuthService` on top of the existing OIDC client; rotating to BFF cookies is a future migration.
- E2E uses Playwright (Foundation choice) instead of the spec's Cypress, with `@axe-core/playwright` providing equivalent axe coverage.
- Several backend gaps are documented in `docs/admin-cms-completion.md`: missing list endpoints for country-resource-requests and community moderation flags, missing `Produces<T>()` annotations causing the generated client to emit `Response = unknown` (worked around by hand-defined DTOs in each feature `*.types.ts`).

## [external-api-v0.1.0] — 2026-04-29

### Added
- ~55 public REST endpoints under `/api/...` and BFF auth under `/auth/...`.
- BFF cookie + Bearer dual-mode auth (`BffSessionMiddleware` decrypts cookie → synthesises Bearer header).
- Redis output cache (60s TTL, anonymous-only; authenticated requests bypass).
- Tiered rate limiter (Anonymous / Authenticated / SearchAndWrite, config-driven via `RateLimiter:PermitLimit`).
- Meilisearch search backend (`ISearchClient` abstraction + `MeilisearchClient` + `MeilisearchIndexer` hosted service).
- HtmlSanitizer for user-submitted content (`IHtmlSanitizer` / `HtmlSanitizerWrapper`, mganss NuGet).
- `ICountryScopeAccessor` + `HttpContextCountryScopeAccessor` for StateRep-scoped reads.
- Smart-assistant stub endpoint (`POST /api/assistant/query`; LLM integration deferred to Sub-8).
- KAPSARC snapshot read (`GET /api/kapsarc/snapshots/{countryId}`; ingest pipeline deferred to Sub-8).
- Service rating submit (`POST /api/surveys/service-rating`; anonymous OK, returns 201 + id).
- `IServiceRatingService` + `ServiceRatingService` Infrastructure implementation.
- 5 new ADRs (0030–0034): country-scoped query pattern, BFF/Bearer dual auth, Meilisearch, Redis output cache, HtmlSanitizer.
- Net new tests: +232 (Application +146, Api Integration +73, Infrastructure +13).

## [internal-api-v0.1.0] — 2026-04-29

### Added
- ~47 admin REST endpoints under `/api/admin/*` (users, roles, expert workflow, content, taxonomies, country admin, notifications, reports, audit log).
- JIT user-sync middleware (Keycloak `sub` → `users` row, IMemoryCache 5min TTL).
- `IFileStorage` abstraction + `LocalFileStorage` (dev) + `IClamAvScanner` synchronous TCP scan.
- 8 streaming-CSV reports under `/api/admin/reports/*.csv`.
- `Audit.Read` permission + `GET /api/admin/audit-events` query.
- `RoleToPermissionClaimsTransformer` (flattens role-name groups to permission claims).
- `HttpContextCurrentUserAccessor` (reads JWT sub on Internal API).
- 3 new ADRs (0027–0029).
- Permission count: 41 → 42.
- Net new tests: +418 (Application +266, Api Integration +139, Domain +6, Infrastructure +7).

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

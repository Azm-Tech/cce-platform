# Sub-Project 10a — App productionization — Completion Report

**Tag:** `app-v1.0.0`
**Date:** 2026-05-03
**Spec:** [App Productionization Design Spec](../project-plan/specs/2026-05-03-sub-10a-design.md)
**Plan:** [App Productionization Implementation Plan](../project-plan/plans/2026-05-03-sub-10a.md)
**Predecessor:** [Sub-9 Smart Assistant completion](sub-9-assistant-completion.md)
**Successors (planned):** Sub-10b Deployment automation, Sub-10c Production infra (AD federation, multi-env, backup, IDD DNS/LB validation)

---

## Summary

Sub-10a takes the four shipped applications (`Api.External`, `Api.Internal`, `web-portal`, `admin-cms`) from "passes tests on a developer laptop" to "production-quality builds with real observability and a real LLM". Every Sub-7/8/9 polish item that was deferred by name lands here — Lighthouse audit, axe-core CI gates, real LLM client. Plus the production Docker images, Serilog/Sentry/Prometheus wiring, and RAG-lite citation source they all need.

**No infra/network/IIS scripting.** Sub-10b targets a real environment; Sub-10c lands AD federation, DNS/LB validation against IDD v1.2, multi-env (test/pre-prod/prod/DR), and backup automation.

**Total tasks:** ~17 across 4 phases. **Test counts: backend Application 439/439 (+10 since Sub-9 baseline); backend Infrastructure 54/55 (1 skipped, unchanged) (+4 since Sub-9 baseline); frontend web-portal 502/502 (unchanged).**

## Phase checklist

- [x] **Phase 00** — Cross-cutting: `Anthropic.SDK` 5.0.0 added to `Directory.Packages.props`; `LoggingExtensions` + `PrometheusExtensions` no-op skeletons under `CCE.Api.Common.Observability`; `AnthropicOptions` config record + `AssistantClientFactory` (always-stub) skeleton in `CCE.Infrastructure.Assistant`; `Assistant:` + `Serilog:` config sections added to both APIs' `appsettings.json` with safe defaults; env-var reference doc.
- [x] **Phase 01** — Production Docker images: `Dockerfile` for each of the four apps (`Api.External`, `Api.Internal`, `web-portal`, `admin-cms`); per-app `nginx.conf` for the SPAs (SPA fallback, gzip, immutable cache on hashed bundles, no-cache on `index.html`); `docker-compose.prod.yml` wiring all four locally; new CI `docker-build` job using `docker/build-push-action@v6` with GHA layer cache, runs `/health` (backend) + `/` (frontend) smoke probes per image.
- [x] **Phase 02** — Observability + real LLM: `UseCceSerilog` wired into both API hosts (console JSON-compact + rolling-file daily + optional Sentry sink, all gated on env-vars); `app.UseSerilogRequestLogging()` picks up `CorrelationId` from the existing middleware; `UseCcePrometheus` exposes `/metrics` with `cce_assistant_streams_total{provider}` + `cce_assistant_citations_total{kind}` custom counters; `CitationSearch` (RAG-lite Jaccard token-overlap, 6 tests); `AnthropicSmartAssistantClient` against `Anthropic.SDK` 5.0.0 with `IAnthropicStreamProvider` mockable abstraction (4 tests); `AssistantClientFactory` honours `Assistant:Provider` + `ANTHROPIC_API_KEY` (4 tests, falls back to stub on missing key with stderr warning).
- [x] **Phase 03** — CI gates + close-out: `lighthouse.yml` workflow against `/knowledge-maps/<seeded-guid>` production build with `lighthouserc.json` asserting accessibility ≥ 90 + best-practices ≥ 90; `a11y.yml` workflow against `/interactive-city` + `/assistant` via the existing `@axe-core/playwright` integration; `a11y.spec.ts` test fixture; ADR-0051 (Anthropic.SDK + RAG-lite citations) + ADR-0052 (Observability stack: Serilog + Sentry + Prometheus); this completion doc; CHANGELOG entry; tag `app-v1.0.0`.

## Endpoint coverage

Sub-10a adds zero new endpoints. Existing endpoints unchanged. New runtime surfaces:

| Path | Method | Auth | Purpose |
|---|---|---|---|
| `/metrics` | GET | Anon | Prometheus scrape (both APIs) |

## Container images

| Image | Base (build) | Base (runtime) | Port |
|---|---|---|---|
| `cce-api-external` | `mcr.microsoft.com/dotnet/sdk:8.0` | `mcr.microsoft.com/dotnet/aspnet:8.0` | 8080 |
| `cce-api-internal` | `mcr.microsoft.com/dotnet/sdk:8.0` | `mcr.microsoft.com/dotnet/aspnet:8.0` | 8080 |
| `cce-web-portal` | `node:22-alpine` | `nginx:alpine` | 8080 |
| `cce-admin-cms` | `node:22-alpine` | `nginx:alpine` | 8080 |

All four are multistage Linux containers, runs as non-root user (backend `app` UID 1654 from the ASP.NET base; nginx default), declares `HEALTHCHECK`. Sub-10b decides whether prod actually deploys the Linux containers on a Windows Server 2022 host (per IDD v1.2) or rebuilds for Windows-native — that's a 10b decision.

## Observability env-vars

Documented in `project-plan/plans/2026-05-03-sub-10a/env-vars.md`.

| Variable | Used by | Effect |
|---|---|---|
| `ASSISTANT_PROVIDER` | Api.External | `stub` (default) or `anthropic` |
| `ANTHROPIC_API_KEY` | Api.External | required when provider=anthropic; absent → stub fallback + stderr warn |
| `SENTRY_DSN` | both APIs | absent → Sentry sink is no-op |
| `LOG_LEVEL` | both APIs | overrides `Serilog:MinimumLevel`; default `Information` |

## ADRs

- [ADR-0051 — Anthropic.SDK + RAG-lite citations](adr/0051-anthropic-sdk-rag-lite-citations.md)
- [ADR-0052 — Observability stack: Serilog + Sentry + Prometheus](adr/0052-observability-stack-serilog-sentry-prometheus.md)

## CI gates

| Workflow | Triggers | Asserts |
|---|---|---|
| `ci.yml` `docker-build` job | PR + push | All four images build with GHA cache + smoke probes pass |
| `lighthouse.yml` | PR (paths: frontend/backend) + workflow_dispatch | a11y ≥ 90, best-practices ≥ 90 (errors); performance ≥ 70, SEO ≥ 80 (warnings) on `/knowledge-maps/:id` |
| `a11y.yml` | PR (paths: frontend) + workflow_dispatch | Zero critical/serious axe-core findings on `/interactive-city` + `/assistant` |

## Test counts (final)

| Project | Suites/Tests | Delta from Sub-9 |
|---|---|---|
| backend `Application.Tests` | 439 | +10 (6 CitationSearch + 4 AssistantClientFactory) |
| backend `Infrastructure.Tests` | 54 (1 skipped) | +4 (4 AnthropicSmartAssistantClient); fixed 2 stale KnowledgeMapSeederTests assertions in passing |
| backend `Api.IntegrationTests` (assistant SSE) | 2 | unchanged from Sub-9 |
| frontend `web-portal` | 90 / 502 | unchanged |
| frontend `admin-cms` | unchanged | unchanged |

## UX decisions baked in

| Area | Decision | Rationale |
|---|---|---|
| LLM provider | Anthropic.SDK 5.0 | ADR-0051 |
| Citations | RAG-lite (Jaccard) over embeddings | ADR-0051 — small catalog, no vector store dependency |
| Stub fallback | Always available, default `Provider=stub` | CI / offline dev / no-key envs work out-of-box |
| Container OS | Linux (multistage) | Smaller, faster builds; Windows hosts run Linux containers; Sub-10b decides final deploy form |
| Observability split | Serilog + Sentry + Prometheus (3 best-of-breed) over OTel | ADR-0052 — packages already paid for; OTel migration when distributed tracing matters |
| `correlation_id` flow | `BeginScope` + `FromLogContext` (no custom enricher) | Existing middleware already attaches it; Serilog reads scope automatically |

## Polish backlog (carried forward)

- **Embedding-based RAG** for assistant citations when the catalog grows beyond ~50 rows. Vector store decision deferred.
- **OpenTelemetry migration** when distributed tracing across services becomes a real requirement.
- **Counter-name consolidation** — `cce_assistant_streams_total` / `_runtime_total` collision is contained but should be cleaned up.
- **Promotion of `ConfirmDialogComponent` to `ui-kit`** (Sub-9 polish backlog) — still out of scope.
- **Markdown rendering of assistant replies** (Sub-9 polish backlog) — still out of scope.
- **Frontend error sink** (browser → Sentry) — out of scope.

## Next steps (Sub-10b / Sub-10c)

- **Sub-10b — Deployment automation**: migration runner, prod docker-compose / IIS scripts targeting one environment end-to-end, secrets management, smoke probes. Tag `deploy-v1.0.0`.
- **Sub-10c — Production infra + DR**: AD federation via Keycloak, DNS + LB config validation against IDD v1.2 (CCE-ext / api.CCE / etc), backup automation per IDD policy, multi-env (test → pre-prod → prod → DR), production observability sinks (Sentry DSN, Prometheus scrape config), secret rotation. Tag `infra-v1.0.0`.

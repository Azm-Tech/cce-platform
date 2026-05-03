# ADR-0052 — Observability stack: Serilog + Sentry + Prometheus

**Status:** Accepted
**Date:** 2026-05-03
**Deciders:** CCE backend team

---

## Context

Sub-1's foundation pulled `Serilog`, `Sentry.AspNetCore`, `Sentry.Serilog`, `prometheus-net`, and `prometheus-net.AspNetCore` into `Directory.Packages.props` but never wired them into the host. Through Sub-7/8/9 the apps shipped with default Microsoft `ILogger` console output and no metrics endpoint. Sub-10a is "make production-quality" — observability has to land here.

Three approaches considered:

| Option | Tradeoff |
|---|---|
| **Serilog (logs) + Sentry (errors) + Prometheus (metrics) — chosen** | Each tool is best-in-class for its slice. Packages already declared — zero net new heavy deps. Familiar shapes for ops folks. Three sinks to maintain config for. |
| **OpenTelemetry-everything (logs + metrics + traces via OTLP)** | Single instrumentation surface; vendor-neutral; better for tracing. Bigger learning curve; ecosystem maturity for log + metric correlation under .NET 8 still uneven. |
| **Application Insights** | Single-vendor (Microsoft) story; deep .NET integration. Vendor lock + cost concerns; the IDD doesn't prescribe Azure. |

OTel is where the industry is heading and we should migrate when production scale demands distributed tracing. Today's needs (structured stdout logs that `kubectl logs` / `docker logs` parse well, error capture for triage, simple histograms for latency / RPS) are met by the lighter stack with packages we already pay for.

---

## Decision

**Backend observability uses three independent sinks, each owned by a single extension method in `CCE.Api.Common.Observability/`:**

- `LoggingExtensions.UseCceSerilog(IHostBuilder)` — reads `Serilog:*` config + `SENTRY_DSN` env-var. Emits JSON-compact events to stdout (Serilog.Formatting.Compact). Optional rolling-file sink (daily, retained 7 days, gated on `Serilog:FileSink:Enabled`). Optional Sentry sink for `Warning+` events when `SENTRY_DSN` is set.
- `PrometheusExtensions.UseCcePrometheus(WebApplication)` — wires `UseHttpMetrics()` for default `http_request_duration_seconds` histograms and `MapMetrics("/metrics")` for the scrape endpoint (`AllowAnonymous`). Two custom counters declared as static fields: `cce_assistant_streams_total{provider}` and `cce_assistant_citations_total{kind}`.
- The existing `CorrelationIdMiddleware` already calls `_logger.BeginScope(...)`. Wiring `app.UseSerilogRequestLogging()` after it picks up the `CorrelationId` scope automatically via `Enrich.FromLogContext()` — no custom enricher needed.

**Config shape (committed in `appsettings.json`):**

```json
"Serilog": {
  "MinimumLevel": "Information",
  "FileSink": { "Enabled": true, "Path": "logs/cce-.log", "RetainedDays": 7 }
}
```

**Env-var overrides:**
- `SENTRY_DSN` — absent → no Sentry sink (CI / dev default)
- `LOG_LEVEL` → `Serilog:MinimumLevel`

## Consequences

**Positive:**
- Three sinks each do exactly one thing and can fail independently. Sentry going down doesn't stop the file sink; the file sink filling disk doesn't stop stdout.
- JSON-compact stdout works for `docker logs` + `kubectl logs` + log aggregators that ingest stdout (Loki, Splunk forwarder, etc.).
- Correlation IDs flow into log events with no per-event code — the existing `BeginScope` is enough.
- `/metrics` is a thin HTTP endpoint Prometheus can scrape from anywhere on the network. No agent install needed.
- The two assistant counters give Sub-10b/10c (and operators) the LLM observability story for free.

**Negative:**
- Sub-10a doesn't wire OTel traces. Cross-service request tracing (when Sub-10b/c lands a deployed multi-service topology) will need a separate decision.
- Serilog config is split between `appsettings.json` and env-vars — no single source of truth. Mitigation: `env-vars.md` lists every override.
- `prometheus-net` declares two counters (`Streams`, `Citations`) on the type-static `Metrics` registry; cross-project counters with the same name would collide. AnthropicSmartAssistantClient declares its own static counters with `_runtime` suffix to avoid the collision; PrometheusExtensions exposes the canonical names. Future cleanup: consolidate.

**Neutral:**
- Migrating to OTel later is a known-good path (Serilog has an OTel sink; prometheus-net can be replaced by `OpenTelemetry.Exporter.Prometheus.AspNetCore`). The `UseCceSerilog` / `UseCcePrometheus` boundary stays stable.
- Frontend observability is out of scope for Sub-10a — Sub-9's `aria-live` + `axe-core` checks are about a11y, not observability. A future phase can add a browser-side error sink.

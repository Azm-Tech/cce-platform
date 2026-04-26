# ADR-0010: Sentry for error tracking (no self-hosted in Foundation)

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §3](../superpowers/specs/2026-04-24-foundation-design.md#3-stack-decisions)

## Context

Both the backend and the two Angular apps need error/exception capture with breadcrumbs, source maps, and release tagging. Foundation should give every contributor an opt-in, low-friction way to test the wiring locally without standing up an observability stack.

A self-hosted Sentry instance is operationally heavy (Postgres, Redis, ClickHouse, multiple workers); not Foundation's job.

## Decision

- **Tool:** [Sentry](https://sentry.io) for both backend (`Sentry.AspNetCore`) and frontend (`@sentry/angular`).
- **DSN:** Configured via env var (`SENTRY_DSN_*`); when empty, the SDK no-ops cleanly — no errors, no console noise.
- **Source maps:** Frontend uploads sourcemaps to Sentry on build (sub-project 5/6 wires this). Backend uses PDBs.
- **No self-hosted Sentry container in Foundation.**

## Consequences

### Positive

- Uniform error pipeline dev → staging → prod.
- DSN-empty no-op means contributors don't need a Sentry account to run the stack.
- Release tagging ties errors to specific git refs once CI uploads release metadata (sub-project 8 / Ops).

### Negative

- Hosted Sentry costs money and ships data off-prem; ministry policy may require a self-hosted instance later.
- A self-host swap is a service stand-up + DSN re-issue, but requires no code change (DSN is just a URL).

### Neutral / follow-ups

- Re-evaluate self-hosted vs SaaS in sub-project 8 (Integration Gateway / Ops).
- PII scrubbing is configured in the SDK — verify against gov data-handling rules in sub-project 8.

## Alternatives considered

### Option A: Application Insights / OpenTelemetry only

- Rejected: AI is Azure-coupled; OTel without a backend is incomplete.

### Option B: Self-hosted Sentry

- Rejected for Foundation: out of scope; revisit when hosting target is decided.

### Option C: No error tracking in Foundation

- Rejected: error visibility is a security gate (silent 500s hide auth or input issues); see [ADR-0011](0011-security-scanning-pipeline.md).

## Related

- [ADR-0011](0011-security-scanning-pipeline.md)
- `backend/src/CCE.Api.External/Program.cs`, `backend/src/CCE.Api.Internal/Program.cs`
- `frontend/apps/*/src/main.ts`

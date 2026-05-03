# Phase 03 — CI gates + close-out (Sub-10a)

> Parent: [`../2026-05-03-sub-10a.md`](../2026-05-03-sub-10a.md) · Spec: [`../../specs/2026-05-03-sub-10a-design.md`](../../specs/2026-05-03-sub-10a-design.md) §11 (success criteria)

**Phase goal:** Land the Lighthouse + axe-core CI gates Sub-7/8/9 deferred to Sub-10. Write ADRs 0051 + 0052. Ship the completion document, update CHANGELOG, and tag `app-v1.0.0`.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 02 closed (commit `c612812` or later); Application 439 + Infrastructure 54 tests passing.

---

## Task 3.1: Lighthouse + axe-core CI workflows

**Files:**
- Create: `.github/workflows/lighthouse.yml`.
- Create: `.github/workflows/a11y.yml`.
- Create: `frontend/apps/web-portal-e2e/src/a11y.spec.ts` if not present (extend existing if it is).

**Lighthouse against `/knowledge-maps/<seeded GUID>` production build.** Boots SQL Server + Redis containers (the catalog endpoint hits the DB), runs `CCE.Seeder` to populate, builds and serves the SPA via `npx serve`, then runs Lighthouse. Asserts a11y ≥ 90, performance ≥ 70, best-practices ≥ 90.

**axe-core against `/interactive-city` and `/assistant`** (production build). Uses the existing `@axe-core/playwright` library that Sub-6 already pulled in for `web-portal-e2e`. Asserts zero critical/serious findings.

## Task 3.2: ADR-0051 — Anthropic SDK + RAG-lite citations

`docs/adr/0051-anthropic-sdk-rag-lite-citations.md`. Documents the choice of the community Anthropic.SDK 5.0 over OpenAI SDK / OpenAI-compat REST, plus token-overlap (Jaccard) citations vs embeddings.

## Task 3.3: ADR-0052 — Observability stack

`docs/adr/0052-observability-stack-serilog-sentry-prometheus.md`. Documents Serilog (logs) + Sentry (errors) + Prometheus (metrics) over a single OpenTelemetry stack.

## Task 3.4: Completion doc + CHANGELOG + tag

- `docs/sub-10a-app-productionization-completion.md` mirroring Sub-7/8/9 shape.
- CHANGELOG entry under `app-v1.0.0`.
- `git tag app-v1.0.0`.

## Phase 03 close-out

- Both new CI workflows committed.
- Both ADRs land.
- Completion doc cross-references everything.
- Tag exists locally.
- Final test/build green.

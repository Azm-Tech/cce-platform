# ADR-0012: A11y gate (axe-core) + k6 load thresholds

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §11](../../project-plan/specs/2026-04-24-foundation-design.md#11-definition-of-done)

## Context

DGA UX guidelines and BRD non-functional requirements both demand WCAG 2.1 AA conformance. "Manual a11y review" is unreliable across a long project; we want a CI gate that catches the categories axe-core can detect (color contrast, missing labels, ARIA misuse, etc.). Axe doesn't catch everything (focus order, screen-reader narration sense, keyboard traps) — those go on a manual checklist.

Likewise, response time is a stated NFR; we need a baseline gate that catches regressions on the cheapest endpoint to test (`/health`).

## Decision

### Accessibility

- **Tool:** axe-core, embedded in Playwright E2E tests for both `web-portal-e2e` and `admin-cms-e2e`.
- **Gate:** Zero `critical` or `serious` WCAG 2.1 AA violations on the smoke E2E pass.
- **Manual checklist:** [`docs/a11y-checklist.md`](../a11y-checklist.md) covers what axe can't catch.

### Load testing

- **Tool:** [k6](https://k6.io), scripts in `loadtest/`.
- **Thresholds:**
  - `/health` (anonymous): p95 < 100 ms
  - `/health` (authenticated): p95 < 200 ms
- **Trigger:** CI on `loadtest/` change + nightly schedule.

## Consequences

### Positive

- A11y regressions visible in PR before merge; fixes are cheaper at PR time.
- Load thresholds catch the worst regressions (a 10× slowdown can't sneak through).
- k6 scripts are versioned alongside code — SLOs evolve as features land.

### Negative

- E2E + axe runtime is non-trivial; CI minutes go up.
- "Zero critical/serious" is strict — third-party widgets occasionally trigger violations that need scoped suppressions.

### Neutral / follow-ups

- Coverage of WCAG criteria expands as feature surface grows; sub-projects 5/6 own the broader a11y test plan.
- k6 thresholds widen to cover real endpoints in sub-projects 3/4.

## Alternatives considered

### Option A: Lighthouse CI only

- Rejected: weaker a11y rule coverage than axe.

### Option B: Manual a11y review only

- Rejected: doesn't scale; regressions slip through.

### Option C: JMeter over k6

- Rejected: k6's JS scripting + Prometheus output fits CI better.

## Related

- [ADR-0011](0011-security-scanning-pipeline.md)
- [`docs/a11y-checklist.md`](../a11y-checklist.md)
- `frontend/apps/*-e2e/`, `loadtest/`

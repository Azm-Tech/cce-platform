# ADR-0007: TDD policy — strict backend, test-after Angular UI

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §11](../../project-plan/specs/2026-04-24-foundation-design.md#11-definition-of-done)

## Context

Test-Driven Development pays off where logic is non-trivial, intent is hard to express in a snapshot, and regressions are costly. It pays off less for thin presentational components whose behavior is mostly Angular template wiring — there, the cost of writing the test before the template often exceeds the regression catch.

CCE has a clear split: backend has rich domain logic (permissions, validation, business rules, integrations), frontend has a mix of form-heavy CMS pages and presentational portal pages.

## Decision

**Strict TDD** (red → green → refactor) for:

- `Domain/`, `Application/`, `Infrastructure/` critical paths
- `Api.*` endpoint behavior tests

**Test-after** for Angular UI components: write the component, then add tests where coverage gates require, prioritizing logic-bearing services and pipes over template snapshots.

**Coverage gates:**

- Domain / Application: ≥ 90%
- Infrastructure / API: ≥ 70%
- Angular overall: ≥ 60%

## Consequences

### Positive

- Domain logic stays test-first — invariants and business rules can't rot.
- Frontend velocity isn't taxed by tests for trivial templates.
- Coverage gates are differentiated to reflect actual ROI, not a one-size-fits-all bar.

### Negative

- Coverage thresholds need per-layer tooling configuration; onboarding has to teach the split.
- "Test-after" can become "test-never" without a coverage gate; CI must enforce the 60% Angular floor.

### Neutral / follow-ups

- Coverage reports flow to SonarCloud ([ADR-0011](0011-security-scanning-pipeline.md)).
- E2E tests + axe-core ([ADR-0012](0012-a11y-axe-and-k6-loadtest.md)) compensate for lower component coverage.

## Alternatives considered

### Option A: Strict TDD everywhere

- Rejected: bad ROI for trivial Angular templates; demoralizing for UI work.

### Option B: Test-after everywhere

- Rejected: domain logic decays without test-first pressure; coverage gates without TDD discipline tend to drift.

### Option C: Property-based tests for domain

- Considered as a future addition; not a Foundation decision.

## Related

- [ADR-0011](0011-security-scanning-pipeline.md)
- [ADR-0012](0012-a11y-axe-and-k6-loadtest.md)
- `backend/tests/`, `frontend/apps/*-e2e/`

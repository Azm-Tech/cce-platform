# ADR-0026: Architecture invariants enforced via NetArchTest.Rules

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §8](../../project-plan/specs/2026-04-27-data-domain-design.md)

## Context

Clean Architecture layering (Domain ⇍ Application ⇍ Infrastructure), aggregate-root sealing, configuration namespacing, and `[Audited]` coverage are decisions only enforced today by code review. Reviews miss things; layering drifts over time.

## Decision

Add `CCE.ArchitectureTests` test project using NetArchTest.Rules 1.3.2. Ship 12 architectural rules covering:
- Domain layer doesn't depend on Application / Infrastructure / Mvc.
- Application layer doesn't depend on Infrastructure / EFCore.
- All aggregate roots are sealed.
- Domain events are sealed records.
- All entities live under `CCE.Domain.*`.
- Configurations are sealed and live under `Configurations.*`.
- All aggregate roots carry `[Audited]`.

These tests run on every CI build alongside the unit tests.

## Consequences

### Positive
- Layering is enforced automatically — refactor breaks fail CI immediately.
- New developers can refer to the test file as live documentation of architectural rules.
- `[Audited]` coverage drift is caught at build time.

### Negative
- The architecture tests can't catch all violations (e.g., reflection-based dependencies). They're a backstop, not a complete guarantee.
- Rule changes need careful staging — too aggressive a rule can block legitimate refactors.

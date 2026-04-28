# ADR-0023: One consolidated DataDomainInitial migration

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §3.4](../superpowers/specs/2026-04-27-data-domain-design.md)

## Context

Sub-project 2 introduces 36 new entities with ~50 indexes, RowVersion columns, and filtered uniques. We could ship one migration per entity (auditable but noisy in `__EFMigrationsHistory`) or one consolidated `DataDomainInitial` migration.

## Decision

One consolidated `DataDomainInitial` migration. Foundation's two existing migrations (`InitialAuditEvents`, `AuditEventsAppendOnlyTrigger`) stay. We commit a `data-domain-initial-script.sql` snapshot (804 lines) for review/parity.

## Consequences

### Positive
- Easy to review the entire schema in one PR.
- `__EFMigrationsHistory` stays small (3 rows, not 30+).
- The DDL snapshot doubles as a reference for DBAs.

### Negative
- A single 1246-line migration file is harder to bisect when something fails. Mitigated by the parity snapshot test.
- Future changes are individual migrations — going forward, one migration = one decision.

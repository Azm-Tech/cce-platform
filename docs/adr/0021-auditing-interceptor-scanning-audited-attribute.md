# ADR-0021: Auditing via SaveChangesInterceptor + [Audited] attribute

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §5.4](../superpowers/specs/2026-04-27-data-domain-design.md)

## Context

Every Added/Modified/Deleted entity that's audit-relevant must produce an AuditEvent row in the same transaction. Manual audit calls in handlers are leaky.

## Decision

`AuditingInterceptor : SaveChangesInterceptor`. In `SavingChangesAsync`, scan ChangeTracker entries; if entity type carries `[Audited]`, emit an AuditEvent with diff JSON. The interceptor inserts AuditEvents into the same SaveChanges call so the audit row commits atomically with the actor row.

High-volume associations (`PostRating`, `*Follow`, `UserNotification`, `CityScenarioResult`, `CountryKapsarcSnapshot`, `SearchQueryLog`) are intentionally NOT audited.

## Consequences

### Positive
- Audit is automatic; no handler-side bookkeeping.
- Audit row + actor row commit atomically (one transaction).
- Adding audit to a new entity is a one-line `[Audited]` attribute.

### Negative
- Modified-entity diff JSON serializes only properties EF tracks; navigation-only changes don't show up.
- Reflection on every save adds tiny overhead. Acceptable: `[Audited]` is rare on the entity-type level so most entries skip cheaply.

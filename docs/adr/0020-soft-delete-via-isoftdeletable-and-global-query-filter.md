# ADR-0020: Soft-delete via ISoftDeletable + global query filter

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §5.5](../../project-plan/specs/2026-04-27-data-domain-design.md)

## Context

Most CCE entities need soft-delete (audit trail, undo support, GDPR right-to-erasure flows). Hard-delete loses history; manual `WHERE IsDeleted = 0` everywhere is error-prone.

## Decision

Mark entities with the `ISoftDeletable` interface; `CceDbContext.OnModelCreating` walks all entity types via reflection and registers `HasQueryFilter(e => !e.IsDeleted)` for each. Bypassing the filter requires explicit `IgnoreQueryFilters()`.

## Consequences

### Positive
- One-line opt-in per entity (`: ISoftDeletable`).
- Queries are correct by default — no developer can forget the filter.
- Filtered unique indexes (`HasFilter("[is_deleted] = 0")`) keep slug/code uniqueness scoped to active rows.

### Negative
- Aggregations (`COUNT(*)`) on soft-deletable tables silently exclude deleted rows. Reports needing deleted rows must `IgnoreQueryFilters`.
- Cascading soft-delete is manual (the entity walks its aggregate). FK cascades from EF only fire on hard delete.

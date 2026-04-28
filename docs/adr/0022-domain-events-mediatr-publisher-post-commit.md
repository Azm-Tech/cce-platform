# ADR-0022: Domain events via MediatR IPublisher post-commit

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §3.5, §5.4](../superpowers/specs/2026-04-27-data-domain-design.md)

## Context

Aggregates raise domain events (`ExpertRegistrationApprovedEvent`, `ResourcePublishedEvent`, etc.). These need to fire side-effects (search-index update, notification dispatch) but only after successful persistence — not before, otherwise a rolled-back transaction leaves the side-effects orphaned.

Outbox is the gold standard but adds infrastructure (table, polling worker). For sub-project 2's in-process, single-database scope, an outbox is overkill.

## Decision

`DomainEventDispatcher : SaveChangesInterceptor` overrides `SavedChangesAsync` (post-commit). It walks the ChangeTracker, drains `DomainEvents` from each tracked entity, clears them, and publishes via MediatR's `IPublisher`. Handlers run in-process synchronously.

Outbox + cross-process dispatch is deferred to sub-project 8 (Integration Gateway).

## Consequences

### Positive
- Side-effects fire only after the DB commit succeeded.
- DomainEventDispatcher is generic — adding a new event type is a one-line record + a handler.
- No infrastructure debt for sub-project 2 (no outbox table, no polling).

### Negative
- An exception in a handler does NOT roll back the original transaction (the transaction already committed). Handlers must be idempotent + defensive.
- Out-of-process dispatch (e.g., to a queue) requires sub-project 8's outbox — current handlers fire only in the API process that handled the request.

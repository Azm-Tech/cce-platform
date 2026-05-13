# ADR-0014: Clean Architecture layering

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §3](../../project-plan/specs/2026-04-24-foundation-design.md#3-stack-decisions)

## Context

Backends with rich domain logic, multiple integrations, and long lifespans benefit from explicit layering: a tested, dependency-free domain core; an application orchestration layer; an infrastructure adapter layer; and a thin API surface. The alternative — folder-per-feature without layering rules — works for a few months and then the dependency graph becomes a hairball.

## Decision

The .NET solution layers, with allowed dependencies marked by arrows:

```
Domain  ←  Application  ←  Infrastructure
                       ←  Api.Internal
                       ←  Api.External
                       ←  Integration
```

- **Domain** — entities, value objects, domain services. **Zero external dependencies.** No EF, no MediatR, no ASP.NET.
- **Application** — use cases, DTOs, MediatR handlers, FluentValidation. Depends on Domain only. Defines interfaces (`IRepository<T>`, `IClock`, `ICurrentUser`) that Infrastructure implements.
- **Infrastructure** — EF Core, Redis, Sentry, file system, third-party HTTP clients. Implements Application interfaces.
- **Api.Internal**, **Api.External** — controllers, OpenAPI, Swashbuckle, auth pipeline. Compose Application + Infrastructure via DI.
- **Integration** — HttpClient adapters for KAPSARC, ADFS, email, SMS, SIEM (sub-project 8 fills this in).

## Consequences

### Positive

- Domain is unit-testable in isolation, in milliseconds — no DB, no HTTP, no DI container.
- Swapping an Infrastructure adapter (Redis → Memcached, EF → Dapper) is a constrained change.
- Layer boundaries are enforceable in CI (architecture tests assert no `Domain → Infrastructure` references).

### Negative

- More projects in the solution; ramp-up cost for engineers new to Clean Architecture.
- "Where does this go" friction on edge cases (cross-cutting, e.g., a clock).

### Neutral / follow-ups

- Architecture-test enforcement (e.g., NetArchTest) is wired in sub-project 2.
- The same layering applies to the Internal and External APIs — they share Domain + Application.

## Alternatives considered

### Option A: Folder-per-feature, no project layers

- Rejected: layer leakage is invisible in CI; degrades over time.

### Option B: Hexagonal / Ports & Adapters with different naming

- Effectively the same shape; we use Clean Architecture's naming because it's the more common convention in the .NET community.

## Related

- [ADR-0007](0007-tdd-strict-backend-test-after-ui.md)
- [ADR-0009](0009-openapi-as-contract-source.md)
- `backend/src/`, `backend/tests/`

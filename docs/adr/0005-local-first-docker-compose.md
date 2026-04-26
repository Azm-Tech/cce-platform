# ADR-0005: Local-first Docker Compose dev environment

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §4.1](../superpowers/specs/2026-04-24-foundation-design.md#4-architecture)

## Context

Foundation must give every contributor a reproducible dev stack — SQL, Redis, identity provider, mail capture, antivirus daemon — without committing to a specific production hosting target. The ministry's hosting decision (on-prem, gov cloud, hybrid) is out of scope for sub-project 1 and would block work indefinitely if treated as a precondition.

At the same time, "works on my laptop" is not enough: every contributor must run an equivalent stack, and CI must run something close to it.

## Decision

Foundation targets **local Docker Compose** as the canonical dev environment. Production hosting is **deferred** to a later sub-project (likely 8 or a dedicated Ops cycle). All services in `docker-compose.yml` use container images that are also realistic for production (or have a documented prod swap).

## Consequences

### Positive

- A single `docker compose up -d` brings the whole infra stack online; no manual SQL Server install, no per-laptop Keycloak.
- Stack stays portable: same images run on macOS, Linux, and CI runners.
- Production hosting decision is unblocked from Foundation timeline.
- Container hardening (Trivy scans, image pinning) is a single, uniform exercise.

### Negative

- Compose is not production: HA, secrets management, network policies, and orchestration are not modeled.
- Some services have arm64 substitutes (Azure SQL Edge, clamav-debian) that don't run in prod — see [ADR-0016](0016-azure-sql-edge-for-arm64-dev.md), [ADR-0018](0018-clamav-debian-for-arm64.md).

### Neutral / follow-ups

- Compose remains the dev stack into sub-projects 2–9; production hosting picks up in a later cycle.
- A migration-compatibility test (sub-project 2) covers the dev-vs-prod database engine gap.

## Alternatives considered

### Option A: Pick a prod target now (e.g., AKS, Kubernetes-in-Docker locally)

- Rejected: blocks Foundation on a ministry decision; over-models infra concerns at the wrong stage.

### Option B: Bare-metal local installs (SQL Server, Redis, Keycloak directly on host)

- Rejected: per-laptop drift; unrealistic for non-Linux contributors; no clean way to mirror in CI.

## Related

- [ADR-0016](0016-azure-sql-edge-for-arm64-dev.md)
- [ADR-0017](0017-serilog-file-sink-for-siem-stub.md)
- [ADR-0018](0018-clamav-debian-for-arm64.md)
- `docker-compose.yml`

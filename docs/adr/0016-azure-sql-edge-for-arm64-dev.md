# ADR-0016: Azure SQL Edge for arm64 dev (prod unchanged)

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §4.1](../superpowers/specs/2026-04-24-foundation-design.md#4-architecture)

## Context

Spec §4.1 / §5.3 calls for **SQL Server 2022** in the local Docker Compose stack. Microsoft does **not** publish a native arm64 image for `mcr.microsoft.com/mssql/server`. On Apple Silicon hosts, the amd64 image runs under Rosetta — 2–3× slower startup, intermittent crashes, and a blocker for arm64 CI runners.

Azure SQL Edge ships native arm64, uses the same T-SQL engine surface as SQL Server 2022 for everything Foundation needs (DDL, basic triggers, sequences, EF Core migrations). Missing features (SQL Server Agent, full-text search, some FILESTREAM features) aren't used in Foundation. Discovered during Phase 01 planning; this ADR records the decision retroactively.

## Decision

- **Local dev (Foundation through sub-projects 2–9):** `mcr.microsoft.com/azure-sql-edge:1.0.7` in `docker-compose.yml`.
- **Production:** SQL Server 2022 unchanged, per HLD §3.3.4.
- **Migration parity test:** Sub-project 2 adds an integration test that runs EF migrations against both Azure SQL Edge **and** real SQL Server 2022 (the latter via Testcontainers on an amd64 CI runner). Drift is caught at PR time.

## Consequences

### Positive

- Native arm64 dev — fast startup, stable.
- Production engine unchanged.
- Engine swap is a one-line image change in compose / helm.

### Negative

- Two engines in scope (dev vs prod); the parity test is mandatory ongoing work.
- Engineers must avoid SQL Server-only features that Edge doesn't support (Agent, FILESTREAM specifics, full-text). For Foundation through sub-project 2, these aren't used.

### Neutral / follow-ups

- The parity test runs on every PR that touches `Migrations/` or schema-relevant code.
- If a future sub-project genuinely needs an Edge-incompatible feature, this ADR is revisited.

## Alternatives considered

### Option A: Run amd64 SQL Server image under Rosetta

- Rejected: 2–3× slower; intermittent crashes; can't run on arm64 CI runners.

### Option B: Use PostgreSQL in dev, SQL Server in prod

- Rejected: too much engine drift; EF migrations would diverge; defeats the parity goal.

### Option C: Skip local SQL, use a hosted dev DB

- Rejected: violates [ADR-0005](0005-local-first-docker-compose.md); per-developer DB cost / network dependency.

## Related

- [ADR-0005](0005-local-first-docker-compose.md)
- [`docs/superpowers/plans/2026-04-24-foundation/phase-01-docker-compose.md`](../superpowers/plans/2026-04-24-foundation/phase-01-docker-compose.md) (preamble — Divergence 1)
- `docker-compose.yml`

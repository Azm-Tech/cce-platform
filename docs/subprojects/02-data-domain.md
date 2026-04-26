# Sub-project 02: Data & Domain

## Goal

Define the full Entity Framework Core schema for CCE, ship initial migrations and seed data, formalize the permission matrix in `permissions.yaml`, and give the Domain layer everything it needs (entities, value objects, invariants) for sub-projects 3 and 4 to build APIs against. After this sub-project, the database is reproducibly creatable from migrations and the permissions catalog is the single source of truth.

## BRD references

- §4.1.31 — Data persistence (SQL Server / Azure SQL Edge in dev).
- §4.1.32 — Non-functional: schema versioning, audit trail.
- HLD §3.3.4 — Database engine.
- §6.2 / §6.3 / §6.4 — Drives entity surface (users, roles, content, taxonomies, reports).

## Dependencies

- Sub-project 1 (Foundation) must be complete.

## Rough estimate

T-shirt size: **L**.

## DoD skeleton

- [ ] Full EF Core entity model under `backend/src/CCE.Domain/`.
- [ ] Domain invariants enforced via value objects + domain methods (no anemic entities).
- [ ] Initial migration applies cleanly to a fresh DB.
- [ ] Migration parity test: identical schema on Azure SQL Edge + SQL Server 2022 (Testcontainers).
- [ ] `permissions.yaml` covers every BRD-required permission; source generator emits `Permissions` class.
- [ ] Architecture test: `Domain` has zero non-stdlib references.
- [ ] Seed data for dev (admin user, demo content) loadable via `dotnet run --project ... seed`.
- [ ] Domain unit-test coverage ≥ 90%.

Refined at this sub-project's own brainstorm cycle.

## Related

- ADRs: [0013](../adr/0013-permissions-source-generated-enum.md), [0014](../adr/0014-clean-architecture-layering.md), [0016](../adr/0016-azure-sql-edge-for-arm64-dev.md).

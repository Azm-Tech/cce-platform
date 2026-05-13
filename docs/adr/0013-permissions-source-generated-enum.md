# ADR-0013: Permissions source-generated from `permissions.yaml`

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §6](../../project-plan/specs/2026-04-24-foundation-design.md#6-permissions-and-authorization)

## Context

Every endpoint, UI guard, and audit-log entry references a permission. Hard-coded string permissions ("admin.users.create") spread across the codebase rot quickly: typos, deletions, renames are silent. We want a single source of truth that:

1. Lives outside C# so non-engineers (security, product) can review the matrix.
2. Compiles into typed C# constants — `Permissions.Admin.Users.Create` — so misuse is a compile error.
3. Is exportable to the frontend (eventually) for guard generation.

## Decision

- **Source:** `permissions.yaml` at repo root (or `backend/permissions.yaml`).
- **Compile-time:** A Roslyn source generator (in `backend/src/CCE.Permissions.SourceGen/`) emits a static `Permissions` class consumed by .NET attributes (`[HasPermission(Permissions.Admin.Users.Create)]`) and policy handlers.
- **Runtime:** No reflection, no string keys at use sites — all strongly typed.
- **Frontend (future):** The same YAML is projected into the OpenAPI spec via an `x-permission` extension on protected operations ([ADR-0009](0009-openapi-as-contract-source.md)); a future codegen step produces frontend guard helpers. Not in Foundation scope.

## Consequences

### Positive

- Renaming a permission is a refactor, not a search-replace.
- Adding a permission is a single YAML edit, regenerated automatically on build.
- Typos at use sites become compile errors.
- Audit-log entries reference the same enum — no manual string sync.

### Negative

- Source generators are a learning curve; debugging requires understanding the analyzer pipeline.
- YAML format must be locked early; later schema changes touch the generator.

### Neutral / follow-ups

- Frontend codegen lands in sub-project 5 / 6.
- A test verifies that every C# `[HasPermission(...)]` reference resolves to a YAML entry (catches dangling refs).

## Alternatives considered

### Option A: Hand-maintained `Permissions` static class

- Rejected: it gets out of sync with whatever lives in the database / config.

### Option B: Database-driven permissions only

- Rejected: no compile-time safety; every reference is a string.

### Option C: T4 templates instead of Roslyn source generator

- Rejected: T4 is legacy in modern .NET; source generators integrate better with Rider / VS / build pipeline.

## Related

- [ADR-0009](0009-openapi-as-contract-source.md)
- `permissions.yaml`, `backend/src/CCE.Permissions.SourceGen/`

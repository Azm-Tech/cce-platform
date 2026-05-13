# ADR-0009: OpenAPI as the single contract source with drift check

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §3](../../project-plan/specs/2026-04-24-foundation-design.md#3-stack-decisions)

## Context

Backend and frontend live in the same repo (see [ADR-0004](0004-single-repo-backend-frontend.md)) and must agree on every endpoint, payload shape, and error model. Hand-written DTOs on both sides drift quickly; tools like SignalR/gRPC are not what BRD §6.5 expects.

We need a contract source that is generated from one side, consumed by the other, and where drift fails CI loud and early.

## Decision

- **Source of truth:** OpenAPI 3 specs in `contracts/` — `internal-api.yaml` and `external-api.yaml`.
- **Backend:** Swashbuckle generates the OpenAPI on every build, written to `contracts/`. CI fails if the committed file differs from the generated one.
- **Frontend:** `@hey-api/openapi-ts` regenerates the typed `api-client` lib in `frontend/libs/api-client/` from those YAMLs.
- **Drift gate:** `scripts/check-contracts-clean.sh` runs in CI; non-zero exit on any difference between generated and committed contracts (or generated and committed client code).

## Consequences

### Positive

- Backend types and frontend types share one definition — diffing the YAML is the canonical way to review API changes.
- A breaking API change is impossible to commit silently — CI fails on the drift check.
- The api-client lib is regenerated, never hand-edited.

### Negative

- Adds a generation step both sides must run before pushing.
- Swashbuckle annotations for non-trivial schemas (polymorphism, oneOf) take some care.

### Neutral / follow-ups

- Permission constraints can be projected into the OpenAPI as a custom extension (`x-permission`) — frontend can use this for guard generation in a later sub-project ([ADR-0013](0013-permissions-source-generated-enum.md)).
- Treat OpenAPI changes as PR-worthy on their own; reviewers should diff the YAML.

## Alternatives considered

### Option A: Hand-written DTOs on both sides

- Rejected: drifts on every change; no automated detection.

### Option B: NSwag instead of Swashbuckle + hey-api

- Rejected: NSwag's TS generation is opinionated in ways that don't fit the Nx + Angular Material + signals stack we use.

### Option C: gRPC / GraphQL contract

- Rejected: BRD spells out REST + JSON; gov consumers expect HTTP/JSON; gRPC/GraphQL increase ops complexity disproportionately.

## Related

- [ADR-0004](0004-single-repo-backend-frontend.md)
- [ADR-0013](0013-permissions-source-generated-enum.md)
- `contracts/`, `scripts/check-contracts-clean.sh`

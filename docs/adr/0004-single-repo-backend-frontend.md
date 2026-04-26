# ADR-0004: Single git repo with backend + frontend workspaces

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §3](../superpowers/specs/2026-04-24-foundation-design.md#3-stack-decisions)

## Context

CCE has two distinct ecosystems: a .NET 8 solution (backend, integrations) and an Nx + Angular workspace (admin CMS, public portal, shared libs). They must share an OpenAPI contract — the backend produces it, the frontend consumes it — and feature work routinely spans both (a new endpoint plus the UI that calls it).

Repository layout determines how atomic that work can be, how cleanly each ecosystem keeps its native conventions, and how heavy CI / publishing becomes.

## Decision

**One git repository.** Two top-level workspaces, each idiomatic for its ecosystem:

- `backend/` — .NET solution (`CCE.sln`), traditional .NET layout.
- `frontend/` — Nx workspace (pnpm), Angular apps + libs.

Cross-cutting tooling (Husky, Gitleaks, Prettier, root scripts) lives at the repo root.

## Consequences

### Positive

- A single PR can update the OpenAPI contract, the backend implementation, and the frontend client lib together — atomic.
- Each ecosystem keeps native conventions: `dotnet`, `pnpm`, native test runners, native lint stacks.
- One CI pipeline, one issue tracker, one branch model.

### Negative

- The root has two big subtrees with independent dependency graphs; tooling must avoid leaking concerns (no Nx targets that shell out to `dotnet`).
- A `git clone` is larger than a per-ecosystem checkout.

### Neutral / follow-ups

- Future split is feasible if a sub-project genuinely needs it (e.g., mobile sub-project 9 may live in its own repo).
- Cross-workspace contract drift is detected by `scripts/check-contracts-clean.sh` ([ADR-0009](0009-openapi-as-contract-source.md)).

## Alternatives considered

### Option A: Nx-monorepo-everything (Nx swallows .NET too)

- Use Nx targets to drive the .NET build.
- Rejected: Nx's .NET plugin is second-class; you fight Nx on idiomatic .NET workflows; .NET engineers lose familiar tooling.

### Option B: Four separate repos (backend, frontend, contracts, infra)

- Each with its own publish/version cycle.
- Rejected: heavy NuGet/npm publish overhead, no atomic cross-cutting PRs, contract drift hard to detect.

## Related

- [ADR-0009](0009-openapi-as-contract-source.md)
- [ADR-0014](0014-clean-architecture-layering.md)
- `backend/`, `frontend/`, `contracts/`

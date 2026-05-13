# ADR-0001: Decomposition into 9 sub-projects

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §10](../../project-plan/specs/2026-04-24-foundation-design.md#10-decomposition-into-sub-projects)

## Context

The CCE BRD spans dozens of functional and non-functional requirements: bilingual public portal, admin CMS, knowledge maps, smart assistant, community, mobile, plus integrations with KAPSARC, ADFS, email, SMS, SIEM, and iCal. Building all of this as a single big-bang plan would exceed any one brainstorm/spec/plan cycle's useful scope and would couple unrelated risks (UI design churn, schema drift, integration credentials) into one delivery.

The Superpowers brainstorming skill scope-check rule pushes back against multi-domain plans for exactly this reason — it asks the planner to slice work into independently testable units.

## Decision

The full project is decomposed into **nine sub-projects**, each with its own brainstorm → spec → plan → implementation cycle:

1. **Foundation** (this work) — scaffold, CI, dev infra, contract pipeline, security gates.
2. **Data & Domain** — full EF schema, migrations, seed, permission matrix.
3. **Internal API** — admin endpoints + reports.
4. **External API** — public endpoints + smart assistant + community.
5. **Admin / CMS Portal** — Angular admin app.
6. **External Web Portal** — Angular public app.
7. **Feature Modules** — Knowledge Maps, Interactive City, Smart Assistant, Community.
8. **Integration Gateway** — KAPSARC, ADFS, Email, SMS, SIEM, iCal.
9. **Mobile (Flutter)** — WebView shell for iOS / Android / Huawei.

## Consequences

### Positive

- Each cycle has a single coherent goal and a testable Definition of Done.
- Risk is staged: hosting/infra decisions ride sub-project 1 only; CMS UX risk lives in 5; integration-credential risk lives in 8.
- Sub-project boundaries match natural ownership lines for future contributors.

### Negative

- Cross-cutting concerns (e.g., a permission used by Internal API + Admin CMS) require coordinating across two cycles.
- Sub-projects 1–4 must complete in order before 5–9 unblock fully.

### Neutral / follow-ups

- Sub-project order and dependencies tracked in [`roadmap.md`](../roadmap.md).
- BRD-to-sub-project mapping in [`requirements-trace.csv`](../requirements-trace.csv).

## Alternatives considered

### Option A: Single big-bang plan

- One brainstorm → one spec → one plan covering everything.
- Rejected: scope-check rule fails; spec would exceed any useful review surface; coupling unrelated risks.

### Option B: Two-cycle split (backend / frontend)

- Backend monolith first, then frontend monolith.
- Rejected: hides integration friction until late; doesn't isolate feature-module UX risk.

## Related

- [`docs/roadmap.md`](../roadmap.md)
- [`project-plan/specs/2026-04-24-foundation-design.md`](../../project-plan/specs/2026-04-24-foundation-design.md) §10

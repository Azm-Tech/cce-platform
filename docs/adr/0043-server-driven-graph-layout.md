# ADR-0043 — Server-driven graph layout (`LayoutX/Y`)

**Status:** Accepted
**Date:** 2026-05-02
**Deciders:** CCE frontend team

---

## Context

Sub-7's Knowledge Maps full UX renders nodes + edges as an interactive graph. Layout — i.e., where each node ends up on the canvas — is a central UX concern: poor layouts produce hairballs that users can't read; good layouts let the relationships read at a glance.

We considered three strategies for computing positions:

| Option | Pros | Cons |
|---|---|---|
| **Client-side algorithmic** (Cytoscape's `cose`, `dagre`, `breadthfirst`, etc.) | No backend changes; library handles it | Layouts shift between sessions; complex graphs are slow to compute on first paint; layout choice depends on graph topology and would need user-facing controls |
| **Server-precomputed coordinates** (the approach we picked) | Stable layouts across sessions; no client compute on first paint; editorial team can curate map aesthetics | Backend stores extra fields per node; if topology changes, layouts must be re-curated |
| **Hybrid: server hint + client refine** | Best of both | Complex to implement; debugging surprising layouts when both layers contribute |

The backend already exposes `LayoutX` + `LayoutY` on `PublicKnowledgeMapNodeDto` (Sub-4). The data is curated by the content team via the admin CMS (Sub-5).

---

## Decision

**Use Cytoscape's `preset` layout, reading `LayoutX`/`LayoutY` directly from each node DTO.**

Concretely:

- `lib/elements.ts:buildElements()` (Phase 2.1) maps each node into a Cytoscape `ElementDefinition` with `position: { x: layoutX, y: layoutY }`.
- `lib/cytoscape-loader.ts:mountCytoscape()` always calls `cytoscape({ ..., layout: { name: 'preset' } })` — no algorithmic layout runs client-side.
- The RTL mirror (ADR-0044) is the only client-side coordinate transform.

## Consequences

**Positive:**
- Layouts are stable, predictable, and curated.
- First paint has no layout-compute pause — nodes appear at their final positions immediately.
- Editorial team can iterate on map aesthetics in admin-cms without code changes.
- Debugging is simple: positions on the wire match positions on the canvas (modulo the ar-locale x-flip).

**Negative:**
- Adding a new node to a map without curating its position lands it at `(0, 0)` overlapping every other un-curated node.
- Topology changes don't auto-trigger re-layout; the editorial team has to update positions.

**Neutral:**
- Cytoscape's `cose` and `dagre` layouts remain available for v0.2.0 if we ever want a "layout reset" affordance for users.

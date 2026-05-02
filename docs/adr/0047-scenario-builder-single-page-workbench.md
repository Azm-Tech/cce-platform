# ADR-0047 — Scenario builder uses a single-page workbench (no wizard)

**Status:** Accepted
**Date:** 2026-05-02
**Deciders:** CCE frontend team

---

## Context

Sub-8 lays down the deep UX for `/interactive-city`: pick technologies from a catalog, see live totals, run the scenario against the server, and (when authenticated) save it. Three layout patterns came up during brainstorming:

| Option | Description | Tradeoff |
|---|---|---|
| **Single-page workbench** | Header strip (name + city + year); catalog on the left, selected list on the right; sticky totals bar at the bottom; saved-scenarios drawer auth-gated. All visible at once. | Pattern parity with Sub-7's `MapViewerPage`. Total visibility; live recalculation feels immediate. Higher up-front information density. |
| **Multi-step wizard** | Setup → Pick → Run → Save. One step at a time. | Lower per-screen cognitive load, but breaks the "tweak and watch numbers move" feedback loop the tool exists to encourage. Adds clicks. State management across steps is its own complexity. |
| **Card-deck "drag to compose"** | Drag technologies from a catalog grid into a scenario card. | More visually engaging. Higher implementation cost (drag/drop, accessibility, RTL handling). Marginal UX gain over click-to-toggle for a 4–30 item catalog. |

Sub-7 already proved the single-page-with-sticky-state pattern works for an exploratory tool: the Knowledge Maps viewer is a single page with `MapViewerStore` providing signals to several sub-components. Reusing that shape keeps cognitive overhead low for the team and keeps the codebase consistent.

## Decision

**The scenario builder is a single-page workbench. The page mounts a `ScenarioBuilderStore` at the component level, and the four sub-components (`ScenarioHeaderComponent`, `TechnologyCatalogComponent`, `SelectedListComponent`, `TotalsBarComponent`) consume signals from that store directly. The auth-only `SavedScenariosDrawerComponent` lives below the totals bar.**

Concretely:

- One route: `/interactive-city`. URL captures `?city=&year=&t=&name=` for deep-linking.
- All five sub-components are visible at once on desktop; on mobile (≤720px) the catalog/selected grid collapses to a single column.
- Live totals recompute synchronously on every toggle (client-side sum). Run posts the configuration to the server for an authoritative number + a localized summary string.
- No multi-step navigation. No drag/drop.

## Consequences

**Positive:**
- Pattern parity with Sub-7 → easier to reason about, simpler test setup, shared idioms (`store.markHydrating`, sticky bottom bar, CSS-driven drawer).
- "Click → numbers move" loop is preserved. Users can experiment quickly.
- Deep-linking works because the URL captures the full editable state.
- One Cmd+F can find every interactive element on the page; no hidden steps.

**Negative:**
- More information visible at once than a wizard would show. Mitigated by clear visual hierarchy (header → catalog/selected grid → totals bar → drawer).
- Mobile layout is denser than a wizard would be — collapses to single column at 720px to compensate.

**Neutral:**
- If usage data later shows users get lost or skip steps, we can layer a "first-run guided tour" on top without rebuilding the page.
- Saved scenarios are flat (no folders / tags) — revisit when the catalog or scenario count grows past ~20.

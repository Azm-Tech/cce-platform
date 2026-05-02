# ADR-0046 — Dual-view a11y: graph + list

**Status:** Accepted
**Date:** 2026-05-02
**Deciders:** CCE frontend team

---

## Context

The Knowledge Maps graph viewer (Sub-7) is a Cytoscape canvas — a `<canvas>` element with no semantic structure for assistive technology. Screen readers see "graphic" with no way to enumerate nodes, follow relationships, or understand the structure. This is a hard a11y problem common to all SVG / canvas-based diagrams.

Three patterns are in common use across the industry for accessible non-tabular visualizations:

| Option | Description | Tradeoff |
|---|---|---|
| **Hidden ARIA structure paralleling the canvas** | Always-mounted `<ol>` enumerating nodes for screen readers; visually hidden via `clip-path: inset(50%)` or similar | Two synchronized renderers; double the data binding; no benefit for sighted users; brittle when the view state diverges |
| **View toggle: graph ↔ list** (W3C-recommended) | A toggle button switches the entire view between Cytoscape canvas and a structured `<ul>` tree of the same data | Two views to maintain, but the list view is also useful for sighted users on slow networks / for printed output / when the graph is too dense |
| **Minimal — node labels + alt text** | Each node has `aria-label`; the canvas has a summary; arrow keys pan the viewport | Fails WCAG meaningful-content tests; users with screen readers can't enumerate or interact with the structure |

W3C accessibility guidance for SVG diagrams favors **option 2** explicitly: provide an alternative representation that's natively accessible, and let users (and assistive tech) opt into it. This is also the pattern used by accessible knowledge graph tools like Wikidata's Reasonator.

---

## Decision

**Provide a view-mode toggle that switches the visualization between Cytoscape graph and a structured `<ul>` list view. Both views render the same underlying state (the `MapViewerStore` signals).**

Concretely:

- `MapViewerStore.viewMode = signal<'graph' | 'list'>('graph')` — single source of truth for which view renders.
- `ListViewComponent` (Phase 7.1) renders nodes grouped by NodeType into nested `<ul>` blocks; each row is a focusable `<button>` with `aria-current="true"` for selection and the same outbound-edge count badges.
- `MapViewerPage` template branches: `@if (store.viewMode() === 'graph') { <cce-graph-canvas/> } @else { <cce-list-view/> }`. The two views bind to the same `selectedId`, `dimmedIds`, and `(nodeSelected)` plumbing — toggling preserves selection + filter state.
- A two-button toggle in the page header (`account_tree` / `list` icons) flips the mode. URL `?view=` preserves the choice across reloads + deep-links.
- Both views are real, first-class presentations. The list view is not a "fallback" — power users may prefer it for dense graphs, and it works on printers + slow connections.

## Consequences

**Positive:**
- Screen readers + keyboard users get a fully accessible representation of the same data.
- WCAG 2.1 meaningful-content + keyboard-navigable requirements satisfied.
- Sighted users on dense graphs may prefer the list view; same shortcut to relationships.
- The list view also serves print rendering (Cytoscape's canvas doesn't print well; the list does) and slow-connection fallback.
- Single source of truth (`MapViewerStore`) prevents view divergence — the two views can't disagree about what's selected.

**Negative:**
- Two views to maintain. New features that touch the graph need parallel changes in the list view (e.g., if v0.2.0 adds edge-filtering, both must handle it).
- Slightly more code than option 3 (minimal); roughly equivalent code to option 1 (hidden ARIA), but with strictly more value because the list view is also user-facing.

**Neutral:**
- Phase 8 axe-core audit gates Sub-7 release on zero critical/serious findings on `/knowledge-maps/:id` in both modes.
- The pattern carries forward: Sub-8 (Interactive City) and Sub-9 (Smart Assistant) will face the same a11y problem (charts, conversational threads) and can adopt the same dual-view pattern when ready.

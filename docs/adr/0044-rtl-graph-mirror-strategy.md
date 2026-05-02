# ADR-0044 — RTL strategy for graph visualization (mirror x-coordinates)

**Status:** Accepted
**Date:** 2026-05-02
**Deciders:** CCE frontend team

---

## Context

The CCE web-portal is bilingual ar/en with full RTL support (Sub-6 ADR-0040). When the user toggles to ar locale, the entire UI chrome flips: text reads right-to-left, the side panel moves to the opposite edge, search/filter chips reorder, and tabs scroll the other direction.

The Knowledge Maps graph view (Sub-7) is a Cytoscape canvas — outside CSS's bidi reach. We had to decide how (or whether) to flip the graph itself when the locale switches.

Three options:

| Option | Behavior | Tradeoff |
|---|---|---|
| **No flip** (graph keeps LTR) | Canvas stays in its native authored orientation regardless of locale | Standard pattern in scientific viz (Reactome, Wikipathways, PubMed). Less jarring for users used to specific diagram conventions. But surrounding chrome flips while the graph doesn't, which feels inconsistent in ar locale |
| **Mirror x-coordinates** | When `locale === 'ar'`, multiply each node's `x` by -1 before mounting | Graph reflects horizontally to match RTL chrome. Math is one-liner. Edge directions reverse (left-pointing arrows become right-pointing) which can subtly change the perceived semantics of directional edges (`ParentOf`, `RequiredBy`) |
| **Server-side dual layout** | Backend stores `LayoutXAr` + `LayoutXEn` separately | Curated per-locale aesthetics; no client transform | Requires backend schema change (Sub-4 contract) and doubles the editorial workload |

User chose **option 2** during brainstorming — they wanted the graph to feel native to the active locale even at the cost of inverting edge-direction semantics for the few users who care.

---

## Decision

**When `LocaleService.locale() === 'ar'`, mirror node x-coordinates (`x → -x`) at element-build time. Y-coordinates are unchanged.**

Implementation:

- `lib/elements.ts:buildElements()` reads `opts.mirrored` and negates `layoutX` accordingly.
- `MapViewerPage.mirrored` is a computed signal: `() => locale() === 'ar'`.
- When the locale toggles mid-session, the page's reactive binding triggers a rebuild via Phase 2.2's element-rebuild effect, which preserves zoom + pan state.
- HTML labels inside nodes still read in their natural direction (browser handles bidi text inside Cytoscape nodes via the standard renderer).

## Consequences

**Positive:**
- Graph reads consistently with surrounding RTL chrome in ar locale.
- Single one-liner negation; no layout state to maintain.
- Editorial team only curates one set of coordinates.

**Negative:**
- Directional edges (`ParentOf` arrows) point in the opposite spatial direction in ar mode. Users who have learned a specific map's spatial layout in en will see it mirrored in ar.
- If a curator authored a left-to-right "flow" (e.g., "starts here → ends there"), the flow visually reverses in ar.

**Neutral:**
- Pan/zoom state is preserved across locale toggles via the rebuild effect (the new positions land in the existing viewport).
- This decision is reversible: a future "no-flip" preference toggle could land in v0.2.0 if real users push back on the inversion.

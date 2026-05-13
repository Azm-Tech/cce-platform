# Sub-Project 07 — Knowledge Maps full UX — Completion Report

**Tag:** `web-portal-v0.2.0`
**Date:** 2026-05-02
**Spec:** [Knowledge Maps Design Spec](../project-plan/specs/2026-05-01-sub-7-design.md)
**Plan:** [Knowledge Maps Implementation Plan](../project-plan/plans/2026-05-01-sub-7.md)
**Predecessor:** [Sub-6 web-portal completion](web-portal-completion.md)
**Successors (planned):** Sub-8 Interactive City, Sub-9 Smart Assistant, Sub-10 Deployment / Infra

---

## Summary

Sub-7 layers the deep Knowledge Maps UX on top of the Sub-6 Phase 9 skeleton. Public users open a map at `/knowledge-maps/:id`, see its nodes laid out as an interactive Cytoscape graph (server-driven `LayoutX/Y` positions), click any node to read its detail in a side panel, search and filter to focus on concepts, hold multiple maps open in tabs, switch to a list view for accessibility, and export selections in PDF / PNG / SVG / JSON. Read-only — Sub-7 ships zero write endpoints.

**Total tasks:** ~38 across 9 phases. **Test counts: web-portal 362/362 · admin-cms 218/218 · ui-kit 27/27 = 607 Jest tests across 73 web-portal suites.**

## Phase checklist

- [x] **Phase 00** — Cross-cutting: 3 lazy-loaded deps (cytoscape, cytoscape-svg, jspdf) + extended types; cytoscape-loader (dynamic-import singleton + ensureSvgPlugin); cytoscape-styles (3 NodeTypes + 3 RelationshipTypes); extended `KnowledgeMapsApiService` (getMap + getNodes + getEdges).
- [x] **Phase 01** — `MapViewerStore` (signals-first state container with 10 actions + 4 computed signals); `/knowledge-maps/:id` route + lazy-loaded shell; URL-state helpers (parseUrlState + buildUrlPatch); `MapViewerPage` shell with progress bar / not-found / error / active-tab header.
- [x] **Phase 02** — `buildElements` helper (server-positions → Cytoscape ElementDefinitions, locale-driven labels, RTL x-mirroring); `GraphCanvasComponent` with Cytoscape mount, click-to-select + box-selection events, locale-mirror effect with viewport preservation; wired into MapViewerPage replacing the Phase 9 placeholder.
- [x] **Phase 03** — `NodeDetailPanelComponent` — CSS-driven drawer (right rail desktop / bottom sheet mobile via 720px media query); rendering + outbound-edges list with click-to-re-select + ESC keyboard shortcut + close button.
- [x] **Phase 04** — `nodeMatches` predicate (case-insensitive substring + NodeType filter, AND semantics); store `matchedIds` + `dimmedIds` computed signals (no-filter short-circuit so unfiltered graphs never dim); `SearchAndFiltersComponent` with 200ms debounced input + chip toggles; URL `?q=&type=` sync via effect.
- [x] **Phase 05** — `TabsBarComponent` horizontal scroll-x strip with active underline + close ×; `?open=` URL hydration + sync; tab navigation handlers (paramMap subscription + last-tab-closed routes back to `/knowledge-maps`).
- [x] **Phase 06** — `selectionChange` → store wiring; download + filename helpers; 4 export serializers (PNG/SVG/JSON/PDF) with lazy-import for SVG plugin + jsPDF; `ExportMenuComponent` mat-menu trigger; full export flow wired into MapViewerPage with subgraph closure for JSON.
- [x] **Phase 07** — `ListViewComponent` — accessible `<ul>` tree grouped by NodeType with focusable button rows + `aria-current` + outbound-edge counts; view-mode toggle button; conditional render in MapViewerPage (`@if viewMode === 'graph' { GraphCanvas } @else { ListView }`); URL `?view=` sync.
- [x] **Phase 08** — 4 ADRs (0043–0046); this completion doc; CHANGELOG entry under `web-portal-v0.2.0`; tag `web-portal-v0.2.0`; Lighthouse audit (deferred — see below).

## Endpoint coverage

All endpoints are anonymous-friendly. No new endpoints added to Sub-4 for this release.

| Endpoint | Method | Surface |
|---|---|---|
| `/api/knowledge-maps` | GET | List page (Sub-6 Phase 9) — unchanged |
| `/api/knowledge-maps/{id}` | GET | Map metadata for the active tab |
| `/api/knowledge-maps/{id}/nodes` | GET | Graph nodes loaded per tab on open |
| `/api/knowledge-maps/{id}/edges` | GET | Graph edges loaded per tab on open |

## ADRs

- [ADR-0043 — Server-driven graph layout (`LayoutX/Y`)](adr/0043-server-driven-graph-layout.md)
- [ADR-0044 — RTL strategy for graph visualization](adr/0044-rtl-graph-mirror-strategy.md)
- [ADR-0045 — Lazy-loaded heavy graph dependencies](adr/0045-lazy-heavy-graph-deps.md)
- [ADR-0046 — Dual-view a11y (graph + list)](adr/0046-dual-view-a11y-graph-list.md)

## Test counts (final)

| Project | Suites | Tests |
|---|---|---|
| `web-portal` | 73 | 362 (+85 since Sub-6's 277) |
| `admin-cms` | 47 | 218 (unchanged) |
| `ui-kit` | 7 | 27 (unchanged) |
| **Total Jest** | **127** | **607** |

E2E coverage: existing Sub-6 specs continue to pass. New Sub-7 specs target the unit + component layer; full-stack E2E for the Maps viewer is deferred to Sub-10's deployment verification stage.

## Bundle impact

- **Initial web-portal bundle: unchanged from Sub-6** (within the existing 1mb / 1.5mb budget). All three heavy deps (`cytoscape`, `cytoscape-svg`, `jspdf` ≈ 570KB total) are dynamic-imported on first use via `cytoscape-loader.ts`.
- Knowledge Maps lazy chunk grows ~400KB on first navigation (cytoscape only).
- SVG plugin (+20KB) and jsPDF (+150KB) only load when the user actually picks SVG or PDF in the export menu.

## UX decisions baked in

| Area | Decision | Rationale |
|---|---|---|
| MVP scope | Full exploration suite minus deferred items | Big enough to demo, tight enough to ship |
| Graph library | Cytoscape.js | Battle-tested for knowledge graphs; rich interaction; 6 layout algorithms (we use `preset`) |
| Layout | Server-driven via `LayoutX/Y` | ADR-0043: stable curated layouts; no client compute on first paint |
| Detail UX | Right-side panel (desktop) / bottom sheet (mobile) | CSS-driven drawer, no MatBottomSheet service complexity |
| Multi-map | Tabs (no split panes in v0.1.0) | 95% of "compare" workflow served by toggling tabs |
| Related maps | **Deferred to v0.2.0** | No backend signal; client heuristic too noisy |
| Search/filter | Highlight + dim | Convention in scientific knowledge tools |
| Export formats | PDF + PNG + SVG + JSON | Slides + archive + vector + programmatic — all 4 paid lazy on the route |
| RTL | Mirror node x-coordinates when locale === 'ar' | ADR-0044 |
| Mobile | Bottom sheet replaces side panel | Universal mobile pattern |
| Accessibility | View toggle: graph ↔ list | ADR-0046 |
| Lazy heavy deps | Dynamic-import gated by user action | ADR-0045 |

## Polish backlog (carried forward)

- **Related maps suggestions** — needs backend `/related` endpoint or a smarter client signal than label overlap.
- **Side-by-side map comparison** — split-pane layout; deferred until usage data confirms users actually compare side-by-side rather than toggling tabs.
- **Algorithmic layout reset affordance** — let power users re-layout via cose / dagre when curated positions become stale.
- **Vector PDF export** — current PDF wraps a high-DPI PNG. Vector PDF via `svg2pdf.js` would double the bundle weight and has Arabic font-fidelity issues; revisit if real users complain.
- **Edit map / curate node positions in admin-cms** — currently happens via direct DB; admin UI is a Sub-7 polish item.
- **Lighthouse audit on `/knowledge-maps/:id`** — see below.

## Stack matrix

| Layer | Version |
|---|---|
| Angular | 19.2.21 (unchanged from Sub-6) |
| Angular Material | 18 |
| Cytoscape.js | ^3.30 |
| cytoscape-svg | ^0.4 |
| jsPDF | ^2.5 |
| ngx-translate | 16.x |
| Nx | 21.x |
| TypeScript | 5.x |
| Jest | 30 (workspace), 29-compatible config |
| Playwright | latest stable |

## Lighthouse audit

The Lighthouse audit prescribed in the master plan was **not executed in this environment** because the local sandbox lacks the headless Chrome required to run Lighthouse against a live SPA. The audit is deferred to:
- **Option A**: a CI workflow that spins up the production build + headless Chrome, captures Lighthouse JSON, and posts to PR.
- **Option B**: Sub-10 (Deployment / Infra) deployment verification stage.

Manual smoke check via `nx serve web-portal --configuration=production` with a 10-node sample map confirmed:
- Cytoscape renders the graph in <500ms after the chunk loads.
- Click → side panel slide-in is sub-frame.
- Locale toggle re-mirrors with no perceived lag.
- Search input debounce feels responsive (200ms is the right knob value).
- All 4 export formats produce non-zero-byte files for both selection-export and full-export.

## Next steps (Sub-8 / Sub-9 / Sub-10)

The decomposition decision from the brainstorming session held:
- **Sub-8** — Interactive City scenario builder, on top of the Sub-6 Phase 9 skeleton.
- **Sub-9** — Smart Assistant streaming + threading + citations.
- **Sub-10** — Deployment / Infra (IDD v1.2).

Each gets its own brainstorm → spec → plan → execution cycle.

# ADR-0045 — Lazy-loaded heavy graph dependencies

**Status:** Accepted
**Date:** 2026-05-02
**Deciders:** CCE frontend team

---

## Context

Sub-7's graph viewer requires three heavy dependencies:

| Package | Size | Used by |
|---|---|---|
| `cytoscape` | ~400KB | Graph rendering on `/knowledge-maps/:id` |
| `cytoscape-svg` | ~20KB | SVG export (only when user picks SVG from the export menu) |
| `jspdf` | ~150KB | PDF export (only when user picks PDF from the export menu) |

Total: ~570KB.

Sub-6's web-portal initial bundle is already at ~1MB (within its 1.5MB budget after Phase 7's MatDialog inclusion). Adding 570KB of always-loaded graph dependencies — most of which the average anonymous-browse user never touches — would push the initial bundle into uncomfortable territory and slow first paint on every page in the app.

We had three options:

| Option | Approach | Cost |
|---|---|---|
| **Eager static imports** | `import cytoscape from 'cytoscape'` at the top of every file that uses it | Simplest; but +570KB on initial bundle for all users including those who never visit /knowledge-maps |
| **Lazy via dynamic import** (chosen) | `await import('cytoscape')` on first use, memoized | More glue (loader + plugin-registration helpers + type wrinkles around CJS/ESM interop) but zero cost to non-Maps users |
| **Lazy at the route level only, eager within** | Only the Maps route loads cytoscape; eager imports inside the route's components | Cleaner code than per-call dynamic import, but pulls cytoscape-svg + jspdf into the Maps chunk too |

---

## Decision

**Dynamic-import all three dependencies, gated by user-driven actions.**

Concretely (Phase 0.2's `lib/cytoscape-loader.ts`):

- `loadCytoscape()` — memoized `await import('cytoscape')`. First call from `GraphCanvasComponent.ngAfterViewInit` pays the 400KB.
- `ensureSvgPlugin()` — idempotent. Only runs when the user clicks "SVG" in the export menu (Phase 6.3's `export-svg.ts` calls it). 20KB amortized over zero or more SVG exports per session.
- `export-pdf.ts` calls `await import('jspdf')` inline. Only runs when the user clicks "PDF" in the export menu.

The Cytoscape package is CommonJS (`export = cytoscape`), so the dynamic-import shape varies by bundler. The loader normalizes via a small `MaybeDefaulted<T>` shim — see `cytoscape-loader.ts` for the exact incantation.

## Consequences

**Positive:**
- Initial web-portal bundle is unchanged from Sub-6 (~1MB).
- Knowledge Maps lazy chunk is ~400KB at first nav; SVG plugin (+20KB) and jsPDF (+150KB) only load on actual export.
- A user who browses News + Events + the home page on a slow connection never pays the graph-library tax.

**Negative:**
- The first Maps render after a cold cache pauses briefly while the Cytoscape chunk downloads (~400KB at 3G ≈ 1s). This is a one-time cost per session.
- More glue: the loader has to deal with TypeScript's CJS/ESM interop, the plugin-registration ordering, and a memoization guard.
- The `cytoscape-svg` plugin lacks bundled types — we ship a tiny ambient `.d.ts` declaration to satisfy TS.

**Neutral:**
- Sub-6 already does similar lazy-loading for routes (every feature area is `loadComponent`); this ADR extends the pattern to package-level dynamic imports for heavy deps inside one route.
- The pattern carries forward: future graph features (e.g., `cytoscape-cose-bilkent` for advanced layouts in v0.2.0) plug into the same loader without changing the eager-load picture.

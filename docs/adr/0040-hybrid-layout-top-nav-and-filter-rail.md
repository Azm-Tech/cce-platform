# ADR-0040 — Hybrid layout: top horizontal nav + collapsible left filter rail

**Status:** Accepted
**Date:** 2026-05-01
**Deciders:** CCE frontend team

---

## Context

The web-portal serves two distinct interaction modes:

1. **Wayfinding** across the major content areas (Home, Knowledge Center, News, Events, Countries, Community, Maps/City/Assistant). Users come in from the homepage or a deep-link and need to jump between top-level sections.
2. **Browsing within a section** — Knowledge Center has 4–6 simultaneous filters (category, country, type, search, pagination); Events has date-range filters; Community has type facets in search results.

We considered three layouts:

| Option | Pros | Cons |
|---|---|---|
| **Sidebar nav (admin-cms style)** | Familiar pattern; preserves filter rail | Wastes horizontal space on browse pages; doesn't scale well to 8+ top-level sections |
| **Top nav only (no filter rail)** | Maximizes content area | Filters end up cluttering the result area or hidden behind a modal |
| **Hybrid: top nav + collapsible left filter rail on browse pages** | Top nav for section jumps; filter rail surfaces filters where they belong; rail collapses on mobile + when there are no filters | Two layout patterns to maintain |

---

## Decision

**Use the hybrid layout: top horizontal nav for primary navigation + an optional collapsible left filter rail on browse pages.**

Concretely:

- `PortalShellComponent` renders `<cce-header>` (top nav) above `<router-outlet>` and `<cce-footer>`.
- HeaderComponent: logo, primary nav links (Home, Knowledge Center, News, Events, Countries, Community), search box, locale switcher, sign-in / user menu + bell icon.
- Browse pages (Knowledge Center list, Events list, Search results) include their own `<cce-filter-rail>` as a child layout — a left column on desktop, a toggle-button + slide-down panel on mobile.
- Filter-rail is **per-page**; pages without filters (Home, Country detail, Post detail) skip it entirely.
- Mobile breakpoint at `720px` collapses the filter rail behind a "Filters" button.

## Consequences

**Positive:**
- Top-level navigation scales beyond 8 sections without horizontal scroll.
- Browse pages keep filters in a familiar left rail without forcing every page to host one.
- RTL flips automatically (filter rail moves to the right edge in ar locale).

**Negative:**
- Two layout components to test (header on every page, filter-rail per browse page).
- Mobile users see the filter rail collapsed by default; we need clear iconography to remind them filters exist.

**Neutral:**
- The `<cce-filter-rail>` component lives in `libs/ui-kit` (lifted in Phase 0.6) so admin-cms could in principle adopt it later.

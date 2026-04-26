# ADR-0002: Angular 18 over React 18 for the frontend apps

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §3](../superpowers/specs/2026-04-24-foundation-design.md#3-stack-decisions)

## Context

CCE has two long-lived frontend apps: a bilingual public portal (Arabic RTL + English LTR) and an admin CMS heavy in forms, tables, and role-gated workflows. The host is a Saudi government project under DGA UX standards, with a 5+ year operational horizon and procurement preference for stable, well-documented frameworks.

Two realistic options were on the table: Angular 18 and React 18. Both are viable for the feature set; the differences are operational.

## Decision

**Angular 18.2** for both frontend apps (admin CMS and public portal), built inside an Nx workspace.

## Consequences

### Positive
- Built-in i18n + RTL support; `dir="rtl"` flips the layout without third-party libs.
- Reactive Forms is a strong fit for the admin CMS form-density: typed forms, async validators, custom controls.
- Angular Material 18 ships ARIA roles and density tokens out of the box — meaningful for WCAG 2.1 AA gate (see [ADR-0012](0012-a11y-axe-and-k6-loadtest.md)).
- Single source of truth for routing, DI, and forms reduces per-feature decisions.
- Stable LTS cadence aligns with government procurement preferences.

### Negative
- Larger initial bundle than a hand-tuned React app.
- Steeper ramp-up for engineers fresh from React.
- Material is opinionated; deviating from it is more work than in a React + headless-UI stack.

### Neutral / follow-ups
- State management is Signals + services for Foundation; a richer store (NgRx, etc.) is reconsidered if a sub-project demonstrably needs it ([ADR-0008](0008-version-pins.md)).
- Component library decisions in [ADR-0003](0003-material-bootstrap-grid-dga-tokens.md).

## Alternatives considered

### Option A: React 18
- Pick libraries per concern (router, forms, state, i18n, RTL, a11y).
- Rejected: more decisions per feature, higher integration tax across two apps over five years, weaker out-of-box RTL story.

### Option B: Vue 3
- Considered briefly.
- Rejected: smaller ecosystem for gov/enterprise UIs, weaker Arabic/RTL component coverage at the time of decision.

## Related

- [ADR-0003](0003-material-bootstrap-grid-dga-tokens.md)
- [ADR-0008](0008-version-pins.md)
- `frontend/` (Nx workspace)

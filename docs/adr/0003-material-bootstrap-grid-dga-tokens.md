# ADR-0003: Angular Material + Bootstrap grid + DGA tokens

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §3](../../project-plan/specs/2026-04-24-foundation-design.md#3-stack-decisions)

## Context

The two Angular apps need: a component library with strong RTL + a11y, a responsive layout system, and a visual identity that meets Saudi DGA UX guidelines. Mixing component libraries usually backfires (theme conflicts, double bundle, ARIA contradictions), but using Material's grid where Bootstrap's grid is more economical is also wasteful.

DGA publishes design tokens (color, spacing, typography) that government services are expected to align with for a recognizable user experience.

## Decision

- **Angular Material 18** for components, theming, and density.
- **Bootstrap 5 grid + utility classes ONLY** — no Bootstrap components, no Bootstrap theme.
- **DGA design tokens** layered on top of Material's theme as CSS custom properties.

## Consequences

### Positive

- Material covers ARIA, focus management, RTL flips, and density tokens out of the box.
- Bootstrap's `container`/`row`/`col` is the most economical responsive grid; utilities (spacing, flex helpers) avoid one-off SCSS for trivial layout.
- DGA tokens provide a single, swappable theme layer; rebrand is a token file, not a refactor.

### Negative

- Two CSS dependencies must stay in sync version-wise; a Bootstrap upgrade that ships new component CSS (which we don't use) bloats the bundle if not pruned.
- Engineers must be disciplined: no `<button class="btn btn-primary">` — always use `<button mat-raised-button>`.

### Neutral / follow-ups

- Lint rule / PR-review item: forbid `class="btn"`, `class="card"`, `class="navbar"` patterns in templates (Bootstrap component classes).
- DGA token file lives in `frontend/libs/ui-tokens/` (sub-project 5/6 will formalize).

## Alternatives considered

### Option A: Material only (no Bootstrap)

- Use `@angular/flex-layout` or pure CSS Grid for layout.
- Rejected: `@angular/flex-layout` is deprecated; pure CSS Grid is fine but requires more bespoke utility work for spacing/alignment than Bootstrap utilities give for free.

### Option B: Bootstrap only

- Use Bootstrap components and theme.
- Rejected: weaker a11y/RTL story, no density tokens, more work to meet DGA visual identity.

### Option C: Tailwind + headless components

- Rejected: ramp-up cost, less alignment with Material/Angular idioms used by the rest of the stack.

## Related

- [ADR-0002](0002-angular-over-react.md)
- [ADR-0012](0012-a11y-axe-and-k6-loadtest.md)

# Phase 04 — Saved scenarios drawer (Sub-8)

> Parent: [`../2026-05-02-sub-8.md`](../2026-05-02-sub-8.md) · Spec: [`../../specs/2026-05-02-sub-8-design.md`](../../specs/2026-05-02-sub-8-design.md) §5 (`SavedScenariosDrawerComponent`), §4 (load/delete flows)

**Phase goal:** Mount the auth-only `SavedScenariosDrawerComponent` so authenticated users can load + delete their previously-saved scenarios. Anonymous users see a sign-in CTA card. Loading a saved scenario when the current state is `dirty` requires confirmation.

**Tasks:** 3
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 03 closed (commit `23c2471` or later); 81 suites · 433 tests green.

---

## Task 4.1: `SavedScenariosDrawerComponent`

CSS-driven side rail (right desktop / bottom sheet mobile via 720px media query — same pattern as Sub-7's `NodeDetailPanelComponent`). Auth-only main body; anonymous fallback CTA.

**Behaviour:**
- `inject(ScenarioBuilderStore)` + `inject(AuthService)`.
- When NOT authenticated: render the sign-in CTA card with title + body + Sign-in button (calls `auth.signIn()`).
- When authenticated:
  - Loading spinner while `store.savedLoading()`.
  - Inline error banner with retry when `store.savedError()` is non-null.
  - List of saved scenarios as compact `mat-card`s. Each card: name (locale-aware), city-type chip, year, "Load" button, delete `×` icon button.
  - Empty state: localized message.
- Load button: if `store.dirty()`, opens `ConfirmDialogComponent` ("Discard current changes?"); on confirm calls `store.loadFromSaved(scenario)`; toasts? (no — load is silent).
- Delete `×` button: opens `ConfirmDialogComponent` ("Delete saved scenario? '{{name}}' will be permanently removed."); on confirm calls `store.delete(id)`, toasts on success/failure.

## Task 4.2: Generic `ConfirmDialogComponent`

Tiny reusable dialog with title + body + confirm/cancel buttons. Lives in the interactive-city `builder/` directory (could be promoted to `ui-kit` later if other features need it). Returns `true` on confirm, `false` on cancel.

Inputs (via MAT_DIALOG_DATA):
- `titleKey`, `bodyKey` — i18n keys.
- `bodyParams` — optional interpolation params for the body (e.g. `{ name }`).
- `confirmKey`, `cancelKey` — button label keys.
- `dangerous` — when true, confirm button is `color="warn"`.

## Task 4.3: Wire drawer into page

- Page imports `SavedScenariosDrawerComponent` and renders `<cce-saved-scenarios-drawer />` after the totals bar.
- Drawer is sticky on the right side desktop; collapses to a bottom sheet on mobile.

## Phase 04 close-out

- `nx test` passes (target ~+10 tests).
- Lint clean; build green.
- Browser smoke: as authenticated user, save a scenario → drawer card appears; click Load on a different scenario → confirm dialog (if dirty) → loads; click × → confirm → row disappears + toast. Anonymous: drawer shows the sign-in CTA only.

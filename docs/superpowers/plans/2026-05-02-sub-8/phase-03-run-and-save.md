# Phase 03 — Run + save (Sub-8)

> Parent: [`../2026-05-02-sub-8.md`](../2026-05-02-sub-8.md) · Spec: [`../../specs/2026-05-02-sub-8-design.md`](../../specs/2026-05-02-sub-8-design.md) §4 (data flow), §5 (TotalsBarComponent), §7 (error handling)

**Phase goal:** Replace the placeholder totals strip with a real `TotalsBarComponent` that shows live numbers, exposes Run + Save buttons, and surfaces the server summary after Run. Add a `SaveScenarioDialogComponent` to capture the scenario name; wire the auth-gated save with a sign-in fallback (401 → `signIn(returnUrl)` → resume save on return). Toasts for success + error.

**Tasks:** 3
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 02 closed (commit `e891af9` or later); 79 suites · 421 tests green.

---

## Task 3.1: Add success-toast i18n keys

Tiny EN/AR additions under `interactiveCity.toasts.*`:
- `runOk` — "Scenario calculated."
- `saveOk` — "Scenario saved."
- `deleteOk` — "Scenario deleted."

(Errors already exist under `interactiveCity.errors.*`.)

## Task 3.2: `TotalsBarComponent`

Sticky bottom bar replacing the Phase 02 placeholder. Three columns + two right-aligned buttons:
- **Carbon impact** + unit (color-coded green for negative)
- **Cost** + unit
- **Server summary** (rendered when `store.serverResult()` is non-null; empty otherwise)
- **Run** button — `mat-flat-button` primary, disabled when `!store.canRun()`, spinner while `store.running()`. Calls `store.run()`, on success toast `interactiveCity.toasts.runOk`, on error toast `interactiveCity.errors.runFailed`.
- **Save** button — `mat-stroked-button`, disabled when `!store.canSave()`, spinner while `store.saving()`. If user is not authenticated → opens sign-in (existing `auth.signIn(returnUrl)` helper). If authenticated → opens `SaveScenarioDialogComponent` to confirm/edit name → on dialog submit calls `store.save()`, toast on success/failure.

`aria-live="polite"` on the totals row so screen readers hear updates after toggles.

Tests:
- Renders live totals + carbon unit + cost unit.
- Run button disabled when no selection; calls store.run() when clicked.
- Save button disabled when name empty or no selection.
- Run button click toasts on success.
- Run button click toasts on error.
- When unauthenticated, Save button calls `auth.signIn(...)` instead of opening the dialog.
- Server summary text appears after a successful run.

## Task 3.3: `SaveScenarioDialogComponent`

Tiny `MatDialog` with a single name input (pre-filled with `store.name()` if non-empty). Save button submits with the trimmed name; Cancel closes without changes. The dialog returns the submitted name (or `null` on cancel) via `MatDialogRef`.

Tests:
- Pre-fills the name from passed-in data.
- Submit returns the trimmed name.
- Cancel returns null.
- Empty name disables submit.

## Wire-up

The page replaces the Phase 02 placeholder totals strip with `<cce-totals-bar />`. The bar lives in the page's component tree (provides nothing extra; just inject the store + toast + auth + dialog).

## Phase 03 close-out

- Full `nx test web-portal --watch=false` passes (target ~+15 tests).
- Lint clean; build green.
- Browser smoke: pick technologies → live totals update → Run shows server summary → Save (unauthenticated) opens login → after sign-in, Save opens dialog → Save persists and saved scenario appears (when Phase 04 wires the drawer).

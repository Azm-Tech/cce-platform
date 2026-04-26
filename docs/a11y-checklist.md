# Accessibility Manual Checklist (what axe-core can't catch)

> Companion to the automated axe-core gate ([ADR-0012](adr/0012-a11y-axe-and-k6-loadtest.md)).
> WCAG 2.1 AA is the bar. This checklist covers items that require human judgement — automated tooling reports zero violations and a human still needs to verify these.

## Keyboard navigation

- [ ] Every interactive element reachable by `Tab`.
- [ ] `Tab` order matches visual reading order (top-to-bottom, with RTL flips when `dir="rtl"`).
- [ ] No keyboard trap: `Esc` exits dialogs; `Tab` cycles inside modals only while open.
- [ ] Focus is visible at every step (no `outline: none` without a replacement style).
- [ ] Skip-to-content link works as the first focusable element on each page.
- [ ] Custom components (combobox, menu, tabs) follow ARIA Authoring Practices keyboard patterns.

## Screen-reader narration order

- [ ] Reading order in **English** matches visual order with VoiceOver / NVDA / JAWS.
- [ ] Reading order in **Arabic (RTL)** matches visual order — verify with `lang="ar"` + `dir="rtl"`.
- [ ] Headings form a coherent outline (no skipped levels; one `<h1>` per page).
- [ ] Landmarks present and labeled (`<main>`, `<nav>`, `<aside>`, `<footer>`).
- [ ] Live regions (`aria-live`) announce notifications + form-submit results without spamming.
- [ ] `aria-label` / `aria-labelledby` on icon-only buttons; the announcement makes sense in both languages.

## Focus management on dialogs and overlays

- [ ] Opening a modal moves focus into it (typically the first focusable element or close button).
- [ ] Closing a modal returns focus to the element that triggered it.
- [ ] Background content has `inert` / `aria-hidden="true"` while the modal is open.
- [ ] Toasts / snackbars don't steal focus; they announce via `aria-live="polite"` (or `assertive` for errors).

## Color use beyond contrast

- [ ] No information conveyed by color alone (e.g., red/green pairs always paired with text/icon).
- [ ] Charts use distinguishable patterns or labels in addition to color.
- [ ] Status indicators have a text or icon variant.
- [ ] Error states pair color with an `aria-invalid` and a text message.

## Animations and motion

- [ ] All non-essential animation respects `prefers-reduced-motion: reduce`.
- [ ] Auto-playing carousels / loops are pausable (and don't loop more than 5s without an interaction option).
- [ ] No flashing / strobing content above WCAG seizure thresholds.
- [ ] Smooth-scroll respects reduced-motion preference.

## Forms

- [ ] Every input has a visible label (not placeholder-only).
- [ ] Error messages programmatically associated (`aria-describedby`) and announced on submit.
- [ ] Required fields marked beyond visual asterisk (e.g., `aria-required="true"` + text).
- [ ] Field grouping uses `<fieldset>` + `<legend>` where appropriate (radio sets, address blocks).
- [ ] Validation triggers respect language: error text is in the user's selected language.

## Bilingual specifics (ar / en)

- [ ] `lang` attribute updated on language switch (root and any inline mixed-language content).
- [ ] Numbers display per locale (Arabic-Indic vs Latin digits where applicable per design).
- [ ] Date/time formats locale-appropriate.
- [ ] Untranslated content doesn't crash the layout — fallback string shows clearly.
- [ ] Mixed-direction content uses `dir="auto"` or explicit `bdi`/`bdo` to avoid garbled rendering.

## Mobile + touch

- [ ] Tap targets ≥ 44 × 44 CSS px.
- [ ] No hover-only affordances (everything reachable on touch).
- [ ] Pinch-zoom not disabled (no `user-scalable=no` in viewport meta).
- [ ] Orientation changes don't trap users in an unusable layout.

## Review cadence

- Run this checklist on every UI-touching PR that introduces a new screen or significantly changes an existing one.
- Re-run during sub-project DoD verification.
- Add items as new patterns emerge — this list is expected to grow.

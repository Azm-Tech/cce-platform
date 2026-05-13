# Phase 03 — Polish + ADRs + close-out (Sub-9)

> Parent: [`../2026-05-02-sub-9.md`](../2026-05-02-sub-9.md) · Spec: [`../../specs/2026-05-02-sub-9-design.md`](../../specs/2026-05-02-sub-9-design.md) §13 (success criteria)

**Phase goal:** Land the URL `?q=` deep-link, wire the clear-thread confirm dialog, write ADRs 0049 + 0050, ship the completion doc, update CHANGELOG, and tag `web-portal-v0.4.0`.

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 02 closed (commit `99334e8` or later); 90 suites · 499 tests green.

---

## Task 3.1: URL `?q=` deep-link auto-send

`AssistantPage` reads `?q=` from `ActivatedRoute.snapshot.queryParamMap` on `ngOnInit`. If non-empty AND thread is empty, calls `store.sendMessage(q)` and removes `q` from the URL via `Router.navigate([], { queryParams: { q: null }, queryParamsHandling: 'merge', replaceUrl: true })`.

## Task 3.2: Clear-thread confirm dialog

Reuse Sub-8's `ConfirmDialogComponent` (lives at `apps/web-portal/src/app/features/interactive-city/builder/confirm-dialog.component.ts`). The Clear button on `AssistantPage` opens it with `assistant.thread.clearConfirm*` keys.

(Note: cross-feature import is fine for v0.1.0; ADR-0050 documents that ConfirmDialog should be promoted to `ui-kit` later.)

## Task 3.3: ADR-0049 — SSE + structured citation events

`docs/adr/0049-sse-structured-citation-events.md`. Justifies SSE-over-fetch + `ReadableStream` over EventSource (POST-capable, BFF-cookie-friendly, abortable) and typed citation events over inline `[N]` text scraping.

## Task 3.4: ADR-0050 — Client-owned in-memory thread state

`docs/adr/0050-client-owned-in-memory-thread.md`. Documents why threads live in memory only for v0.1.0 (LLM is stub; persistence design lands with the real LLM and its identity/auth model).

## Task 3.5: Completion doc + CHANGELOG + tag

- `docs/sub-9-assistant-completion.md` mirroring Sub-7/8 shape.
- CHANGELOG entry under `web-portal-v0.4.0`.
- `git tag web-portal-v0.4.0`.

## Phase 03 close-out

- ADRs land in `docs/adr/`.
- Completion doc cross-references ADRs + spec + plan.
- CHANGELOG mirrors entry.
- Tag exists locally.
- Final test/lint/build still green.

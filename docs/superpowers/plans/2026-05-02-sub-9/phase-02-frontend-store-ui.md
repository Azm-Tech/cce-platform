# Phase 02 — Frontend store + UI (Sub-9)

> Parent: [`../2026-05-02-sub-9.md`](../2026-05-02-sub-9.md) · Spec: [`../../specs/2026-05-02-sub-9-design.md`](../../specs/2026-05-02-sub-9-design.md) §4 (data flow), §5 (components), §7 (a11y)

**Phase goal:** Bring the assistant UI to life. Implement `AssistantStore` (signals + 5 actions), then fill the 5 sub-component stubs (MessageList, MessageBubble, ComposeBox, CitationChip, TypingIndicator), and finally wire them into `AssistantPage` so an anonymous user can type a question, watch tokens stream in, and see citation chips. Phase 03 adds the URL `?q=` deep-link, clear-thread confirm dialog, ADRs, and the v0.4.0 tag.

**Tasks:** 6 (consolidated from the 9-task budget in the master)
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 01 closed (commit `4363824` or later); 85 suites · 453 frontend tests, backend SSE endpoint streaming, `AssistantApiService.query` wired.

---

## Task 2.1: `AssistantStore` — full implementation

State: `messages` + `streaming`. Computed: `canSend`. Actions: `sendMessage`, `cancel`, `retry`, `regenerate`, `clear`. Manages an `AbortController` per stream. Mocks the API service via `useValue` in tests.

## Task 2.2: `CitationChipComponent` + `TypingIndicatorComponent` (paired — both small)

CitationChip: button with `routerLink` (one inline variant `[N]`, one footer variant `[N] Title` with kind icon + tooltip). TypingIndicator: pure CSS three-dot animation.

## Task 2.3: `MessageBubbleComponent`

Role-styled `<li>` with copy / retry / regenerate actions. Streaming cursor while `status === 'streaming'`. Citations footer.

## Task 2.4: `MessageListComponent`

`<ul aria-live="polite">` rendering one bubble per message. Auto-scroll to bottom on message-count increase via `effect()`. TypingIndicator after the last message when status is `pending`. Empty state.

## Task 2.5: `ComposeBoxComponent`

Reactive `FormControl<string>` textarea. Enter sends, Shift+Enter newlines. Send button morphs into Cancel during streaming.

## Task 2.6: Wire components into `AssistantPage` + integration smoke

Mount `<cce-message-list />` in the scroll area, `<cce-compose-box />` sticky at the bottom. Spec asserts the page wires components.

## Phase 02 close-out

- `nx test web-portal --watch=false` — target ~+45 tests to ~498.
- Lint clean; build green.

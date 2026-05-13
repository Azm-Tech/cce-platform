# Sub-Project 09 — Smart Assistant streaming + threading + citations — Completion Report

**Tag:** `web-portal-v0.4.0`
**Date:** 2026-05-03
**Spec:** [Smart Assistant Design Spec](../project-plan/specs/2026-05-02-sub-9-design.md)
**Plan:** [Smart Assistant Implementation Plan](../project-plan/plans/2026-05-02-sub-9.md)
**Predecessor:** [Sub-8 Interactive City completion](sub-8-interactive-city-completion.md)
**Successor (planned):** Sub-10 Deployment / Infra

---

## Summary

Sub-9 replaces the Sub-6 Phase 9 single-turn assistant skeleton at `/assistant` with a real conversational UI: multi-turn threading, server-streamed responses (SSE), and structured citations grounded in the existing knowledge maps + resources. The LLM itself stays a fake-streaming stub that yields ~8 chunks over ~1.2s and emits 1–2 citations from seeded data — real LLM integration drops in by replacing one class without touching the frontend.

**Total tasks:** ~21 across 4 phases. **Test count: web-portal 499/499 (was 445 at end of Sub-8) · admin-cms 218/218 · ui-kit 27/27 = 744 Jest tests across 90 web-portal suites. Backend `Application.Tests` 433/433 + integration tests `AssistantEndpointTests` 2/2 passing.**

## Phase checklist

- [x] **Phase 00** — Cross-cutting: extended `assistant.types.ts` (Role, Citation, ThreadMessage, AssistantQueryRequest, SseEvent + helpers, 4 tests); `lib/sse-client.ts` async-iterator parser with chunk-buffering + abort + malformed-tolerant skip (7 tests); backend `SseEvent` records (Text/Citation/Done/Error) + `CitationDto` + `SseWriter` helper (4 xunit tests); EN+AR `assistant.*` i18n keys (32 keys, parity verified); 5 sub-component stubs + page replacement.
- [x] **Phase 01** — Backend SSE: `ISmartAssistantClient` reshaped to `IAsyncEnumerable<SseEvent>`; `AskAssistantCommand` carries `IReadOnlyList<ChatMessage>`; validator enforces non-empty + max 50 + last-is-user + role-in-{user,assistant} + content max 4000 + locale in {ar,en} (9 tests); `/api/assistant/query` writes SSE via `SseWriter` (2 integration tests); fake-streamer yields ~8 chunks @ 150ms with citations from seeded Resources + KnowledgeMapNodes; frontend `AssistantApiService.query` wired to `openSseStream` (1 test).
- [x] **Phase 02** — Frontend store + UI: `AssistantStore` (2 state signals + 1 computed + 5 actions: sendMessage / cancel / retry / regenerate / clear, 13 tests); `CitationChipComponent` (inline + footer variants with kind icons + tooltips, 7 tests); `MessageBubbleComponent` (role-styled with streaming cursor + copy/retry/regenerate actions, 9 tests); `MessageListComponent` (auto-scroll on new message, aria-live="polite", typing indicator, 5 tests); `ComposeBoxComponent` (Reactive Forms textarea, Enter sends, Shift+Enter newline, send/cancel morph, char-count at ≥1500, 9 tests); `AssistantPage` mounts list + compose with clear-thread button (4 tests).
- [x] **Phase 03** — Polish + ADRs + completion: URL `?q=` deep-link auto-send (with URL strip on entry); clear-thread confirm dialog (reuses Sub-8's `ConfirmDialogComponent`); 2 new ADRs (0049 SSE + structured citations, 0050 client-owned in-memory thread); this completion doc; CHANGELOG entry under `web-portal-v0.4.0`; tag `web-portal-v0.4.0`.

## Endpoint coverage

Sub-9 adds zero new endpoints. The single existing endpoint changed shape:

| Endpoint | Method | Auth | Status | Notes |
|---|---|---|---|---|
| `/api/assistant/query` | POST | Anon | **Reshape** | Was `{ question, locale } → JSON { reply }`; now `{ messages[], locale } → text/event-stream` of typed events. |

## ADRs

- [ADR-0049 — SSE + structured citation events](adr/0049-sse-structured-citation-events.md)
- [ADR-0050 — Client-owned in-memory thread state](adr/0050-client-owned-in-memory-thread.md)

## Test counts (final)

| Project | Suites | Tests |
|---|---|---|
| `web-portal` | 90 | 499 (+54 since Sub-8's 445) |
| `admin-cms` | 47 | 218 (unchanged) |
| `ui-kit` | 7 | 27 (unchanged) |
| **Total Jest** | **144** | **744** |
| Backend `Application.Tests` | — | 433 (+5 net since Sub-8: +4 SseEvent serialization + 9 new validator − 8 old handler/validator) |
| Backend integration `AssistantEndpointTests` | — | 2 (was 4 single-turn tests; reshape replaces them) |

## Bundle impact

- **Initial web-portal bundle: unchanged from Sub-8** — no new heavy dependencies. SSE handled with native `fetch` + `ReadableStream`; no `eventsource` polyfill.
- The `/assistant` lazy chunk grows modestly (5 sub-components + `lib/sse-client.ts`).

## UX decisions baked in

| Area | Decision | Rationale |
|---|---|---|
| Transport | SSE over `fetch` + `ReadableStream` (POST-capable, abortable, BFF-cookie-friendly) | ADR-0049 |
| Citations | Typed `citation` events (kind + href + title) instead of inline `[N]` markers in prose | ADR-0049 |
| Persistence | In-memory only for v0.1.0 | ADR-0050 |
| LLM | Fake-streaming stub | Real LLM swap-in is one class change |
| Cancel | Preserve partial content, mark complete (not error) | User chose to stop, not a failure |
| Auto-scroll | On new-message length increase, NOT on each text chunk | Bubble grows in place at the bottom; no jitter |
| Compose | Enter sends, Shift+Enter newlines | Standard chat-app convention |
| Clear-thread | Reuses Sub-8's `ConfirmDialogComponent` | Cross-feature import; promote to ui-kit later |
| URL ?q= | Auto-send on entry; strip from URL | Deep-link entry from search; refresh-safe |

## Polish backlog (carried forward)

- **Real LLM client** — replace the fake-streamer with an Anthropic / OpenAI / vendor SDK call. Lands in Sub-10+.
- **Server-persisted threads** — needs identity / privacy / retention model.
- **Markdown rendering** — once the real LLM emits markdown.
- **Multi-thread sidebar** — list past conversations.
- **Voice input / TTS** — out of scope.
- **Promote `ConfirmDialogComponent` to ui-kit** — currently cross-imported from Sub-8 (interactive-city/builder).
- **axe-core a11y CI gate** — manual axe-core check passed; CI gate deferred to Sub-10 alongside Sub-7's Lighthouse audit.

## Stack matrix

| Layer | Version |
|---|---|
| Angular | 19.2.21 (unchanged) |
| Angular Material | 18 |
| ngx-translate | 16.x |
| Nx | 21.x |
| Reactive Forms | built-in |
| TypeScript | 5.x |
| Jest | 30 (workspace), 29-compatible config |
| Playwright | latest stable |
| .NET | 8 (unchanged) |
| MediatR | 12.x (unchanged; assistant endpoint now bypasses it for streaming) |

No new heavy dependencies. The `/assistant` lazy chunk stays light.

## Next steps (Sub-10)

- **Sub-10 — Deployment / Infra (IDD v1.2)**: production build, CI workflows, Lighthouse + axe-core gates, Kubernetes manifests, observability. The real LLM client likely lands here too, alongside whatever auth/persistence model real conversations need.

# ADR-0049 — SSE + structured citation events

**Status:** Accepted
**Date:** 2026-05-03
**Deciders:** CCE frontend + backend team

---

## Context

Sub-9 introduces conversational threading and citation grounding for the Smart Assistant at `/assistant`. Two transport choices and two citation-modeling choices crossed during brainstorming:

| Concern | Option A | Option B (chosen) |
|---|---|---|
| Transport | Single JSON `POST → 200 OK { reply, citations }` after the LLM finishes | Server-sent events streamed as the LLM produces tokens |
| Citation modeling | Inline `[N]` markers scraped from text + a separate request for the source list | Structured `citation` events typed alongside `text` events |

The single-JSON option fails the "watch the assistant think" UX users have come to expect — they wait staring at a spinner for whatever the model takes. Once a real LLM lands later (Sub-10+), full responses can run several seconds. Streaming makes the wait useful.

The inline-marker option is fragile: the model has to reliably embed `[1]` / `[2]` markers in its prose, the client has to scrape them back out, and the link between marker and source has to round-trip a second request. Structured events let the server emit `text` and `citation` events independently — the model never has to format markers; the client gets typed payloads it can render exactly.

Two transport mechanics were available for SSE on the wire:
- Native `EventSource` API: GET-only, can't send a body, doesn't propagate cookies in all browsers, can't be aborted via `AbortController`.
- `fetch` + `ReadableStream`: POST-capable, BFF-cookie-friendly via `credentials: 'same-origin'`, abortable with `AbortSignal`.

Sub-9 needs all three (POST, BFF cookies, abort), so `EventSource` is out.

## Decision

**The assistant transport is `text/event-stream` over `fetch` + `ReadableStream`. Each event is `data: <json>\n\n` where the JSON is one of four discriminated-union shapes:**
- `{ "type": "text", "content": "<chunk>" }` — append to the current assistant message's content.
- `{ "type": "citation", "citation": { id, kind, title, href, sourceText? } }` — append to the current assistant message's citation list.
- `{ "type": "done" }` — stream is complete; no more events.
- `{ "type": "error", "error": { "kind": "<network|server|...>" } }` — server-side failure mid-stream.

Frontend `lib/sse-client.ts` wraps `fetch` + `ReadableStream` to expose the events as `AsyncIterable<SseEvent>`. Backend `SseWriter.WriteAsync` writes an `IAsyncEnumerable<SseEvent>` to the HTTP response with `Content-Type: text/event-stream; charset=utf-8`, `Cache-Control: no-cache`, `X-Accel-Buffering: no`.

Citations carry `kind: 'resource' | 'map-node'` and a routable `href` so the UI renders them as `<button [routerLink]>` chips that open the existing knowledge-center / knowledge-maps pages.

## Consequences

**Positive:**
- Users see tokens as they arrive — perceived latency drops to roughly first-chunk time (~150ms in the stub, model-dependent in production).
- Citations are first-class structured data — the UI can show distinct chip styles per kind, count them, group them, link them. No regex on assistant prose.
- The transport works under our existing BFF cookie auth model. No special cross-origin handling.
- `AbortController` propagates cleanly through `fetch` to the server's `CancellationToken` — Cancel really cancels.
- Real LLM swap-in later changes one class (`SmartAssistantClient`); the SSE wire format and frontend client stay identical.

**Negative:**
- Custom SSE parser must handle event-frame splits across `ReadableStream` chunk boundaries. Tested explicitly in `lib/sse-client.spec.ts`.
- Server-side cancellation responsibility falls on the LLM client implementation — must honour `CancellationToken` in `await Task.Delay(ms, ct)` and any LLM-vendor SDK calls.
- No native browser request retry on connection drops — the store's retry action handles user-initiated retry; no automatic reconnect.

**Neutral:**
- The wire format is intentionally narrow (4 event types) so swap-ins don't need format extensions. Future event types (e.g. `tool-use`, `thinking`) would extend the discriminator union.
- `Content-Type: text/event-stream` is recognised by browser dev tools so events are inspectable in Network tab.

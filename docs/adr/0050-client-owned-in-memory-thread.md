# ADR-0050 — Client-owned in-memory thread state

**Status:** Accepted
**Date:** 2026-05-03
**Deciders:** CCE frontend + backend team

---

## Context

The Smart Assistant at `/assistant` (Sub-9) introduces multi-turn conversation. Three persistence models were considered:

| Option | Behaviour | Tradeoff |
|---|---|---|
| **In-memory only (chosen)** | The thread lives in the `AssistantStore` for the route activation. Refresh wipes it. | Zero schema design. Zero persistence bugs. Loses conversations on refresh. |
| **`sessionStorage` mirror** | Thread persists across refreshes within the same tab. | Trivial to implement. Schema-versioning headache when message shapes evolve (citations added in v0.2, attachments in v0.3, etc). |
| **Server-persisted threads** | Backend table + identity-tied thread aggregate; threads survive forever. | Real database schema, real auth model (anonymous threads vs authenticated), real privacy concerns (deletion, export, retention), real LLM ops. |

The Sub-9 LLM is a stub that fake-streams placeholder text + 1-2 citations from seeded data. Spending design budget on persistence schemas and identity for placeholder responses is YAGNI. Real persistence design lands together with the real LLM (Sub-10+) — at that point we'll know whether anonymous threads need a server-issued conversation token, whether retention is per-tenant, whether there's a "share this conversation" feature, etc.

`sessionStorage` was rejected for similar reasons: the marginal "survives a refresh in this tab" UX gain isn't worth the schema-versioning weight when v0.2+ will likely change the message shape.

## Decision

**The `AssistantStore` owns an in-memory `messages: signal<ThreadMessage[]>`. No persistence layer. Refresh / route-leave wipes the thread. The `AssistantPage` is provided per-route so each entry gets a fresh store instance.**

The store carries:
- `messages: signal<ThreadMessage[]>` — the full conversation, including in-flight assistant messages.
- `streaming: signal<boolean>` — true while a stream is active.
- `canSend: computed(() => !streaming())`.
- One `AbortController` per in-flight stream, held in a private field, used by `cancel()`.

Actions: `sendMessage`, `cancel`, `retry`, `regenerate`, `clear`. `URL ?q=` deep-link triggers `sendMessage` once on entry, then strips itself from the URL.

## Consequences

**Positive:**
- Zero persistence code in v0.1.0 — simpler store, simpler tests, simpler mental model.
- No schema versioning churn while message shapes are still settling (citation kinds, status enum, error kinds).
- Privacy is the default: nothing about a conversation leaves the user's tab unless they're actively asking the assistant.
- The store's API (`sendMessage` / `cancel` / `retry` / `regenerate` / `clear`) is the same shape we'd want with persistence — adding it later is a non-breaking enhancement.

**Negative:**
- Refreshing `/assistant` loses the entire conversation. Users who Cmd+R out of habit will be surprised. Mitigation for now: the URL `?q=` deep-link from search results lets users replay a starting question, which is the most common entry pattern.
- No "history" sidebar. Users who want to revisit yesterday's chat can't.

**Neutral:**
- The `ConfirmDialogComponent` (cross-imported from Sub-8 for the clear-thread dialog) should be promoted to `libs/ui-kit` when more features need it. Out of scope for Sub-9.
- When real LLM lands and persistence is added, the migration is store-internal: replace the `messages` signal with one backed by a `ThreadsApiService.list/save/delete` flow. Sub-components don't change.

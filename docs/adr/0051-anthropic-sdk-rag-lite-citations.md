# ADR-0051 — Anthropic.SDK + RAG-lite citations

**Status:** Accepted
**Date:** 2026-05-03
**Deciders:** CCE backend team

---

## Context

Sub-9 shipped the assistant UX with a fake-streaming stub on the backend. Sub-10a replaces the stub with a real LLM client + a citation source. Two crossing decisions had to be resolved.

### LLM provider choice

| Option | Tradeoff |
|---|---|
| **Anthropic.SDK 5.0 (community NuGet) — chosen** | Maps cleanly onto the existing `IAsyncEnumerable<SseEvent>` interface from Sub-9. Streaming is first-class (`StreamClaudeMessageAsync`). Tool-use ready for future RAG. Community SDK rather than first-party Anthropic-released, but actively maintained. |
| **Provider-agnostic OpenAI-compat REST via `HttpClient`** | Works against Azure OpenAI, OpenRouter, Anthropic via wrappers. More wiring code; we'd rebuild streaming-event parsing on top of `fetch` instead of getting it from the SDK. |
| **Microsoft.Extensions.AI** | Promising abstraction layer but still pre-1.0 in Sub-10a's timeframe; the streaming surface for Anthropic specifically isn't as mature as the SDK's. |
| **OpenAI SDK** | Would commit us to a different model lineage than the Sub-9 design's reference (Claude). |

Anthropic.SDK won on lowest-friction-to-streaming + the project's existing tooling preference for Claude.

### Citation source — RAG vs RAG-lite

| Option | Tradeoff |
|---|---|
| **RAG-lite: Jaccard token overlap (chosen)** | No vector store, no embeddings infra, no model-dependency at search time. Locale-aware via the title-field switch. Adequate recall on the small seeded catalog (~13 nodes + 5 resources). |
| **Embedding-based RAG (text-embedding-3-small + pgvector or Azure AI Search)** | Higher recall on conceptually related queries. New infra component (vector store). Extra cold-start cost. Locale handling needs separate embeddings per language. |
| **No citations** | Simpler. Loses Sub-9's design promise that citations link the answer to existing knowledge maps + resources. |

RAG-lite gets us "citations show up and they're relevant for the small catalog we've seeded today." When the catalog grows past ~50 rows or users start asking semantically rich questions that don't share tokens with titles, embeddings become the right call. That's a future sub-project, not Sub-10a.

---

## Decision

**Production assistant uses `Anthropic.SDK` 5.0.0. The implementation lives in `CCE.Infrastructure.Assistant.AnthropicSmartAssistantClient` and is selected by `AssistantClientFactory` based on `Assistant:Provider=anthropic` + a non-empty `ANTHROPIC_API_KEY` env-var. The fake-streaming stub remains the default (CI / offline dev / no-key environments fall back to it automatically).**

**Citations are sourced by `CCE.Infrastructure.Assistant.CitationSearch` — token-overlap Jaccard scoring against `Resources` and `KnowledgeMapNodes`, returning up to 1 of each kind per assistant turn. Locale chooses the title field (`TitleEn`/`TitleAr`, `NameEn`/`NameAr`).**

The `IAnthropicStreamProvider` abstraction wraps the SDK's `MessagesEndpoint.StreamClaudeMessageAsync` so unit tests can mock the streaming behaviour without touching the network.

## Consequences

**Positive:**
- The frontend Sub-9 work doesn't change at all when the stub flips to real Claude — `IAsyncEnumerable<SseEvent>` is the interface boundary, not "is the model real."
- The stub stays available for CI, offline dev, and local smoke runs without an API key.
- Citation chips link to existing pages users already know (`/knowledge-center/resources/...`, `/knowledge-maps/...?node=...`).
- One class change moves to a different LLM provider (e.g., OpenAI) — `AnthropicSmartAssistantClient` is the only file that imports the SDK.

**Negative:**
- RAG-lite misses semantic-relevance queries: "How do I reduce emissions?" won't match "Energy Efficiency" unless those tokens are in the user/assistant text.
- `Anthropic.SDK` is community-maintained — risk of API drift between minor versions. Pinned to 5.0.0 in `Directory.Packages.props`.
- The community SDK exposes its own `Anthropic.SDK.SseEvent` type that collides with our `CCE.Application.Assistant.SseEvent` — disambiguated via `using SseEvent = ...` alias. Slightly noisy, but contained to one file.

**Neutral:**
- Embedding-based RAG remains a future sub-project; the `ICitationSearch` interface lets us swap in an embedding-backed implementation without changing the AnthropicSmartAssistantClient.
- Bootstrap-time stderr warning when `Assistant:Provider=anthropic` but no `ANTHROPIC_API_KEY` is set surfaces the fallback to operators who'd otherwise wonder why responses look like the stub.

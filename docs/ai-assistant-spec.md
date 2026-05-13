# CCE Smart Assistant — Technical & Business Specification

**Audience:** Product, engineering, infra, ops
**Status:** Shipped (`web-portal-v0.4.0` + `app-v1.0.0`)
**Last reviewed:** 2026-05-08
**Sources of truth:**
- Design: [`project-plan/specs/2026-05-02-sub-9-design.md`](../project-plan/specs/2026-05-02-sub-9-design.md)
- Completion (frontend + stub): [`docs/sub-9-assistant-completion.md`](sub-9-assistant-completion.md)
- Completion (real LLM): [`docs/sub-10a-app-productionization-completion.md`](sub-10a-app-productionization-completion.md)
- ADRs: [`0049`](adr/0049-sse-structured-citation-events.md), [`0050`](adr/0050-client-owned-in-memory-thread.md), [`0051`](adr/0051-anthropic-sdk-rag-lite-citations.md)

---

## 1. Executive summary

The Smart Assistant is a conversational chat surface inside the public Carbon Circular Economy (CCE) portal at `/assistant`. Users can ask natural-language questions about the circular carbon economy and receive streamed bilingual (English / Arabic) answers from a hosted large language model (Anthropic Claude Sonnet 4.5). Every answer is grounded with structured citations that link to the platform's own Knowledge Center resources and Knowledge Map nodes.

The product satisfies BRD §4.1.11 (Smart Assistant) and §6.2.6–§6.2.9 (Smart-assistant user stories). It ships as part of Sub-Project 9 (frontend + transport + stub LLM) and Sub-Project 10a (real LLM client + citation source).

Key properties:
- **Anonymous use** — no sign-in is required to chat (rate-limiting governs abuse).
- **Server-Sent Events streaming** — tokens render as they arrive.
- **Structured citations** — typed events, not regex-scraped markers.
- **In-memory threads** — no server-side conversation persistence in v0.x.
- **Provider-swappable** — one factory + one class change moves to a different LLM vendor.
- **Stub fallback** — local dev / CI / no-API-key environments serve a deterministic fake stream.

---

## 2. Business specification

### 2.1 BRD coverage

| BRD reference | Requirement | How the assistant satisfies it |
|---|---|---|
| §4.1.11 | Smart Assistant — conversational Q&A grounded in the knowledge base | Multi-turn threading, SSE streaming, RAG-lite citation chips linking to resources + map nodes |
| §4.1.3 | Bilingual content (Arabic / English) | Locale-aware system prompt, locale-aware citation titles, RTL-correct UI |
| §6.2.6 | "As a visitor I want to ask the assistant a question…" | Anonymous compose box, no auth required, BFF cookies forwarded |
| §6.2.7 | "…and see the answer stream as it's produced" | Tokens render chunk-by-chunk with a typing indicator, ~150 ms first-chunk in stub, model-dependent in production |
| §6.2.8 | "…with citations to the platform's content" | Per-turn citation chips — up to 1 resource + 1 map node — rendered as `<button [routerLink]>` |
| §6.2.9 | "…and be able to retry / regenerate / cancel" | Cancel preserves partial content; retry replays the last user prompt after an error; regenerate re-streams the last completed turn |

### 2.2 Personas + use cases

| Persona | Primary use case | Auth |
|---|---|---|
| **Visitor** (anonymous reader) | Ask a quick question about a concept, get a 2–4 sentence answer plus pointers to relevant material | Anonymous (BFF cookie may exist but is not required) |
| **Authenticated member** | Same as visitor; identity is not used by the assistant yet (no per-user history, no per-user rate limits beyond global) | BFF cookie |
| **Operator** | Roll the API key, toggle provider (stub ↔ Anthropic), inspect metrics | n/a — config + ops surface |

### 2.3 Out-of-scope (v0.x)

The following were explicitly deferred:

- **Server-persisted threads** — conversations live in browser memory only; closing the tab loses the thread. Persistence requires identity / privacy / retention model (ADR-0050).
- **Multi-thread sidebar** — only one active conversation per tab.
- **Markdown rendering** — assistant prose is plain-text whitespace-preserved (`white-space: pre-wrap`). Will land when the system prompt explicitly asks for markdown.
- **Tool use** (function calling) — supported by `Anthropic.SDK` but not wired; future RAG/lookup work.
- **Voice input / TTS** — not in scope.
- **Embedding-based RAG** — current citation engine is token-overlap (Jaccard); embeddings deferred until catalog exceeds ~50 documents.
- **Per-user rate limiting** — global rate limit applies; per-identity quotas deferred.

### 2.4 Success metrics

| Metric | Target | Surfaced as |
|---|---|---|
| First-chunk latency | ≤ 1.5 s p50, ≤ 4 s p95 | Prometheus histogram (`cce_assistant_first_chunk_ms`) |
| Stream completion rate | ≥ 98 % of started streams reach a `done` event | `cce_assistant_streams_runtime_total{outcome="done"}` / `started` |
| Citation attach rate | ≥ 60 % of completed turns carry ≥ 1 citation | `cce_assistant_citations_runtime_total` divided by completed streams |
| Cost per turn | < USD 0.01 average | Vendor invoice + token counts; tracked monthly |

---

## 3. Functional specification

### 3.1 User flows

**3.1.1 Open the assistant**
1. User clicks the floating "Ask AI" capsule in the bottom-leading corner OR navigates to `/assistant` directly.
2. The assistant opens as a side-sheet dialog (modal) on desktop and a full-screen sheet on phones.
3. The empty state shows a welcome hint plus 6 suggested prompts (POLICY / ENERGY / CIRCULAR / FINANCE / CITIES / TECH).

**3.1.2 Send a message**
1. User types in the compose box; Enter sends, Shift+Enter inserts a newline.
2. A user message bubble appears immediately, followed by an assistant placeholder bubble with a typing indicator.
3. The browser opens an SSE stream against `POST /api/assistant/query` with `{ messages, locale }` and cookies attached.
4. Tokens stream into the assistant bubble (`status: 'streaming'`); a blinking cursor at the end shows liveness.
5. Citation chips render below the bubble as `citation` events arrive.
6. A `done` event flips the bubble to `status: 'complete'`; the cursor disappears.

**3.1.3 Cancel an in-flight stream**
- Compose-box send-button morphs into a stop button while streaming.
- Clicking stop calls `AbortController.abort()`; partial content is **preserved** and the bubble is marked `complete` (not error) — the user *chose* to stop.

**3.1.4 Recover from a failed stream**
- An error event or stream-open failure marks the bubble `status: 'error'` with `errorKind`.
- The bubble exposes a **Retry** button that drops the failed assistant placeholder and re-sends the last user prompt.

**3.1.5 Regenerate a successful answer**
- The most recent completed assistant bubble exposes a **Regenerate** button.
- Click drops the completed bubble and re-runs the last user prompt.

**3.1.6 Clear the thread**
- A "New conversation" button in the dialog header opens a confirm dialog. On confirm, `messages` is reset to `[]`.

**3.1.7 Deep-link entry**
- Hitting `/assistant?q=...` auto-sends the URL-decoded `q` as the first message and strips the `q` parameter from the URL to keep refresh safe.
- Used by the global search component to route ambiguous queries to the assistant.

### 3.2 UI components (Angular)

All components live under `apps/web-portal/src/app/features/assistant/`.

| File | Role |
|---|---|
| `assistant.page.{ts,html,scss}` | `/assistant` route — full-page mount of list + compose, plus header with clear-thread + close buttons |
| `assistant.dialog.{ts,html,scss}` | Side-sheet dialog version invoked from the floating FAB and from the portal-shell |
| `thread/assistant-store.service.ts` | Signals-first state container — `messages`, `streaming`, plus 5 actions: `sendMessage`, `cancel`, `retry`, `regenerate`, `clear` |
| `thread/message-list.component.{ts,html,scss}` | Scroll container, auto-scrolls on new-message length, `aria-live="polite"` |
| `thread/message-bubble.component.{ts,html,scss}` | Role-styled bubble; renders streaming cursor, copy/retry/regenerate actions, citation footer |
| `thread/compose-box.component.{ts,html,scss}` | Reactive Forms textarea; Enter sends; Shift+Enter newlines; send/cancel morph; char-count at ≥ 1500 |
| `thread/citation-chip.component.{ts,html,scss}` | Inline + footer variants with kind icons (book / map) + tooltip + `[routerLink]` |
| `thread/typing-indicator.component.{ts,html,scss}` | 3-dot pulsing indicator for the assistant placeholder |
| `lib/sse-client.ts` | `fetch` + `ReadableStream` SSE parser; yields typed events as `AsyncIterable<SseEvent>` |
| `assistant-api.service.ts` | Single thin method `query(req, signal)` returning the iterable stream |
| `assistant.types.ts` | TypeScript mirrors of the wire types (`SseEvent` union, `ThreadMessage`, etc.) |
| `routes.ts` | Lazy-loaded route definition |

### 3.3 i18n surface

The assistant ships 32 translation keys under `interactiveCity` and `assistant` namespaces, parity-verified between `en.json` and `ar.json`. Key clusters:

- `assistant.title`, `subtitle`, `empty`, `hint`
- `assistant.compose.{placeholder, send, sendLabel, cancel, cancelLabel, charCount, charLimitWarning}`
- `assistant.message.{user, assistant, copy, copied, retry, regenerate}`
- `assistant.citations.*`
- `assistant.errors.*`
- `assistant.suggested.*` — the 6 starter prompts

Locale comes from `LocaleService.locale()` and is sent to the backend in the request body so the system prompt is locale-correct.

---

## 4. Wire protocol

### 4.1 Endpoint

| Endpoint | Method | Auth | Status |
|---|---|---|---|
| `/api/assistant/query` | `POST` | Anonymous (allowed) | Production |

**Request body** (`application/json`):

```json
{
  "messages": [
    { "role": "user",      "content": "What is industrial symbiosis?" },
    { "role": "assistant", "content": "Earlier reply…" },
    { "role": "user",      "content": "Follow-up question." }
  ],
  "locale": "en"
}
```

Validation (FluentValidation — `AskAssistantCommandValidator`):
- `messages` non-empty, ≤ 50 entries
- last message must have `role: "user"`
- each entry's `role` ∈ {`user`, `assistant`}, `content` non-empty + ≤ 4 000 chars
- `locale` ∈ {`ar`, `en`}

**Response**: `Content-Type: text/event-stream; charset=utf-8`, `Cache-Control: no-cache`, `X-Accel-Buffering: no`. Each event is a single `data: <json>\n\n` frame.

### 4.2 Event types

```ts
type SseEvent =
  | { type: 'text';     content: string }
  | { type: 'citation'; citation: { id: string; kind: 'resource' | 'map-node'; title: string; href: string; sourceText?: string } }
  | { type: 'done' }
  | { type: 'error';    error:    { kind: string } };
```

Backend `SseWriter` serializes via `System.Text.Json` with `PropertyNamingPolicy = camelCase`. Frontend `lib/sse-client.ts` parses with a chunk-boundary-tolerant buffer.

### 4.3 Frame ordering contract

1. Zero or more `text` events (each is a delta — append to the current assistant message's `content`).
2. Zero or more `citation` events emitted **after** the model finishes producing prose (citations are computed from the full reply text).
3. Exactly one terminal event: `done` (success) or `error` (failure).

The frontend tolerates streams that close without a terminal event by marking the message `complete` with whatever partial content arrived.

### 4.4 Cancellation

The frontend's `AbortController.abort()` cancels the underlying `fetch`, which propagates to the server's `CancellationToken`. The Anthropic SDK's `StreamClaudeMessageAsync` honours the token, so cancelled requests stop billing within milliseconds.

---

## 5. Backend implementation

### 5.1 Project layout (.NET 8 / clean architecture)

| Project | Files | Purpose |
|---|---|---|
| `CCE.Api.External` | `Endpoints/AssistantEndpoints.cs`, `Endpoints/SseWriter.cs` | HTTP route + SSE writer |
| `CCE.Application` | `Assistant/{ISmartAssistantClient, ICitationSearch, SseEvent, CitationDto, ChatMessage}.cs`, `Assistant/Commands/AskAssistant/*` | Streaming contract + DTOs + command + validator |
| `CCE.Infrastructure` | `Assistant/{AnthropicSmartAssistantClient, SmartAssistantClient, CitationSearch, AssistantClientFactory, AnthropicOptions}.cs` | Real + stub LLM clients, factory, RAG-lite |

### 5.2 Provider selection

`AssistantClientFactory.AddCceAssistantClient(IConfiguration)` picks the implementation at startup based on:

| Config | Behavior |
|---|---|
| `Assistant:Provider = "stub"` (default) | `SmartAssistantClient` — deterministic fake stream + citations from seeded data |
| `Assistant:Provider = "anthropic"` + non-empty `ANTHROPIC_API_KEY` env | `AnthropicSmartAssistantClient` — real Claude streaming + RAG-lite citations |
| `Assistant:Provider = "anthropic"` + missing key | Falls back to stub + prints `warn: …` to stderr |

This means CI, offline dev, and any environment without an API key keep working with a stub. Production environments set `ASSISTANT_PROVIDER=anthropic` and `ANTHROPIC_API_KEY=sk-ant-…` (per the deploy stack — see §10).

### 5.3 `AnthropicSmartAssistantClient` flow

1. Convert the request `messages` array to `Anthropic.SDK.Messaging.Message` list (Role mapping: `assistant` → `RoleType.Assistant`, otherwise `User`).
2. Inject a locale-aware system prompt:
   - **English**: *"You are the CCE Knowledge Center assistant. Answer in English. Be concise (2-4 sentences). Topics relate to circular carbon economy."*
   - **Arabic**: *"أنت مساعد منصة المعرفة لـ CCE. أجب باللغة العربية. كن موجزاً (2-4 جمل). تتعلق المواضيع بالاقتصاد الكربوني الدائري."*
3. Open `MessagesEndpoint.StreamClaudeMessageAsync` with parameters from `AnthropicOptions` (defaults: model `claude-sonnet-4-5-20250929`, `MaxTokens = 1024`, `Temperature = 0.3`).
4. For each `text_delta` from the SDK, accumulate the full assistant text and `yield return new TextEvent(delta)`.
5. After the model stream completes, call `ICitationSearch.FindCitationsAsync(userQuestion, fullReply, locale, ct)` and yield each result as `CitationEvent`.
6. Yield `DoneEvent` to terminate.

Exception handling uses a wrapper iterator (`WithErrorHandling`) that converts any SDK or network failure into an `ErrorEvent("server")` while preserving partial text already emitted. Mid-flight cancellation via `CancellationToken` exits cleanly without an error event.

### 5.4 `CitationSearch` (RAG-lite — ADR-0051)

Jaccard token-overlap scoring against two stores:
- `Resources` table — `TitleEn` / `TitleAr`
- `KnowledgeMapNodes` table — `NameEn` / `NameAr`

Returns up to 1 citation per kind per turn (1 resource + 1 map node max).

Tokenizer rules:
- Lowercase ASCII letters + digits
- Drop tokens shorter than 3 chars
- Drop a small bilingual stop-word list (English: `the, and, for, with, …`; Arabic: `في, من, على, عن, …`)

Scoring:
```
score = |query_tokens ∩ row_tokens| / |query_tokens ∪ row_tokens|
```
Highest non-zero score wins. Ties broken by insertion order.

Hrefs:
- Resource → `/knowledge-center/resources/{id}`
- Map node → `/knowledge-maps/{mapId}?node={nodeId}`

**Limitations**: misses purely-semantic queries ("How do I reduce emissions?" doesn't match "Energy Efficiency" without shared tokens). Embedding-based RAG is a future sub-project gated by catalog size.

### 5.5 Stub `SmartAssistantClient`

Used in dev / CI / no-key environments. Reads seeded `Resources` and `KnowledgeMapNodes`, splits an echo string into 8 ~3-word chunks delayed 150 ms each, emits 1-2 citations mid-stream, then `done`. Exercises the full frontend code path without touching the network.

### 5.6 Streaming + MediatR

The `/api/assistant/query` endpoint bypasses MediatR's pipeline because MediatR's `IRequest<TResponse>` is single-response Task-shaped, incompatible with streaming. The endpoint resolves `ISmartAssistantClient` directly and pipes its `IAsyncEnumerable<SseEvent>` through `SseWriter`. `AskAssistantCommand` + validator are still present as typed input boundaries but the handler is not used in the streaming path.

---

## 6. Observability

Prometheus metrics emitted by `AnthropicSmartAssistantClient`:

| Metric | Type | Labels | Use |
|---|---|---|---|
| `cce_assistant_streams_runtime_total` | Counter | `provider` (`anthropic`) | Total streams initiated |
| `cce_assistant_citations_runtime_total` | Counter | `kind` (`resource`, `map-node`) | Citation emission rate |

Structured logs:
- `warn` — citation search failure (continues without citations)
- `error` — stream open failure or mid-flight SDK exception (with exception details, before yielding `ErrorEvent`)
- `warn` (bootstrap stderr) — `Assistant:Provider=anthropic` with missing API key, falling back to stub

Sentry DSN (`SENTRY_DSN` env) is wired at the host level for exception capture.

---

## 7. Security & privacy

| Concern | Treatment |
|---|---|
| Authentication | Endpoint is `AllowAnonymous`. BFF cookies are forwarded but not required; user identity is not used by the assistant. |
| API key handling | `ANTHROPIC_API_KEY` is loaded once at startup from the host env (or IConfiguration). Never logged, never echoed in responses. Held in process memory only. |
| Outbound network | Allow-list: `api.anthropic.com:443`. The deploy host's firewall permits outbound TLS to that endpoint. |
| Input limits | Per validator: ≤ 50 messages per request, each ≤ 4 000 chars. Hard ceiling caps abusive payloads. |
| Output limits | `MaxTokens: 1024` capped server-side. |
| PII | The CCE platform's BRD instructs the assistant to discuss circular carbon economy topics. No user-PII collected by the endpoint beyond message content. Anthropic's data-handling policy applies — no data is sent for model training under the standard API tier. |
| Logging | Message content is **not** logged. Only counters + structured error metadata. |
| Rate limiting | Inherits the API gateway's global rate limit. No per-conversation quota in v0.x. |
| CORS | Same-origin only (BFF model). The frontend reaches `/api/assistant/query` through the portal's reverse proxy on the same origin. |
| Transport | TLS 1.2+ enforced by the API gateway / ingress. SSE frames flow over the same HTTPS connection. |
| Secret rotation | Documented in `docs/runbooks/secret-rotation.md`. `ANTHROPIC_API_KEY` rotates semi-annually or on compromise: create new key → update `.env.<env>` → `validate-env.ps1` → `deploy.ps1` → revoke old key. |

---

## 8. Technology stack

### 8.1 Frontend (Angular 19)

| Layer | Technology | Version |
|---|---|---|
| Framework | Angular standalone components | 19.2.21 |
| State | Angular Signals (`signal`, `computed`) | built-in |
| Forms | Reactive Forms | built-in |
| Material UI | Angular Material | 18 |
| i18n | ngx-translate | 16.x |
| Transport | Native `fetch` + `ReadableStream` | browser |
| Build | Nx workspace + Angular CLI | Nx 21 |
| Tests | Jest | 30 (workspace) / 29-compat config |
| E2E | Playwright | latest stable |
| TypeScript | strict mode | 5.x |

No new heavy dependencies introduced for the assistant; the `/assistant` lazy chunk stays light. No `eventsource` polyfill (native `fetch` covers POST + abort + cookies).

### 8.2 Backend (.NET 8)

| Layer | Technology | Version |
|---|---|---|
| Runtime | .NET ASP.NET Core | 8 |
| LLM SDK | `Anthropic.SDK` (community NuGet) | 5.0.0 (pinned in `Directory.Packages.props`) |
| Validation | FluentValidation | latest stable |
| ORM | Entity Framework Core (for `Resources` + `KnowledgeMapNodes` lookups) | 8 |
| DB engine | SQL Server (or Postgres in some envs) | per `Infrastructure__SqlConnectionString` |
| Metrics | `prometheus-net` | latest stable |
| Logging | Serilog | latest stable |
| Streaming | `IAsyncEnumerable<T>` + `await foreach` | built-in |
| HTTP | Minimal APIs (`MapPost`) + `HttpResponse.Body.WriteAsync` | built-in |
| Tests | xUnit + `WebApplicationFactory` integration tests | xUnit 2 |

### 8.3 LLM provider

**Production**: Anthropic Claude Sonnet 4.5 (model id `claude-sonnet-4-5-20250929`) via Anthropic's hosted API.

Configuration knobs (`Assistant:Anthropic`):
- `Model`: `claude-sonnet-4-5-20250929` (default; override via config)
- `MaxTokens`: 1024
- `Temperature`: 0.3

Provider can be swapped in one class (`AnthropicSmartAssistantClient`) — see ADR-0051. The `IAsyncEnumerable<SseEvent>` interface stays stable across vendor swaps.

---

## 9. Architecture diagram

```
┌────────────────────────────────────────────────────────────────────┐
│  Browser (Angular web-portal)                                      │
│                                                                    │
│  AssistantPage / AssistantDialog                                   │
│     │                                                              │
│     ├─→ AssistantStore  (signals: messages, streaming)             │
│     │       │                                                      │
│     │       └─→ AssistantApiService.query(req, AbortSignal)        │
│     │                │                                             │
│     │                └─→ openSseStream('/api/assistant/query', …)  │
│     │                        │ fetch + ReadableStream              │
│     ├─→ MessageList                                                │
│     ├─→ ComposeBox     (Enter sends, Shift+Enter newline)          │
│     └─→ CitationChip   (routerLink → /knowledge-center / -maps)    │
└──────────────────────────────────┬─────────────────────────────────┘
                                   │ HTTPS POST (BFF cookie, same-origin)
                                   ▼
┌────────────────────────────────────────────────────────────────────┐
│  CCE.Api.External  (.NET 8 minimal API, container)                 │
│                                                                    │
│  POST /api/assistant/query                                         │
│       ├── validates body (AskAssistantCommandValidator)            │
│       └── ISmartAssistantClient.StreamAsync(messages, locale, ct)  │
│              │                                                     │
│              ▼                                                     │
│  AssistantClientFactory  ── (provider config + key)                │
│       │                                                            │
│       ├──→ SmartAssistantClient (stub)                             │
│       │       └─→ EF Core → Resources + KnowledgeMapNodes (mock)   │
│       │                                                            │
│       └──→ AnthropicSmartAssistantClient (production)              │
│              ├─→ Anthropic.SDK  ── HTTPS  ──→ api.anthropic.com    │
│              │       (text_delta stream)                           │
│              └─→ CitationSearch  ── EF Core ──→ SQL                │
│                                                                    │
│  SseWriter  ── writes `data: {json}\n\n` frames to HttpResponse    │
└─────────────────────┬──────────────────────────────────────────────┘
                      │ text/event-stream
                      ▼
                  Browser
```

---

## 10. Infrastructure

### 10.1 Container images

The assistant ships inside the existing `cce-api-external` container — there is **no separate assistant service**. The frontend ships inside `cce-web-portal`.

| Image | Built from | Hosts |
|---|---|---|
| `ghcr.io/moenergy-cce/cce-api-external:<tag>` | `backend/src/CCE.Api.External/Dockerfile` | `POST /api/assistant/query` + every other public API endpoint |
| `ghcr.io/moenergy-cce/cce-web-portal:<tag>` | `frontend/apps/web-portal/Dockerfile` | `/assistant` route + entire portal SPA |
| `ghcr.io/moenergy-cce/cce-migrator:<tag>` | `backend/src/CCE.Seeder/Dockerfile` | EF Core migrations + reference-data seed (Resources + KnowledgeMapNodes the citation search reads) |

The runtime image:
- Base: `mcr.microsoft.com/dotnet/aspnet:8.0` (with non-root `app` user uid 1654)
- Ports: `8080` (HTTP)
- Health check: `curl -fsS http://localhost:8080/health`

### 10.2 Compose stack (`docker-compose.prod.yml`)

The api-external service environment includes:

```yaml
api-external:
  image: ghcr.io/${CCE_REGISTRY_OWNER}/cce-api-external:${CCE_IMAGE_TAG}
  env_file: ${CCE_ENV_FILE}
  environment:
    ASPNETCORE_ENVIRONMENT: Production
    ASSISTANT_PROVIDER:     ${ASSISTANT_PROVIDER:-stub}        # 'stub' | 'anthropic'
    ANTHROPIC_API_KEY:      ${ANTHROPIC_API_KEY:-}
    SENTRY_DSN:             ${SENTRY_DSN:-}
    LOG_LEVEL:              ${LOG_LEVEL:-Information}
    Keycloak__Authority:    ${KEYCLOAK_AUTHORITY}
    Keycloak__Audience:     ${KEYCLOAK_AUDIENCE}
    Keycloak__RequireHttpsMetadata: ${KEYCLOAK_REQUIRE_HTTPS}
    Infrastructure__SqlConnectionString: ${INFRA_SQL}
    Infrastructure__RedisConnectionString: ${INFRA_REDIS}
  depends_on: { migrator: { condition: service_completed_successfully } }
  ports: ["5001:8080"]
```

Notes:
- `ASSISTANT_PROVIDER` and `ANTHROPIC_API_KEY` are the only assistant-specific environment knobs.
- The migrator must run to completion before `api-external` starts so the citation source has tables to query.
- SQL + Redis + Keycloak / Entra ID are externally-resident on the Windows host (see IDD v1.1).

### 10.3 Environment matrix

From `deploy/promote-env.ps1`:

| Environment | `ASSISTANT_PROVIDER` | API key source |
|---|---|---|
| `dev` | `stub` | — |
| `test` | `anthropic` | shared sandbox key (CI Vault secret) |
| `preprod` | `anthropic` | preprod key (operator-rotated) |
| `prod` | `anthropic` | prod key (operator-rotated semi-annually) |

### 10.4 Backend `appsettings.json` defaults

```json
{
  "Assistant": {
    "Provider": "stub",
    "Anthropic": {
      "Model": "claude-sonnet-4-5-20250929",
      "MaxTokens": 1024,
      "Temperature": 0.3
    }
  }
}
```

Environment variables override these at runtime via .NET's standard binding (`ASSISTANT__PROVIDER`, `ASSISTANT__ANTHROPIC__MODEL`, etc.).

---

## 11. Deployment workflow

### 11.1 Build & publish (CI — GitHub Actions)

1. PR merges to `main` → `build-images` workflow runs.
2. Each container is built via its Dockerfile and tagged with the short commit SHA.
3. Images push to GHCR: `ghcr.io/moenergy-cce/{cce-api-external,cce-web-portal,cce-migrator,cce-admin-cms}:<sha>`.
4. Smoke pipeline (`.github/workflows/deploy-smoke.yml`) brings the compose stack up against a clean SQL container, runs integration tests (including `AssistantEndpointTests`), and reports green/red on the PR.

### 11.2 Promote to an environment (operator on Windows host)

The deploy automation lives in `deploy/`. The full flow:

```powershell
# 1. Validate the target env file exists, has all required keys, and
#    the provider/key pair is internally consistent.
./validate-env.ps1 -Env preprod
# Fails fast if ASSISTANT_PROVIDER=anthropic without ANTHROPIC_API_KEY.

# 2. Promote env-specific values (e.g. flip provider from stub to anthropic
#    when moving dev → test).
./promote-env.ps1 -From test -To preprod

# 3. Deploy: pull images, run migrator, bring up apps.
./deploy.ps1 -Env preprod -Tag <commit-sha>
# Validates: ANTHROPIC_API_KEY is required when ASSISTANT_PROVIDER=anthropic.

# 4. Smoke test the assistant endpoint (sample query, verify real response).
./smoke.ps1 -Env preprod

# 5. Roll back if needed.
./rollback.ps1 -Env preprod -Tag <previous-sha>
```

The deploy script:
- Pulls images by tag from GHCR
- Runs `docker compose -f docker-compose.prod.yml -f docker-compose.prod.deploy.yml run --rm --no-deps migrator` first
- Then `docker compose ... up -d --no-deps api-external api-internal web-portal admin-cms`
- Tails logs for 60 s and watches health checks
- Aborts if any container reports unhealthy

### 11.3 Configuration files

- `.env.dev`, `.env.test`, `.env.preprod`, `.env.prod` — per-env config files, **not** in git. Stored on the deploy host in `C:\cce\envs\` with NTFS ACLs limiting read to the deploy service account.
- `docker-compose.prod.deploy.yml` — overlay used in production that strips fallback defaults so missing required env vars fail loud.

### 11.4 Health & readiness

- `/health` — liveness probe (returns 200 once the app starts).
- The assistant endpoint itself is not part of the health gate (depends on external Anthropic API).
- Operators can manually check liveness with `./smoke.ps1` which posts a minimal query and asserts a 200 + at least one `text` event before timing out.

### 11.5 Observability deployment

- Prometheus scrapes `/metrics` on `api-external` (port 8080).
- Grafana dashboards (provisioned in `deploy/grafana-dashboards/`) include an Assistant panel showing streams started, first-chunk latency, completion rate, citation rate.
- Sentry receives unhandled exceptions when `SENTRY_DSN` is set.

---

## 12. Operational runbook

### 12.1 Rotate the Anthropic API key

(Excerpt — see `docs/runbooks/secret-rotation.md` for the full procedure.)

1. Anthropic console → API keys → **Create a new key**.
2. Update `ANTHROPIC_API_KEY` in `.env.<env>` on the deploy host.
3. `./validate-env.ps1 -Env <env>` → confirm validation passes.
4. `./deploy.ps1 -Env <env> -Tag <current-tag>` → redeploy with the new key.
5. Smoke-test: hit the assistant, confirm a real Claude reply (not the stub's deterministic echo).
6. Anthropic console → **Revoke the old key**.

Cadence: semi-annually or immediately on compromise.

### 12.2 Switch back to the stub (incident mitigation)

If the Anthropic API is degraded or the key is compromised and rotation will take time:

1. Set `ASSISTANT_PROVIDER=stub` in `.env.<env>`.
2. `./deploy.ps1 -Env <env> -Tag <current-tag>`.
3. Users now see the deterministic stub response (still bilingual, still cites real resources, but obviously not a real LLM answer).
4. After mitigation, set provider back to `anthropic` and redeploy.

### 12.3 Switch LLM providers

Provider swap is a one-class change:

1. Implement a new `ISmartAssistantClient` (e.g. `OpenAiSmartAssistantClient`).
2. Extend `AssistantClientFactory` with a new branch.
3. Add a new `Assistant:Provider` value.
4. Deploy.

The frontend and the wire protocol are unchanged.

---

## 13. Testing

| Layer | Suite | Count (as of `app-v1.0.0`) |
|---|---|---|
| Frontend unit (Jest, web-portal) | 90 suites | 499 tests |
| Frontend SSE parser | `lib/sse-client.spec.ts` | 7 tests covering buffer-split / abort / malformed / open-failure paths |
| Frontend store | `assistant-store.service.spec.ts` | 13 tests covering sendMessage, cancel, retry, regenerate, clear, error mapping |
| Frontend message bubble | `message-bubble.component.spec.ts` | 9 tests covering streaming cursor, copy/retry/regenerate visibility |
| Frontend message list | `message-list.component.spec.ts` | 5 tests covering auto-scroll, aria-live, typing indicator |
| Frontend compose box | `compose-box.component.spec.ts` | 9 tests covering Enter / Shift+Enter, send/cancel morph, char-count |
| Frontend citation chip | `citation-chip.component.spec.ts` | 7 tests covering kind icons, tooltips, routerLink |
| Backend application | `Application.Tests` | 433 tests (includes 9 new validator tests + 4 SseEvent serialization tests for Sub-9) |
| Backend integration | `AssistantEndpointTests` | 2 tests covering happy path + validator rejection |
| Backend Anthropic client | `Infrastructure.Tests/Assistant/*` | Coverage of stream wrapping, partial-content preservation, citation attach failure tolerance |

---

## 14. Architectural Decision Records (ADRs)

| ADR | Title | Status |
|---|---|---|
| [ADR-0049](adr/0049-sse-structured-citation-events.md) | SSE + structured citation events | Accepted |
| [ADR-0050](adr/0050-client-owned-in-memory-thread.md) | Client-owned in-memory thread state | Accepted |
| [ADR-0051](adr/0051-anthropic-sdk-rag-lite-citations.md) | Anthropic.SDK + RAG-lite citations | Accepted |

---

## 15. Roadmap (carried-forward backlog)

| Item | Trigger | Likely sub-project |
|---|---|---|
| Server-persisted threads | Identity + retention policy approved | Future "Sub-N — Assistant memory" |
| Embedding-based RAG | Catalog grows past ~50 docs OR semantic-relevance complaints in feedback | Future "Sub-N — Embedding citations" |
| Markdown rendering | System prompt asks for markdown + sanitizer wired | Minor follow-up |
| Multi-thread sidebar | Once threads persist | Same as memory sub-project |
| Tool use (function calling) | When we have stable platform tools to expose (e.g. "get country profile", "list events") | Stretch |
| Voice input / TTS | Mobile app priority | Out of scope for portal |
| Per-user / per-IP rate limits | Abuse signals from production | Ops follow-up |
| axe-core a11y CI gate | Sub-10's Lighthouse audit lands | Sub-10 follow-up |

---

## 16. Glossary

| Term | Meaning |
|---|---|
| **SSE** | Server-Sent Events — a one-way streaming HTTP response with `Content-Type: text/event-stream` |
| **RAG** | Retrieval-Augmented Generation — augmenting an LLM response with retrieved knowledge-base content |
| **RAG-lite** | The token-overlap (Jaccard) lookup used here in lieu of vector embeddings |
| **BFF** | Backend-for-Frontend — the same-origin cookie auth pattern the portal uses |
| **Thread** | A single conversation (a list of user/assistant messages) |
| **Stub** | The `SmartAssistantClient` deterministic fake-streamer used in dev / CI |
| **Citation kind** | `resource` (a Knowledge Center entry) or `map-node` (a Knowledge Map node) |

# Phase 00 — Cross-cutting (Sub-9)

> Parent: [`../2026-05-02-sub-9.md`](../2026-05-02-sub-9.md) · Spec: [`../../specs/2026-05-02-sub-9-design.md`](../../specs/2026-05-02-sub-9-design.md) §3 (data contracts), §9 (i18n), §11 (backend details)

**Phase goal:** Lay foundations without changing user-visible behaviour. Add the new TypeScript types, ship a tested `lib/sse-client.ts` that parses an `AsyncIterable<SseEvent>` from a `fetch`+`ReadableStream`, declare the matching backend C# `SseEvent` records + a write-helper, populate `assistant.*` i18n keys, and pre-create the file structure each later phase will fill in. Phase 01 starts the backend endpoint reshape.

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Sub-8 closed (`web-portal-v0.3.0` tag exists; main at the post-Sub-8 commit or later).
- web-portal Jest baseline: 83 suites · 445 tests passing; lint + build clean.
- Backend `dotnet test` passes against `main`.

---

## Task 0.1: Extended frontend types

**Files:**
- Modify: `frontend/apps/web-portal/src/app/features/assistant/assistant.types.ts`.
- Create: `frontend/apps/web-portal/src/app/features/assistant/assistant.types.spec.ts`.

**Final state of `assistant.types.ts`** (replace existing contents — file currently doesn't exist as a dedicated module; the types are inlined in `assistant-api.service.ts`. This task **creates** the dedicated types file):

```ts
/**
 * Mirrors backend DTOs from CCE.Application.Assistant.* and the SSE
 * wire format from /api/assistant/query (Sub-9 reshape). Threads are
 * client-owned in-memory state — no persistence layer in v0.1.0.
 */

export type Role = 'user' | 'assistant';

export interface Citation {
  id: string;
  kind: 'resource' | 'map-node';
  title: string;
  href: string;
  sourceText?: string;
}

export interface ThreadMessage {
  id: string;
  role: Role;
  content: string;
  citations: Citation[];
  status: 'pending' | 'streaming' | 'complete' | 'error';
  errorKind?: string;
  /** ISO 8601 timestamp set client-side at message creation. */
  createdAt: string;
}

/** Wire-format request body for POST /api/assistant/query. */
export interface AssistantQueryRequest {
  messages: { role: Role; content: string }[];
  locale: 'ar' | 'en';
}

/** SSE event discriminated union. The wire format is `data: <json>\n\n`
 *  per event; the parser maps each event into one of these. */
export type SseEvent =
  | { type: 'text'; content: string }
  | { type: 'citation'; citation: Citation }
  | { type: 'done' }
  | { type: 'error'; error: { kind: string } };

/** Helper to build a ThreadMessage with sensible defaults. Used by the
 *  store so tests don't have to repeat the same boilerplate. */
export function newMessage(role: Role, content: string): ThreadMessage {
  return {
    id: crypto.randomUUID(),
    role,
    content,
    citations: [],
    status: role === 'user' ? 'complete' : 'pending',
    createdAt: new Date().toISOString(),
  };
}
```

**`assistant.types.spec.ts`:**

```ts
import { newMessage, type ThreadMessage } from './assistant.types';

describe('assistant types helpers', () => {
  it('newMessage("user", text) produces a complete user message', () => {
    const m: ThreadMessage = newMessage('user', 'hello');
    expect(m.role).toBe('user');
    expect(m.content).toBe('hello');
    expect(m.status).toBe('complete');
    expect(m.citations).toEqual([]);
    expect(m.errorKind).toBeUndefined();
  });

  it('newMessage("assistant", "") produces a pending assistant placeholder', () => {
    const m: ThreadMessage = newMessage('assistant', '');
    expect(m.role).toBe('assistant');
    expect(m.content).toBe('');
    expect(m.status).toBe('pending');
  });

  it('newMessage assigns a unique id per call', () => {
    const a = newMessage('user', 'x');
    const b = newMessage('user', 'x');
    expect(a.id).not.toBe(b.id);
  });

  it('newMessage stamps an ISO 8601 createdAt', () => {
    const m = newMessage('user', 'x');
    expect(() => new Date(m.createdAt).toISOString()).not.toThrow();
    expect(m.createdAt.endsWith('Z')).toBe(true);
  });
});
```

- [ ] **Step 1: Create the spec** with the contents above (file does not exist yet).
- [ ] **Step 2: Run it** to verify it fails (module-not-found):
  ```bash
  cd frontend && ./node_modules/.bin/nx test web-portal --watch=false --testPathPattern=assistant.types.spec
  ```
  Expected: failure (the module doesn't exist as a dedicated file yet — types are inlined in `assistant-api.service.ts`).

- [ ] **Step 3: Create `assistant.types.ts`** with the contents above.

- [ ] **Step 4: Run tests** — expected: 4 passing.

- [ ] **Step 5: Commit:**
  ```bash
  git add frontend/apps/web-portal/src/app/features/assistant/assistant.types.ts \
          frontend/apps/web-portal/src/app/features/assistant/assistant.types.spec.ts
  git -c commit.gpgsign=false commit -m "feat(assistant): extract types module + add Sub-9 shapes

  Role, Citation, ThreadMessage, AssistantQueryRequest, SseEvent, and
  newMessage helper. Sub-9 Phase 00.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.2: `lib/sse-client.ts` — fetch+ReadableStream → AsyncIterable<SseEvent>

**Files:**
- Create: `frontend/apps/web-portal/src/app/features/assistant/lib/sse-client.ts`.
- Create: `frontend/apps/web-portal/src/app/features/assistant/lib/sse-client.spec.ts`.

**Why a custom client (not native `EventSource`):** EventSource is GET-only, doesn't accept a request body, doesn't propagate cookies in all browsers, and can't be aborted with an `AbortController`. Sub-9 needs POST + cookie + abort, so we wrap `fetch` + `ReadableStream` ourselves.

**Final state of `sse-client.ts`:**

```ts
import type { SseEvent } from '../assistant.types';

const EVENT_DELIMITER = '\n\n';
const DATA_PREFIX = 'data:';

/**
 * Open a server-sent-events stream against `url` with a JSON POST body
 * and yield typed events as they arrive. Honours `signal` for abort.
 *
 * The transport layer assumes:
 *  - Server responds with Content-Type: text/event-stream.
 *  - Each event is exactly `data: <json>\n\n` (no event-id / event-name fields).
 *  - JSON parses to one of the SseEvent shapes.
 *
 * Malformed events are skipped (not thrown) so a single corrupt frame
 * doesn't kill the stream. The store turns the absence of events into
 * its own error state.
 */
export async function* openSseStream(
  url: string,
  body: unknown,
  signal: AbortSignal,
): AsyncGenerator<SseEvent, void, void> {
  const res = await fetch(url, {
    method: 'POST',
    credentials: 'same-origin',
    headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
    body: JSON.stringify(body),
    signal,
  });

  if (!res.ok || !res.body) {
    throw new Error(`SSE open failed: ${res.status}`);
  }

  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  try {
    while (true) {
      const { value, done } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });

      // Drain complete events (separated by \n\n).
      let delimiterIdx = buffer.indexOf(EVENT_DELIMITER);
      while (delimiterIdx !== -1) {
        const rawEvent = buffer.slice(0, delimiterIdx);
        buffer = buffer.slice(delimiterIdx + EVENT_DELIMITER.length);

        const parsed = parseEvent(rawEvent);
        if (parsed) yield parsed;

        delimiterIdx = buffer.indexOf(EVENT_DELIMITER);
      }
    }
  } finally {
    try {
      reader.releaseLock();
    } catch {
      // ignore
    }
  }
}

function parseEvent(raw: string): SseEvent | null {
  // An event may have multiple lines; we only honour `data:` lines.
  const dataLines: string[] = [];
  for (const line of raw.split('\n')) {
    if (line.startsWith(DATA_PREFIX)) {
      dataLines.push(line.slice(DATA_PREFIX.length).trimStart());
    }
  }
  if (dataLines.length === 0) return null;
  const json = dataLines.join('\n');
  try {
    const parsed: unknown = JSON.parse(json);
    if (isValidEvent(parsed)) return parsed;
  } catch {
    return null;
  }
  return null;
}

function isValidEvent(x: unknown): x is SseEvent {
  if (!x || typeof x !== 'object') return false;
  const t = (x as { type?: unknown }).type;
  return t === 'text' || t === 'citation' || t === 'done' || t === 'error';
}
```

**Final state of `sse-client.spec.ts`** (drives a fake `Response` body via `ReadableStream.from()` to exercise the parser end-to-end):

```ts
import { openSseStream } from './sse-client';
import type { SseEvent } from '../assistant.types';

function makeResponse(chunks: string[]): Response {
  const stream = new ReadableStream<Uint8Array>({
    start(controller) {
      const enc = new TextEncoder();
      for (const c of chunks) controller.enqueue(enc.encode(c));
      controller.close();
    },
  });
  return new Response(stream, {
    status: 200,
    headers: { 'Content-Type': 'text/event-stream' },
  });
}

describe('openSseStream', () => {
  let originalFetch: typeof globalThis.fetch;
  beforeEach(() => {
    originalFetch = globalThis.fetch;
  });
  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it('yields events delimited by \\n\\n', async () => {
    globalThis.fetch = jest.fn().mockResolvedValue(
      makeResponse([
        'data: {"type":"text","content":"Hello "}\n\n',
        'data: {"type":"text","content":"world"}\n\n',
        'data: {"type":"done"}\n\n',
      ]),
    );

    const events: SseEvent[] = [];
    for await (const e of openSseStream('/x', {}, new AbortController().signal)) {
      events.push(e);
    }
    expect(events).toEqual([
      { type: 'text', content: 'Hello ' },
      { type: 'text', content: 'world' },
      { type: 'done' },
    ]);
  });

  it('reassembles events split across chunk boundaries', async () => {
    // Single event arriving in 3 small chunks.
    globalThis.fetch = jest.fn().mockResolvedValue(
      makeResponse([
        'data: {"type":"te',
        'xt","content":"split"}\n',
        '\n',
      ]),
    );
    const events: SseEvent[] = [];
    for await (const e of openSseStream('/x', {}, new AbortController().signal)) {
      events.push(e);
    }
    expect(events).toEqual([{ type: 'text', content: 'split' }]);
  });

  it('skips malformed JSON frames without aborting the stream', async () => {
    globalThis.fetch = jest.fn().mockResolvedValue(
      makeResponse([
        'data: {bad json\n\n',
        'data: {"type":"text","content":"ok"}\n\n',
      ]),
    );
    const events: SseEvent[] = [];
    for await (const e of openSseStream('/x', {}, new AbortController().signal)) {
      events.push(e);
    }
    expect(events).toEqual([{ type: 'text', content: 'ok' }]);
  });

  it('skips frames with unknown event type', async () => {
    globalThis.fetch = jest.fn().mockResolvedValue(
      makeResponse([
        'data: {"type":"unknown","stuff":1}\n\n',
        'data: {"type":"text","content":"ok"}\n\n',
      ]),
    );
    const events: SseEvent[] = [];
    for await (const e of openSseStream('/x', {}, new AbortController().signal)) {
      events.push(e);
    }
    expect(events).toEqual([{ type: 'text', content: 'ok' }]);
  });

  it('throws when fetch responds with a non-2xx status', async () => {
    globalThis.fetch = jest.fn().mockResolvedValue(
      new Response('boom', { status: 500 }),
    );
    await expect(async () => {
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      for await (const _ of openSseStream('/x', {}, new AbortController().signal)) {
        // noop
      }
    }).rejects.toThrow(/SSE open failed: 500/);
  });

  it('parses citation events with their nested payload', async () => {
    globalThis.fetch = jest.fn().mockResolvedValue(
      makeResponse([
        'data: {"type":"citation","citation":{"id":"r1","kind":"resource","title":"T","href":"/x"}}\n\n',
        'data: {"type":"done"}\n\n',
      ]),
    );
    const events: SseEvent[] = [];
    for await (const e of openSseStream('/x', {}, new AbortController().signal)) {
      events.push(e);
    }
    expect(events[0]).toEqual({
      type: 'citation',
      citation: { id: 'r1', kind: 'resource', title: 'T', href: '/x' },
    });
  });

  it('passes the JSON-encoded body and SSE Accept header to fetch', async () => {
    const fetchMock = jest.fn().mockResolvedValue(
      makeResponse(['data: {"type":"done"}\n\n']),
    );
    globalThis.fetch = fetchMock;
    const body = { messages: [{ role: 'user', content: 'hi' }], locale: 'en' };
    for await (const _ of openSseStream('/api/assistant/query', body, new AbortController().signal)) {
      // drain
    }
    expect(fetchMock).toHaveBeenCalledWith('/api/assistant/query', expect.objectContaining({
      method: 'POST',
      credentials: 'same-origin',
      body: JSON.stringify(body),
    }));
    const opts = fetchMock.mock.calls[0][1] as RequestInit;
    const headers = opts.headers as Record<string, string>;
    expect(headers['Content-Type']).toBe('application/json');
    expect(headers['Accept']).toBe('text/event-stream');
  });
});
```

- [ ] **Step 1: Create both files** with the contents above.
- [ ] **Step 2: Run tests:**
  ```bash
  cd frontend && ./node_modules/.bin/nx test web-portal --watch=false --testPathPattern=sse-client.spec
  ```
  Expected: 7 passing tests. (TS may fail first if it can't find `crypto.randomUUID` etc. — re-run after `tsc` resolves; the spec only depends on `fetch` and `Response` which are first-class in jest-environment-jsdom v22+.)

- [ ] **Step 3: Commit:**
  ```bash
  git add frontend/apps/web-portal/src/app/features/assistant/lib/
  git -c commit.gpgsign=false commit -m "feat(assistant): SSE client for fetch+ReadableStream streams

  openSseStream(url, body, signal) — POST-capable, BFF-cookie-friendly,
  AbortController-aware async iterator. Buffers events across chunk
  boundaries; skips malformed JSON / unknown event types rather than
  failing the stream. Sub-9 Phase 00.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.3: Backend SSE event records + write-helper (no endpoint change yet)

**Files:**
- Create: `backend/src/CCE.Application/Assistant/SseEvent.cs` — discriminated-union-style record types.
- Create: `backend/src/CCE.Application/Assistant/CitationDto.cs` — DTO matching the frontend `Citation` shape.
- Create: `backend/src/CCE.Api.External/Endpoints/SseWriter.cs` — extension method `Results.ServerSentEvents(IAsyncEnumerable<SseEvent>, ct)` that writes the events to the response stream.

**`SseEvent.cs`:**

```cs
using System.Text.Json.Serialization;

namespace CCE.Application.Assistant;

/// <summary>
/// Discriminated union of SSE events emitted by the assistant stream.
/// JSON discriminator is `type` to match the frontend SseEvent shape.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextEvent), typeDiscriminator: "text")]
[JsonDerivedType(typeof(CitationEvent), typeDiscriminator: "citation")]
[JsonDerivedType(typeof(DoneEvent), typeDiscriminator: "done")]
[JsonDerivedType(typeof(ErrorEvent), typeDiscriminator: "error")]
public abstract record SseEvent;

public sealed record TextEvent(string Content) : SseEvent;

public sealed record CitationEvent(CitationDto Citation) : SseEvent;

public sealed record DoneEvent : SseEvent;

public sealed record ErrorEvent(ErrorPayload Error) : SseEvent;

public sealed record ErrorPayload(string Kind);
```

**`CitationDto.cs`:**

```cs
namespace CCE.Application.Assistant;

public sealed record CitationDto(
    string Id,
    string Kind,
    string Title,
    string Href,
    string? SourceText);
```

**`SseWriter.cs`:**

```cs
using System.Text;
using System.Text.Json;
using CCE.Application.Assistant;
using Microsoft.AspNetCore.Http;

namespace CCE.Api.External.Endpoints;

/// <summary>
/// Helper for writing IAsyncEnumerable&lt;SseEvent&gt; to an HTTP response
/// as text/event-stream. Each event is emitted as `data: {json}\n\n`.
/// </summary>
public static class SseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // Enums-as-strings is wired globally by ConfigureHttpJsonOptions
        // (Sub-7 ship-readiness fix); we re-apply in case this writer is
        // ever used outside the standard pipeline.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task WriteAsync(
        HttpResponse response,
        IAsyncEnumerable<SseEvent> events,
        CancellationToken ct)
    {
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no"; // disable proxy buffering

        await foreach (var ev in events.WithCancellation(ct))
        {
            var json = JsonSerializer.Serialize<SseEvent>(ev, JsonOptions);
            var frame = Encoding.UTF8.GetBytes($"data: {json}\n\n");
            await response.Body.WriteAsync(frame, ct).ConfigureAwait(false);
            await response.Body.FlushAsync(ct).ConfigureAwait(false);
        }
    }
}
```

**Test:** create `backend/tests/CCE.Application.Tests/Assistant/SseEventSerializationTests.cs`:

```cs
using System.Text.Json;
using CCE.Application.Assistant;
using FluentAssertions;
using Xunit;

namespace CCE.Application.Tests.Assistant;

public class SseEventSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void TextEvent_serializes_to_camelCase_with_type_discriminator()
    {
        SseEvent ev = new TextEvent("Hello");
        var json = JsonSerializer.Serialize(ev, JsonOptions);
        json.Should().Be("""{"type":"text","content":"Hello"}""");
    }

    [Fact]
    public void CitationEvent_serializes_with_nested_citation_payload()
    {
        SseEvent ev = new CitationEvent(new CitationDto(
            Id: "r1", Kind: "resource", Title: "T", Href: "/x", SourceText: null));
        var json = JsonSerializer.Serialize(ev, JsonOptions);
        json.Should().Be(
            """{"type":"citation","citation":{"id":"r1","kind":"resource","title":"T","href":"/x","sourceText":null}}""");
    }

    [Fact]
    public void DoneEvent_serializes_to_just_a_type_field()
    {
        SseEvent ev = new DoneEvent();
        var json = JsonSerializer.Serialize(ev, JsonOptions);
        json.Should().Be("""{"type":"done"}""");
    }

    [Fact]
    public void ErrorEvent_serializes_with_error_payload()
    {
        SseEvent ev = new ErrorEvent(new ErrorPayload("network"));
        var json = JsonSerializer.Serialize(ev, JsonOptions);
        json.Should().Be("""{"type":"error","error":{"kind":"network"}}""");
    }
}
```

- [ ] **Step 1: Create the test file** with the contents above.

- [ ] **Step 2: Run dotnet test** to confirm the suite fails (the new types don't exist):
  ```bash
  cd backend && dotnet test --filter FullyQualifiedName~SseEventSerializationTests
  ```

- [ ] **Step 3: Create the three production files** (`SseEvent.cs`, `CitationDto.cs`, `SseWriter.cs`).

- [ ] **Step 4: Re-run dotnet test** — expected: 4 passing.

- [ ] **Step 5: Commit:**
  ```bash
  git add backend/src/CCE.Application/Assistant/SseEvent.cs \
          backend/src/CCE.Application/Assistant/CitationDto.cs \
          backend/src/CCE.Api.External/Endpoints/SseWriter.cs \
          backend/tests/CCE.Application.Tests/Assistant/SseEventSerializationTests.cs
  git -c commit.gpgsign=false commit -m "feat(assistant): backend SseEvent types + SseWriter helper

  Discriminated-union records (TextEvent, CitationEvent, DoneEvent,
  ErrorEvent) JSON-serialize to {type, ...payload} matching the
  frontend SseEvent shape. SseWriter.WriteAsync streams an
  IAsyncEnumerable<SseEvent> to an HTTP response as text/event-stream.
  Phase 01 wires this into /api/assistant/query. Sub-9 Phase 00.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.4: i18n keys (EN + AR)

**Files:**
- Modify: `frontend/libs/i18n/src/lib/i18n/en.json`.
- Modify: `frontend/libs/i18n/src/lib/i18n/ar.json`.

**Purpose:** Every string Sub-9 will reference lives under `assistant.*` before any component touches them.

**Add to BOTH `en.json` and `ar.json`** under the existing top-level `"assistant"` key. The Sub-6 Phase 9 stub put a small block there — replace it wholesale with the structure below.

**EN — `assistant` section:**
```json
"assistant": {
  "title": "Smart Assistant",
  "subtitle": "Ask anything about the Circular Carbon Economy.",
  "empty": "Start by asking a question — your conversation will appear here.",
  "hint": "The assistant cites sources from the knowledge center and knowledge maps.",
  "compose": {
    "placeholder": "Ask a question…",
    "send": "Send",
    "sendLabel": "Send message",
    "cancel": "Stop",
    "cancelLabel": "Stop generating response",
    "charCount": "{{count}} / {{max}}",
    "charLimitWarning": "Approaching the character limit."
  },
  "message": {
    "user": "You",
    "assistant": "Assistant",
    "copy": "Copy",
    "copied": "Copied",
    "retry": "Retry",
    "regenerate": "Regenerate"
  },
  "citations": {
    "title": "Sources",
    "sourceText": "Source excerpt",
    "resourceKind": "Resource",
    "mapNodeKind": "Knowledge map"
  },
  "thread": {
    "clear": "Clear conversation",
    "clearConfirmTitle": "Clear this conversation?",
    "clearConfirmBody": "Removes all messages from the current thread. This cannot be undone.",
    "clearConfirm": "Clear",
    "clearCancel": "Keep"
  },
  "errors": {
    "network": "Couldn't reach the assistant. Check your connection.",
    "server": "The assistant ran into a problem.",
    "unknown": "Something went wrong.",
    "retry": "Retry",
    "streamFailed": "The reply was interrupted.",
    "offline": "You're offline. Reconnect to ask the assistant."
  }
}
```

**AR — `assistant` section** (same key shape, Arabic strings):
```json
"assistant": {
  "title": "المساعد الذكي",
  "subtitle": "اسأل أي شيء عن الاقتصاد الكربوني الدائري.",
  "empty": "ابدأ بطرح سؤال — ستظهر المحادثة هنا.",
  "hint": "يستشهد المساعد بمصادر من مركز المعرفة والخرائط المعرفية.",
  "compose": {
    "placeholder": "اطرح سؤالاً…",
    "send": "إرسال",
    "sendLabel": "إرسال الرسالة",
    "cancel": "إيقاف",
    "cancelLabel": "إيقاف توليد الرد",
    "charCount": "{{count}} / {{max}}",
    "charLimitWarning": "تقترب من الحد الأقصى للأحرف."
  },
  "message": {
    "user": "أنت",
    "assistant": "المساعد",
    "copy": "نسخ",
    "copied": "تم النسخ",
    "retry": "إعادة المحاولة",
    "regenerate": "إعادة التوليد"
  },
  "citations": {
    "title": "المصادر",
    "sourceText": "اقتباس المصدر",
    "resourceKind": "مصدر",
    "mapNodeKind": "خريطة معرفية"
  },
  "thread": {
    "clear": "مسح المحادثة",
    "clearConfirmTitle": "هل تريد مسح هذه المحادثة؟",
    "clearConfirmBody": "سيتم إزالة جميع الرسائل من الموضوع الحالي. لا يمكن التراجع عن هذا.",
    "clearConfirm": "مسح",
    "clearCancel": "إبقاء"
  },
  "errors": {
    "network": "تعذّر الوصول إلى المساعد. تحقق من الاتصال.",
    "server": "واجه المساعد مشكلة.",
    "unknown": "حدث خطأ ما.",
    "retry": "إعادة المحاولة",
    "streamFailed": "تمت مقاطعة الرد.",
    "offline": "أنت غير متصل. أعد الاتصال لطرح سؤال على المساعد."
  }
}
```

- [ ] **Step 1: Edit `en.json`** — replace the existing `"assistant"` block with the EN block above (use the `Edit` tool with the existing block as `old_string`).
- [ ] **Step 2: Edit `ar.json`** the same way.
- [ ] **Step 3: Verify both files are valid JSON** + parity:
  ```bash
  cd /Users/m/CCE && python3 -c "
  import json
  def keys(d, prefix=''):
      out = []
      for k, v in d.items():
          full = prefix + ('.' if prefix else '') + k
          if isinstance(v, dict):
              out.extend(keys(v, full))
          else:
              out.append(full)
      return sorted(out)
  en = json.load(open('frontend/libs/i18n/src/lib/i18n/en.json'))
  ar = json.load(open('frontend/libs/i18n/src/lib/i18n/ar.json'))
  en_a = keys(en['assistant'])
  ar_a = keys(ar['assistant'])
  assert set(en_a) == set(ar_a), f'mismatch: {set(en_a) ^ set(ar_a)}'
  print('parity ok —', len(en_a), 'assistant keys')
  "
  ```
  Expected: `parity ok — 35 assistant keys`.

- [ ] **Step 4: Run web-portal tests** to confirm nothing else broke:
  ```bash
  cd frontend && ./node_modules/.bin/nx test web-portal --watch=false
  ```

- [ ] **Step 5: Commit:**
  ```bash
  git add frontend/libs/i18n/src/lib/i18n/en.json frontend/libs/i18n/src/lib/i18n/ar.json
  git -c commit.gpgsign=false commit -m "feat(assistant): add Sub-9 i18n keys (EN + AR)

  assistant.{title, subtitle, empty, hint, compose, message, citations,
  thread, errors} with full RTL Arabic translations. Parity verified.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.5: Component file structure (placeholders)

**Files:**
- Create: `frontend/apps/web-portal/src/app/features/assistant/thread/assistant-store.service.ts` — stub.
- Create: `frontend/apps/web-portal/src/app/features/assistant/thread/message-list.component.{ts,html,scss}` — empty standalone.
- Create: same for `message-bubble`, `compose-box`, `citation-chip`, `typing-indicator`.
- Modify: `frontend/apps/web-portal/src/app/features/assistant/assistant.page.ts` — replace the Sub-6 Phase 9 single-turn page with the Sub-9 page shell stub.
- Modify: `frontend/apps/web-portal/src/app/features/assistant/assistant.page.html` and `.scss` — minimal layout slots.
- Modify: `frontend/apps/web-portal/src/app/features/assistant/assistant.page.spec.ts` — replace existing tests with a minimal "renders title" smoke test (Phase 02 fills this in).
- Modify: `frontend/apps/web-portal/src/app/features/assistant/assistant-api.service.ts` — keep the file path but reshape the method signature to take `AssistantQueryRequest` and return `AsyncIterable<SseEvent>` (Phase 01 fills the body; Phase 00 leaves a stub that throws).
- Modify: `frontend/apps/web-portal/src/app/features/assistant/assistant-api.service.spec.ts` — remove the old single-turn test (the Phase 01 reshape is incompatible).

**Purpose:** Pre-create every file each later phase will fill in. Each placeholder compiles + lints clean.

**Stub `assistant-store.service.ts`:**

```ts
import { Injectable, computed, signal } from '@angular/core';
import type { ThreadMessage } from '../assistant.types';

/**
 * Signals-first state container for the assistant thread. Phase 02 fills
 * in actions (sendMessage / cancel / retry / regenerate / clear). This
 * stub exists so Phase 00 stubs can import the type without circular refs.
 */
@Injectable()
export class AssistantStore {
  readonly messages = signal<ThreadMessage[]>([]);
  readonly streaming = signal<boolean>(false);
  readonly canSend = computed(() => !this.streaming());
}
```

**Stub component template** (apply for each of `message-list`, `message-bubble`, `compose-box`, `citation-chip`, `typing-indicator` — adjust selector and class name):

```ts
// thread/message-list.component.ts (example — repeat for the other 4)
import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'cce-message-list',
  standalone: true,
  imports: [],
  templateUrl: './message-list.component.html',
  styleUrl: './message-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MessageListComponent {}
```

**Stub `.html`:** `<!-- TODO Phase 02 -->`
**Stub `.scss`:** `:host { display: block; }`

Selectors:
- `cce-message-list`
- `cce-message-bubble`
- `cce-compose-box`
- `cce-citation-chip`
- `cce-typing-indicator`

**Page replacement (`assistant.page.ts`):**

```ts
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { AssistantStore } from './thread/assistant-store.service';

/**
 * Top-level page for /assistant. Phase 01 wires the SSE backend; Phase 02
 * fills in the layout slots. Provides AssistantStore so each route
 * activation gets a fresh thread.
 */
@Component({
  selector: 'cce-assistant-page',
  standalone: true,
  imports: [TranslateModule],
  providers: [AssistantStore],
  templateUrl: './assistant.page.html',
  styleUrl: './assistant.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssistantPage {}
```

**`assistant.page.html`:**

```html
<section class="cce-assistant">
  <h1>{{ 'assistant.title' | translate }}</h1>
  <p class="cce-assistant__subtitle">{{ 'assistant.subtitle' | translate }}</p>
  <!-- Phase 02: <cce-message-list /> + <cce-compose-box /> -->
</section>
```

**`assistant.page.scss`:**

```scss
:host { display: block; padding: 1.5rem; max-width: 900px; margin: 0 auto; }

.cce-assistant__subtitle {
  margin: 0.5rem 0 1.5rem 0;
  color: rgba(0, 0, 0, 0.6);
}
```

**`assistant.page.spec.ts`** (minimal — replace existing content):

```ts
import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { AssistantPage } from './assistant.page';

describe('AssistantPage (Phase 00 stub)', () => {
  it('renders title and subtitle from i18n keys', () => {
    TestBed.configureTestingModule({
      imports: [AssistantPage, TranslateModule.forRoot()],
      providers: [provideNoopAnimations()],
    });
    const fixture = TestBed.createComponent(AssistantPage);
    fixture.detectChanges();
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('assistant.title');
    expect(html).toContain('assistant.subtitle');
  });
});
```

**`assistant-api.service.ts`** (reshape the method; Phase 01 fills the body):

```ts
import { Injectable } from '@angular/core';
import type { AssistantQueryRequest, SseEvent } from './assistant.types';

@Injectable({ providedIn: 'root' })
export class AssistantApiService {
  /**
   * Phase 01 wires this to /api/assistant/query via openSseStream.
   * Returns a typed async iterator of SSE events.
   */
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  query(_req: AssistantQueryRequest, _signal: AbortSignal): AsyncIterable<SseEvent> {
    throw new Error('AssistantApiService.query: implemented in Sub-9 Phase 01.');
  }
}
```

**`assistant-api.service.spec.ts`** (replace with a minimal placeholder test that doesn't call `query`):

```ts
import { TestBed } from '@angular/core/testing';
import { AssistantApiService } from './assistant-api.service';

describe('AssistantApiService (Phase 00 stub)', () => {
  it('is provided in root', () => {
    TestBed.configureTestingModule({});
    const sut = TestBed.inject(AssistantApiService);
    expect(sut).toBeTruthy();
  });

  it('query throws until Phase 01 wires it', () => {
    TestBed.configureTestingModule({});
    const sut = TestBed.inject(AssistantApiService);
    expect(() => sut.query({ messages: [], locale: 'en' }, new AbortController().signal)).toThrow(
      /Phase 01/,
    );
  });
});
```

- [ ] **Step 1: Create the store stub** in `thread/assistant-store.service.ts`.

- [ ] **Step 2: Create each of the 5 sub-component stubs** following the template above.

- [ ] **Step 3: Replace the page** (`assistant.page.{ts,html,scss}`) and its spec.

- [ ] **Step 4: Reshape the API service** (`assistant-api.service.{ts,spec.ts}`).

- [ ] **Step 5: Lint check**:
  ```bash
  cd frontend && ./node_modules/.bin/nx run web-portal:lint
  ```
  Expected: zero new warnings.

- [ ] **Step 6: Run tests** — full web-portal suite:
  ```bash
  cd frontend && ./node_modules/.bin/nx test web-portal --watch=false
  ```
  Expected: all green. Net change: the old assistant single-turn test is gone (it tested `query()` returning a Promise, which the reshape breaks); the new placeholder tests replace it.

- [ ] **Step 7: Production build** to verify the import graph:
  ```bash
  cd frontend && ./node_modules/.bin/nx build web-portal
  ```

- [ ] **Step 8: Commit:**
  ```bash
  git add frontend/apps/web-portal/src/app/features/assistant/
  git -c commit.gpgsign=false commit -m "feat(assistant): scaffold Sub-9 file structure

  Empty stub components for AssistantPage + 5 sub-components (MessageList,
  MessageBubble, ComposeBox, CitationChip, TypingIndicator) +
  AssistantStore service. AssistantApiService.query reshape lands in
  Phase 01. Page renders title + subtitle from new i18n keys; layout
  slots empty pending Phase 02. Sub-9 Phase 00.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Phase 00 close-out

After Task 0.5 commits cleanly:

- [ ] **Run the full check** to make sure Phase 00 leaves the repo green:
  ```bash
  cd frontend && ./node_modules/.bin/nx test web-portal --watch=false \
                && ./node_modules/.bin/nx run web-portal:lint \
                && ./node_modules/.bin/nx build web-portal
  cd backend && dotnet test
  ```
  Expected: all four succeed. Frontend test count grows by ~13 (4 types + 7 sse-client + 2 page/api stub) to roughly **458**. Backend xunit grows by 4.

- [ ] **Smoke-check the route** (if dev server is running): visit `http://localhost:4200/assistant`. The page should render title + subtitle in the active locale; no other UI yet.

- [ ] **Hand off to Phase 01.** Phase 01 wires the backend SSE endpoint and the `SmartAssistantClient` fake-streamer; the frontend store + UI work lands in Phase 02. Plan file: `phase-01-backend-sse.md` (to be written when we're ready to start it).

**Phase 00 done when:**
- Test count grows by ~13 frontend + 4 backend.
- 5 commits land on `main` (one per task), each green.
- The new `assistant.*` i18n keys exist in both EN and AR with parity.
- `assistant-api.service.ts` `query()` is reshape-stubbed; Phase 01 fills the body.

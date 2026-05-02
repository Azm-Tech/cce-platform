import { openSseStream } from './sse-client';
import type { SseEvent } from '../assistant.types';

/** Builds a fake Response whose `.body.getReader()` yields the given
 *  string chunks one at a time. Bypasses jsdom's missing ReadableStream
 *  by providing the minimal reader-shaped object the SSE client uses. */
function makeResponse(chunks: string[], status = 200): Response {
  const enc = new TextEncoder();
  const queue = chunks.map((c) => enc.encode(c));

  const reader = {
    read: jest.fn(async () => {
      if (queue.length === 0) return { value: undefined, done: true };
      return { value: queue.shift()!, done: false };
    }),
    releaseLock: jest.fn(),
  };

  // Cast to Response so the SUT's `res.body.getReader()` call works.
  const fakeBody = { getReader: () => reader };
  return {
    ok: status >= 200 && status < 300,
    status,
    body: fakeBody,
  } as unknown as Response;
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
    globalThis.fetch = jest.fn().mockResolvedValue(makeResponse([], 500));
    await expect(async () => {
      for await (const _ of openSseStream('/x', {}, new AbortController().signal)) {
        void _;
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
      void _;
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

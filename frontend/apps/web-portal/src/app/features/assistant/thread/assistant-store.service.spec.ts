import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { AssistantApiService } from '../assistant-api.service';
import type { Citation, SseEvent } from '../assistant.types';
import { AssistantStore } from './assistant-store.service';

const SAMPLE_CITATION: Citation = {
  id: 'r1', kind: 'resource', title: 'A resource', href: '/knowledge-center/resources/r1',
};

function streamFrom(events: SseEvent[]): AsyncIterable<SseEvent> {
  return {
    [Symbol.asyncIterator](): AsyncIterator<SseEvent> {
      let i = 0;
      return {
        async next(): Promise<IteratorResult<SseEvent>> {
          if (i >= events.length) return { value: undefined as unknown as SseEvent, done: true };
          return { value: events[i++], done: false };
        },
      };
    },
  };
}

describe('AssistantStore', () => {
  let store: AssistantStore;
  let api: { query: jest.Mock };

  beforeEach(() => {
    api = { query: jest.fn() };
    TestBed.configureTestingModule({
      providers: [
        AssistantStore,
        { provide: AssistantApiService, useValue: api },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
      ],
    });
    store = TestBed.inject(AssistantStore);
  });

  it('starts empty + canSend is true', () => {
    expect(store.messages()).toEqual([]);
    expect(store.streaming()).toBe(false);
    expect(store.canSend()).toBe(true);
  });

  it('sendMessage appends user + assistant placeholder, then streams text', async () => {
    api.query.mockReturnValue(streamFrom([
      { type: 'text', content: 'Hello ' },
      { type: 'text', content: 'world' },
      { type: 'done' },
    ]));
    await store.sendMessage('What is CCE?');
    const msgs = store.messages();
    expect(msgs).toHaveLength(2);
    expect(msgs[0].role).toBe('user');
    expect(msgs[0].content).toBe('What is CCE?');
    expect(msgs[1].role).toBe('assistant');
    expect(msgs[1].content).toBe('Hello world');
    expect(msgs[1].status).toBe('complete');
    expect(store.streaming()).toBe(false);
  });

  it('citation events accumulate into the assistant message', async () => {
    api.query.mockReturnValue(streamFrom([
      { type: 'text', content: 'See ' },
      { type: 'citation', citation: SAMPLE_CITATION },
      { type: 'done' },
    ]));
    await store.sendMessage('q');
    expect(store.messages()[1].citations).toEqual([SAMPLE_CITATION]);
  });

  it('passes the full thread history to api.query', async () => {
    api.query.mockReturnValue(streamFrom([{ type: 'text', content: 'x' }, { type: 'done' }]));
    await store.sendMessage('first');
    api.query.mockReturnValue(streamFrom([{ type: 'text', content: 'y' }, { type: 'done' }]));
    await store.sendMessage('second');
    const lastCall = api.query.mock.calls[1][0];
    expect(lastCall.messages.map((m: { role: string; content: string }) => m.content))
      .toEqual(['first', 'x', 'second']);
    expect(lastCall.locale).toBe('en');
  });

  it('error event sets status="error" + errorKind', async () => {
    api.query.mockReturnValue(streamFrom([
      { type: 'text', content: 'Oh ' },
      { type: 'error', error: { kind: 'server' } },
    ]));
    await store.sendMessage('q');
    const last = store.messages().at(-1)!;
    expect(last.status).toBe('error');
    expect(last.errorKind).toBe('server');
    expect(last.content).toBe('Oh ');
  });

  it('thrown error sets status="error" with kind="unknown"', async () => {
    api.query.mockImplementation(() => {
      throw new Error('boom');
    });
    await store.sendMessage('q');
    const last = store.messages().at(-1)!;
    expect(last.status).toBe('error');
    expect(last.errorKind).toBe('unknown');
  });

  it('thrown SSE-open error maps to errorKind="server"', async () => {
    api.query.mockImplementation(() => {
      throw new Error('SSE open failed: 500');
    });
    await store.sendMessage('q');
    expect(store.messages().at(-1)!.errorKind).toBe('server');
  });

  it('cancel() aborts stream and marks message complete (not error)', async () => {
    let yielded = 0;
    api.query.mockImplementation((_req: unknown, signal: AbortSignal) => ({
      async *[Symbol.asyncIterator](): AsyncIterator<SseEvent> {
        yield { type: 'text', content: 'a ' };
        yielded++;
        // Wait for abort
        while (!signal.aborted) await new Promise((r) => setTimeout(r, 5));
        const err = new Error('Aborted');
        err.name = 'AbortError';
        throw err;
      },
    }));
    const promise = store.sendMessage('q');
    // Wait for first chunk to be applied
    while (yielded === 0) await new Promise((r) => setTimeout(r, 5));
    store.cancel();
    await promise;
    const last = store.messages().at(-1)!;
    expect(last.status).toBe('complete');
    expect(last.content).toBe('a ');
  });

  it('clear() wipes all messages', async () => {
    api.query.mockReturnValue(streamFrom([{ type: 'text', content: 'x' }, { type: 'done' }]));
    await store.sendMessage('q');
    expect(store.messages()).toHaveLength(2);
    store.clear();
    expect(store.messages()).toEqual([]);
  });

  it('retry replaces a failed assistant message and re-streams', async () => {
    api.query.mockReturnValue(streamFrom([{ type: 'error', error: { kind: 'server' } }]));
    await store.sendMessage('q');
    expect(store.messages().at(-1)!.status).toBe('error');
    api.query.mockReturnValue(streamFrom([{ type: 'text', content: 'ok' }, { type: 'done' }]));
    await store.retry();
    expect(store.messages()).toHaveLength(2);
    expect(store.messages()[1].content).toBe('ok');
    expect(store.messages()[1].status).toBe('complete');
  });

  it('regenerate rolls back the last assistant reply and re-streams', async () => {
    api.query.mockReturnValue(streamFrom([{ type: 'text', content: 'first' }, { type: 'done' }]));
    await store.sendMessage('q');
    api.query.mockReturnValue(streamFrom([{ type: 'text', content: 'second' }, { type: 'done' }]));
    await store.regenerate();
    expect(store.messages()).toHaveLength(2);
    expect(store.messages()[1].content).toBe('second');
  });

  it('canSend is false while streaming', async () => {
    let resolveStream: () => void = () => undefined;
    const streamReady = new Promise<void>((r) => (resolveStream = r));
    api.query.mockImplementation(() => ({
      async *[Symbol.asyncIterator](): AsyncIterator<SseEvent> {
        yield { type: 'text', content: 'a' };
        await streamReady;
        yield { type: 'done' };
      },
    }));
    const promise = store.sendMessage('q');
    // Allow first yield to land
    await new Promise((r) => setTimeout(r, 5));
    expect(store.canSend()).toBe(false);
    resolveStream();
    await promise;
    expect(store.canSend()).toBe(true);
  });

  it('sendMessage with empty content is a no-op', async () => {
    await store.sendMessage('   ');
    expect(api.query).not.toHaveBeenCalled();
    expect(store.messages()).toEqual([]);
  });
});

import { TestBed } from '@angular/core/testing';
import { AssistantApiService } from './assistant-api.service';

describe('AssistantApiService', () => {
  let originalFetch: typeof globalThis.fetch;
  beforeEach(() => { originalFetch = globalThis.fetch; });
  afterEach(() => { globalThis.fetch = originalFetch; });

  it('is provided in root', () => {
    TestBed.configureTestingModule({});
    const sut = TestBed.inject(AssistantApiService);
    expect(sut).toBeTruthy();
  });

  it('query passes the request body to openSseStream as a POST', async () => {
    const fetchMock = jest.fn().mockResolvedValue({
      ok: true, status: 200,
      body: { getReader: () => ({
        read: jest.fn().mockResolvedValueOnce({ value: undefined, done: true }),
        releaseLock: jest.fn(),
      }) },
    });
    globalThis.fetch = fetchMock as unknown as typeof globalThis.fetch;

    TestBed.configureTestingModule({});
    const sut = TestBed.inject(AssistantApiService);
    const it = sut.query(
      { messages: [{ role: 'user', content: 'hi' }], locale: 'en' },
      new AbortController().signal,
    );
    for await (const _ of it) void _;

    expect(fetchMock).toHaveBeenCalledWith('/api/assistant/query', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ messages: [{ role: 'user', content: 'hi' }], locale: 'en' }),
    }));
  });
});

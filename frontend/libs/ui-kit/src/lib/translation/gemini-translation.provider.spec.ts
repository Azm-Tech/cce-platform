import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { GeminiTranslationProvider } from './gemini-translation.provider';

const CONFIG_URL = 'assets/translation-config.json';
const GEMINI = /generativelanguage\.googleapis\.com/;

/** Let pending microtasks settle so the async config→Gemini chain issues its request. */
const tick = () => new Promise((r) => setTimeout(r, 0));

function geminiOk(text: string) {
  return { candidates: [{ content: { parts: [{ text }] } }] };
}

describe('GeminiTranslationProvider', () => {
  let provider: GeminiTranslationProvider;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    provider = TestBed.inject(GeminiTranslationProvider);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('returns translateNotConfigured when no key is present', async () => {
    const p = provider.translate('مرحبا');
    httpMock.expectOne(CONFIG_URL).flush({ apiKey: '' });
    await expect(p).resolves.toEqual({ ok: false, kind: 'translateNotConfigured' });
  });

  it('calls Gemini with the configured model + key and a plain-text prompt', async () => {
    const p = provider.translate('مرحبا', { format: 'text' });
    httpMock.expectOne(CONFIG_URL).flush({ apiKey: 'secret', model: 'gemini-2.0-flash' });
    await tick();

    const req = httpMock.expectOne((r) => GEMINI.test(r.url));
    expect(req.request.method).toBe('POST');
    expect(req.request.urlWithParams).toContain('models/gemini-2.0-flash:generateContent');
    expect(req.request.urlWithParams).toContain('key=secret');
    const prompt = req.request.body.contents[0].parts[0].text as string;
    expect(prompt).toContain('Arabic text into English');
    expect(prompt).not.toContain('HTML');
    expect(prompt).toContain('مرحبا');
    req.flush(geminiOk('Hello'));

    await expect(p).resolves.toEqual({ ok: true, text: 'Hello' });
  });

  it('falls back to the default model when config omits one', async () => {
    const p = provider.translate('مرحبا');
    httpMock.expectOne(CONFIG_URL).flush({ apiKey: 'k' });
    await tick();
    const req = httpMock.expectOne((r) => GEMINI.test(r.url));
    expect(req.request.urlWithParams).toContain('models/gemini-2.5-flash:generateContent');
    req.flush(geminiOk('Hello'));
    await p;
  });

  it('uses the HTML-preserving prompt for format=html and strips code fences', async () => {
    const p = provider.translate('<p>مرحبا</p>', { format: 'html' });
    httpMock.expectOne(CONFIG_URL).flush({ apiKey: 'k' });
    await tick();

    const req = httpMock.expectOne((r) => GEMINI.test(r.url));
    const prompt = req.request.body.contents[0].parts[0].text as string;
    expect(prompt).toContain('Preserve ALL HTML tags');
    req.flush(geminiOk('```html\n<p>Hello</p>\n```'));

    await expect(p).resolves.toEqual({ ok: true, text: '<p>Hello</p>' });
  });

  it('maps a Gemini error to translateFailed', async () => {
    const p = provider.translate('مرحبا');
    httpMock.expectOne(CONFIG_URL).flush({ apiKey: 'k' });
    await tick();
    httpMock
      .expectOne((r) => GEMINI.test(r.url))
      .flush('boom', { status: 500, statusText: 'Server Error' });
    await expect(p).resolves.toEqual({ ok: false, kind: 'translateFailed' });
  });

  it('caches config across calls (only one config fetch)', async () => {
    const p1 = provider.translate('أ');
    httpMock.expectOne(CONFIG_URL).flush({ apiKey: 'k' });
    await tick();
    httpMock.expectOne((r) => GEMINI.test(r.url)).flush(geminiOk('A'));
    await p1;

    const p2 = provider.translate('ب');
    await tick();
    // No second config fetch — would throw on verify if one were issued.
    httpMock.expectOne((r) => GEMINI.test(r.url)).flush(geminiOk('B'));
    await expect(p2).resolves.toEqual({ ok: true, text: 'B' });
  });
});

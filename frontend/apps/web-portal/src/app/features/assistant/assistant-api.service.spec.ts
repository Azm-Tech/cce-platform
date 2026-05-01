import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AssistantApiService } from './assistant-api.service';

describe('AssistantApiService', () => {
  let sut: AssistantApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(AssistantApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('query POSTs /api/assistant/query with the payload', async () => {
    const promise = sut.query({ question: 'What is CCE?', locale: 'en' });
    const req = http.expectOne('/api/assistant/query');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ question: 'What is CCE?', locale: 'en' });
    req.flush({ reply: 'CCE is a platform.' });
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value.reply).toBe('CCE is a platform.');
  });

  it('returns server error on 500', async () => {
    const promise = sut.query({ question: 'q', locale: 'en' });
    http.expectOne('/api/assistant/query').flush('', { status: 500, statusText: 'Server Error' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('server');
  });
});

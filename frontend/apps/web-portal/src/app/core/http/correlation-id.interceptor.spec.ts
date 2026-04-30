import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { correlationIdInterceptor } from './correlation-id.interceptor';

describe('correlationIdInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([correlationIdInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('adds X-Correlation-Id header when missing', () => {
    http.get('/api/me').subscribe();
    const req = httpTesting.expectOne('/api/me');
    expect(req.request.headers.get('X-Correlation-Id')).toBeTruthy();
    req.flush({});
  });

  it('does NOT stamp the header on cross-origin requests', () => {
    http.get('http://example.com/api').subscribe();
    const req = httpTesting.expectOne('http://example.com/api');
    expect(req.request.headers.has('X-Correlation-Id')).toBe(false);
    req.flush({});
  });

  it('preserves existing X-Correlation-Id header', () => {
    http.get('/api/me', { headers: { 'X-Correlation-Id': 'abc-123' } }).subscribe();
    const req = httpTesting.expectOne('/api/me');
    expect(req.request.headers.get('X-Correlation-Id')).toBe('abc-123');
    req.flush({});
  });

  it('stamps the header on absolute same-origin URLs', () => {
    const sameOrigin = `${window.location.origin}/api/x`;
    http.get(sameOrigin).subscribe();
    const req = httpTesting.expectOne(sameOrigin);
    expect(req.request.headers.get('X-Correlation-Id')).toBeTruthy();
    req.flush({});
  });
});

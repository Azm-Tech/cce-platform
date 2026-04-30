import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { bffCredentialsInterceptor } from './bff-credentials.interceptor';

describe('bffCredentialsInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([bffCredentialsInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('sets withCredentials on relative URLs', () => {
    http.get('/api/me').subscribe();
    const req = httpTesting.expectOne('/api/me');
    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });

  it('does NOT set withCredentials on cross-origin URLs', () => {
    http.get('http://example.com/api').subscribe();
    const req = httpTesting.expectOne('http://example.com/api');
    expect(req.request.withCredentials).toBe(false);
    req.flush({});
  });

  it('sets withCredentials on absolute same-origin URLs', () => {
    const sameOrigin = `${window.location.origin}/api/x`;
    http.get(sameOrigin).subscribe();
    const req = httpTesting.expectOne(sameOrigin);
    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });
});

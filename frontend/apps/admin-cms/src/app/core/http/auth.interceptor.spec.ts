import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let assignMock: jest.Mock;

  beforeEach(() => {
    // jsdom makes window.location non-configurable; redefine it so we can spy on assign.
    assignMock = jest.fn();
    Object.defineProperty(window, 'location', {
      configurable: true,
      writable: true,
      value: { ...window.location, assign: assignMock },
    });

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: { url: '/users' } as Partial<Router> },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('adds withCredentials to every request', () => {
    http.get('/api/admin/users').subscribe();
    const req = httpTesting.expectOne('/api/admin/users');
    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });

  it('redirects to /auth/login on 401 for non-/api/me URLs', () => {
    http.get('/api/admin/users').subscribe({ error: () => undefined });
    const req = httpTesting.expectOne('/api/admin/users');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    expect(assignMock).toHaveBeenCalledWith('/auth/login?returnUrl=%2Fusers');
  });

  it('does NOT redirect for /api/me 401', () => {
    http.get('/api/me').subscribe({ error: () => undefined });
    const req = httpTesting.expectOne('/api/me');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    expect(assignMock).not.toHaveBeenCalled();
  });

  it('does NOT add withCredentials to cross-origin requests', () => {
    http.get('http://localhost:8080/realms/cce-internal/.well-known/openid-configuration').subscribe();
    const req = httpTesting.expectOne(
      'http://localhost:8080/realms/cce-internal/.well-known/openid-configuration',
    );
    expect(req.request.withCredentials).toBe(false);
    req.flush({});
  });

  it('does NOT redirect on 401 for cross-origin URLs', () => {
    http.get('http://localhost:8080/x').subscribe({ error: () => undefined });
    const req = httpTesting.expectOne('http://localhost:8080/x');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    expect(assignMock).not.toHaveBeenCalled();
  });
});

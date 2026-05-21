import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let signInSpy: jest.Mock;
  let refreshSpy: jest.Mock;
  let accessTokenFn: () => string | null;

  beforeEach(() => {
    signInSpy = jest.fn();
    refreshSpy = jest.fn().mockResolvedValue(undefined);
    accessTokenFn = () => null;

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: { url: '/users' } as Partial<Router> },
        {
          provide: AuthService,
          useValue: {
            signIn: signInSpy,
            refresh: refreshSpy,
            accessToken: () => accessTokenFn(),
          } as Partial<AuthService>,
        },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('adds withCredentials to internal requests', () => {
    http.get('/api/admin/users').subscribe();
    const req = httpTesting.expectOne('/api/admin/users');
    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });

  it('retries with new Bearer token on 401 if refresh succeeds', async () => {
    accessTokenFn = () => 'new-token';

    http.get('/api/admin/users').subscribe();

    // First attempt → 401
    const first = httpTesting.expectOne('/api/admin/users');
    first.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    // Wait for refresh + retry
    await Promise.resolve();

    // Retry should have the new token
    const retry = httpTesting.expectOne('/api/admin/users');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new-token');
    expect(signInSpy).not.toHaveBeenCalled();
    retry.flush({});
  });

  it('calls auth.signIn when refresh yields no token', async () => {
    // accessTokenFn stays () => null — refresh cleared the session
    http.get('/api/admin/users').subscribe({ error: () => undefined });

    const req = httpTesting.expectOne('/api/admin/users');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    await Promise.resolve();

    expect(refreshSpy).toHaveBeenCalled();
    expect(signInSpy).toHaveBeenCalledWith('/users');
  });

  it('does NOT redirect for /api/me 401', () => {
    http.get('/api/me').subscribe({ error: () => undefined });
    const req = httpTesting.expectOne('/api/me');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    expect(refreshSpy).not.toHaveBeenCalled();
    expect(signInSpy).not.toHaveBeenCalled();
  });

  it('does NOT redirect for /api/auth/ 401', () => {
    http.get('/api/auth/refresh').subscribe({ error: () => undefined });
    const req = httpTesting.expectOne('/api/auth/refresh');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    expect(refreshSpy).not.toHaveBeenCalled();
    expect(signInSpy).not.toHaveBeenCalled();
  });

  it('does NOT add withCredentials to cross-origin requests', () => {
    http.get('http://localhost:8080/realms/cce-internal/.well-known/openid-configuration').subscribe();
    const req = httpTesting.expectOne(
      'http://localhost:8080/realms/cce-internal/.well-known/openid-configuration',
    );
    expect(req.request.withCredentials).toBe(false);
    req.flush({});
  });

  it('does NOT refresh or redirect on 401 for cross-origin URLs', () => {
    http.get('http://localhost:8080/x').subscribe({ error: () => undefined });
    const req = httpTesting.expectOne('http://localhost:8080/x');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    expect(refreshSpy).not.toHaveBeenCalled();
    expect(signInSpy).not.toHaveBeenCalled();
  });
});

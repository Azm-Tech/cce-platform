import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ToastService } from '../feedback/toast.service';
import { serverErrorInterceptor, SUPPRESS_ERROR_TOAST } from './server-error.interceptor';

describe('serverErrorInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let toast: { error: jest.Mock };

  beforeEach(() => {
    toast = { error: jest.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([serverErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: ToastService, useValue: toast },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('toasts errors.network on status 0', () => {
    http.get('/x').subscribe({ error: () => undefined });
    httpTesting.expectOne('/x').flush('', { status: 0, statusText: 'Unknown' });
    expect(toast.error).toHaveBeenCalledWith('errors.network');
  });

  it('toasts errors.rateLimit on 429', () => {
    http.get('/x').subscribe({ error: () => undefined });
    httpTesting.expectOne('/x').flush('', { status: 429, statusText: 'Too Many Requests' });
    expect(toast.error).toHaveBeenCalledWith('errors.rateLimit');
  });

  it('toasts errors.server on 5xx', () => {
    http.get('/x').subscribe({ error: () => undefined });
    httpTesting.expectOne('/x').flush('boom', { status: 500, statusText: 'Server' });
    expect(toast.error).toHaveBeenCalledWith('errors.server');
  });

  it('toasts errors.forbidden on 403', () => {
    http.get('/x').subscribe({ error: () => undefined });
    httpTesting.expectOne('/x').flush('nope', { status: 403, statusText: 'Forbidden' });
    expect(toast.error).toHaveBeenCalledWith('errors.forbidden');
  });

  it('toasts errors.not-found on 404', () => {
    http.get('/x').subscribe({ error: () => undefined });
    httpTesting.expectOne('/x').flush('nope', { status: 404, statusText: 'Not Found' });
    expect(toast.error).toHaveBeenCalledWith('errors.not-found');
  });

  it('does NOT toast on 404 when SUPPRESS_ERROR_TOAST includes 404', () => {
    const context = new HttpContext().set(SUPPRESS_ERROR_TOAST, [404]);
    http.get('/x', { context }).subscribe({ error: () => undefined });
    httpTesting.expectOne('/x').flush('nope', { status: 404, statusText: 'Not Found' });
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('does not toast on 2xx', () => {
    http.get('/x').subscribe();
    httpTesting.expectOne('/x').flush({});
    expect(toast.error).not.toHaveBeenCalled();
  });
});

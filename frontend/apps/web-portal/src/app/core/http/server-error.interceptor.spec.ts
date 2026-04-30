import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ToastService } from '@frontend/ui-kit';
import { serverErrorInterceptor } from './server-error.interceptor';

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

  it('does not toast on 200', () => {
    http.get('/x').subscribe();
    httpTesting.expectOne('/x').flush({});
    expect(toast.error).not.toHaveBeenCalled();
  });
});

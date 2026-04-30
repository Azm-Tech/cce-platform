import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ReportsApiService } from './reports-api.service';

describe('ReportsApiService', () => {
  let sut: ReportsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(ReportsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('download GETs /api/admin/reports/{slug}.csv with no params when no range provided', async () => {
    const promise = sut.download('users-registrations');
    const req = http.expectOne((r) => r.url === '/api/admin/reports/users-registrations.csv');
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    expect(req.request.params.keys()).toEqual([]);
    req.flush(new Blob(['id,name\n1,a'], { type: 'text/csv' }));
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toBeInstanceOf(Blob);
  });

  it('download passes from + to query when supplied', async () => {
    const promise = sut.download('news', { from: '2026-01-01', to: '2026-04-29' });
    const req = http.expectOne((r) => r.url === '/api/admin/reports/news.csv');
    expect(req.request.params.get('from')).toBe('2026-01-01');
    expect(req.request.params.get('to')).toBe('2026-04-29');
    req.flush(new Blob([''], { type: 'text/csv' }));
    await promise;
  });

  it('download returns FeatureError on 403', async () => {
    const promise = sut.download('experts');
    http.expectOne('/api/admin/reports/experts.csv').flush(
      new Blob(['Forbidden'], { type: 'text/plain' }),
      { status: 403, statusText: 'Forbidden' },
    );
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('forbidden');
  });
});

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuditApiService } from './audit-api.service';

describe('AuditApiService', () => {
  let sut: AuditApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(AuditApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list builds full filter set', async () => {
    const p = sut.list({
      page: 2, pageSize: 50,
      actor: 'admin@cce.local',
      actionPrefix: 'Resource.',
      resourceType: 'Resource',
      correlationId: 'cid-1',
      from: '2026-01-01',
      to: '2026-04-29',
    });
    const req = http.expectOne((r) => r.url === '/api/admin/audit-events');
    expect(req.request.params.get('actor')).toBe('admin@cce.local');
    expect(req.request.params.get('actionPrefix')).toBe('Resource.');
    expect(req.request.params.get('resourceType')).toBe('Resource');
    expect(req.request.params.get('correlationId')).toBe('cid-1');
    expect(req.request.params.get('from')).toBe('2026-01-01');
    expect(req.request.params.get('to')).toBe('2026-04-29');
    req.flush({ items: [], page: 2, pageSize: 50, total: 0 });
    await p;
  });

  it('list returns FeatureError on 403', async () => {
    const promise = sut.list();
    http.expectOne('/api/admin/audit-events').flush('', { status: 403, statusText: 'Forbidden' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('forbidden');
  });
});

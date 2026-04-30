import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ExpertApiService } from './expert-api.service';
import type { ExpertProfile, ExpertRequest } from './expert.types';

describe('ExpertApiService', () => {
  let sut: ExpertApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(ExpertApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('listRequests builds status query when provided', async () => {
    const promise = sut.listRequests({ page: 1, pageSize: 20, status: 'Pending' });
    const req = http.expectOne((r) => r.url === '/api/admin/expert-requests');
    expect(req.request.params.get('status')).toBe('Pending');
    expect(req.request.params.get('page')).toBe('1');
    req.flush({ items: [], page: 1, pageSize: 20, total: 0 });
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('approve POSTs with academic title body', async () => {
    const promise = sut.approve('req-1', { academicTitleAr: 'دكتور', academicTitleEn: 'Dr.' });
    const req = http.expectOne('/api/admin/expert-requests/req-1/approve');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ academicTitleAr: 'دكتور', academicTitleEn: 'Dr.' });
    const updated: ExpertRequest = {
      id: 'req-1',
      requestedById: 'u',
      requestedByUserName: 'a',
      requestedBioAr: '',
      requestedBioEn: '',
      requestedTags: [],
      submittedOn: '2026-04-29',
      status: 'Approved',
      processedById: 'admin',
      processedOn: '2026-04-29',
      rejectionReasonAr: null,
      rejectionReasonEn: null,
    };
    req.flush(updated);
    const res = await promise;
    if (res.ok) expect(res.value.status).toBe('Approved');
    else fail('expected ok');
  });

  it('reject POSTs with rejection reason body', async () => {
    const promise = sut.reject('req-1', {
      rejectionReasonAr: 'سبب',
      rejectionReasonEn: 'reason',
    });
    const req = http.expectOne('/api/admin/expert-requests/req-1/reject');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ rejectionReasonAr: 'سبب', rejectionReasonEn: 'reason' });
    req.flush({
      id: 'req-1',
      requestedById: 'u',
      requestedByUserName: 'a',
      requestedBioAr: '',
      requestedBioEn: '',
      requestedTags: [],
      submittedOn: '2026-04-29',
      status: 'Rejected',
      processedById: 'admin',
      processedOn: '2026-04-29',
      rejectionReasonAr: 'سبب',
      rejectionReasonEn: 'reason',
    } satisfies ExpertRequest);
    const res = await promise;
    if (res.ok) expect(res.value.status).toBe('Rejected');
    else fail('expected ok');
  });

  it('listProfiles passes search query', async () => {
    const promise = sut.listProfiles({ search: 'alice' });
    const req = http.expectOne((r) => r.url === '/api/admin/expert-profiles');
    expect(req.request.params.get('search')).toBe('alice');
    const profiles: ExpertProfile[] = [
      {
        id: 'p1',
        userId: 'u1',
        userName: 'alice',
        bioAr: '',
        bioEn: '',
        expertiseTags: ['ccs'],
        academicTitleAr: 'دكتور',
        academicTitleEn: 'Dr.',
        approvedOn: '2026-04-29',
        approvedById: 'admin',
      },
    ];
    req.flush({ items: profiles, page: 1, pageSize: 20, total: 1 });
    const res = await promise;
    if (res.ok) expect(res.value.items).toHaveLength(1);
    else fail('expected ok');
  });

  it('returns FeatureError on 403', async () => {
    const promise = sut.listRequests();
    http.expectOne('/api/admin/expert-requests').flush('', { status: 403, statusText: 'Forbidden' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('forbidden');
  });
});

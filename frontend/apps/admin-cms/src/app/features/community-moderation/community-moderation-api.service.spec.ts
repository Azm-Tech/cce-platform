import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CommunityModerationApiService } from './community-moderation-api.service';

describe('CommunityModerationApiService — moderation queue', () => {
  let sut: CommunityModerationApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(CommunityModerationApiService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('listModerationQueue capitalizes status, keeps contentType lowercase, sends paging', async () => {
    const p = sut.listModerationQueue({ status: 'flagged', contentType: 'post', page: 2, pageSize: 20 });
    const req = http.expectOne(
      (r) => r.url === '/api/admin/community/moderation/queue',
    );
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('status')).toBe('Flagged');
    expect(req.request.params.get('contentType')).toBe('post');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({ items: [], page: 2, pageSize: 20, total: 0 });
    const res = await p;
    expect(res.ok).toBe(true);
  });

  it('listModerationQueue omits status/contentType when not provided', async () => {
    const p = sut.listModerationQueue({ page: 1, pageSize: 20 });
    const req = http.expectOne((r) => r.url === '/api/admin/community/moderation/queue');
    expect(req.request.params.has('status')).toBe(false);
    expect(req.request.params.has('contentType')).toBe(false);
    req.flush({ items: [], page: 1, pageSize: 20, total: 0 });
    await p;
  });

  it('approveModeration POSTs to the approve endpoint', async () => {
    const p = sut.approveModeration('rec-1');
    const req = http.expectOne('/api/admin/community/moderation/rec-1/approve');
    expect(req.request.method).toBe('POST');
    req.flush(null);
    expect((await p).ok).toBe(true);
  });

  it('rejectModeration sends the reason when provided', async () => {
    const p = sut.rejectModeration('rec-2', '  spam  ');
    const req = http.expectOne('/api/admin/community/moderation/rec-2/reject');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reason: 'spam' });
    req.flush(null);
    expect((await p).ok).toBe(true);
  });

  it('rejectModeration sends an empty body when no reason', async () => {
    const p = sut.rejectModeration('rec-3');
    const req = http.expectOne('/api/admin/community/moderation/rec-3/reject');
    expect(req.request.body).toEqual({});
    req.flush(null);
    expect((await p).ok).toBe(true);
  });
});

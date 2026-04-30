import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NotificationsApiService } from './notifications-api.service';
import type { PagedResult, UserNotification } from './notification.types';

const SAMPLE: UserNotification = {
  id: 'n1',
  templateId: 't1',
  renderedSubjectAr: 'عنوان',
  renderedSubjectEn: 'Subject',
  renderedBody: 'Body',
  renderedLocale: 'en',
  channel: 'InApp',
  sentOn: '2026-04-29T12:00:00Z',
  readOn: null,
  status: 'Sent',
};

const PAGED: PagedResult<UserNotification> = {
  items: [SAMPLE],
  page: 1,
  pageSize: 20,
  total: 1,
};

describe('NotificationsApiService', () => {
  let sut: NotificationsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(NotificationsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list({ page: 2 }) GETs with ?page=2', async () => {
    const promise = sut.list({ page: 2 });
    const req = http.expectOne((r) => r.url === '/api/me/notifications');
    expect(req.request.params.get('page')).toBe('2');
    req.flush(PAGED);
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('list({ status: "Sent" }) adds status query param', async () => {
    const promise = sut.list({ status: 'Sent' });
    const req = http.expectOne((r) => r.url === '/api/me/notifications');
    expect(req.request.params.get('status')).toBe('Sent');
    req.flush(PAGED);
    await promise;
  });

  it('getUnreadCount() unwraps { count: N }', async () => {
    const promise = sut.getUnreadCount();
    http.expectOne('/api/me/notifications/unread-count').flush({ count: 7 });
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toBe(7);
  });

  it('markRead(id) POSTs to /api/me/notifications/{id}/mark-read', async () => {
    const promise = sut.markRead('n1');
    const req = http.expectOne('/api/me/notifications/n1/mark-read');
    expect(req.request.method).toBe('POST');
    req.flush(null, { status: 204, statusText: 'No Content' });
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('markAllRead() POSTs and returns { marked }', async () => {
    const promise = sut.markAllRead();
    const req = http.expectOne('/api/me/notifications/mark-all-read');
    expect(req.request.method).toBe('POST');
    req.flush({ marked: 3 });
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toBe(3);
  });
});

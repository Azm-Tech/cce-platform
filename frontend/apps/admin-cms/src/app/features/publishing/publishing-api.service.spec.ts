import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PublishingApiService } from './publishing-api.service';

describe('PublishingApiService', () => {
  let sut: PublishingApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(PublishingApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  describe('news', () => {
    it('listNews builds query', async () => {
      const promise = sut.listNews({ page: 1, pageSize: 20, search: 'q', isPublished: true });
      const req = http.expectOne((r) => r.url === '/api/admin/news');
      expect(req.request.params.get('isPublished')).toBe('true');
      req.flush({ items: [], page: 1, pageSize: 20, total: 0 });
      await promise;
    });
    it('createNews POSTs body', async () => {
      const body = { titleAr: 'a', titleEn: 'b', contentAr: 'c', contentEn: 'd', slug: 'a-b' };
      const p = sut.createNews(body);
      const req = http.expectOne('/api/admin/news');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(body);
      req.flush({});
      await p;
    });
    it('updateNews PUTs with rowVersion', async () => {
      const p = sut.updateNews('n1', {
        titleAr: 'a', titleEn: 'b', contentAr: 'c', contentEn: 'd', slug: 's', rowVersion: 'v',
      });
      const req = http.expectOne('/api/admin/news/n1');
      expect(req.request.method).toBe('PUT');
      req.flush({});
      await p;
    });
    it('deleteNews DELETEs', async () => {
      const p = sut.deleteNews('n1');
      const req = http.expectOne('/api/admin/news/n1');
      expect(req.request.method).toBe('DELETE');
      req.flush(null, { status: 204, statusText: 'No Content' });
      await p;
    });
    it('publishNews POSTs to /publish', async () => {
      const p = sut.publishNews('n1');
      const req = http.expectOne('/api/admin/news/n1/publish');
      expect(req.request.method).toBe('POST');
      req.flush({});
      await p;
    });
  });

  describe('events', () => {
    it('createEvent POSTs body', async () => {
      const body = {
        titleAr: 'a', titleEn: 'b', descriptionAr: 'c', descriptionEn: 'd',
        startsOn: '2026-04-29T10:00:00Z', endsOn: '2026-04-29T12:00:00Z',
      };
      const p = sut.createEvent(body);
      const req = http.expectOne('/api/admin/events');
      expect(req.request.method).toBe('POST');
      req.flush({});
      await p;
    });
    it('rescheduleEvent POSTs to /reschedule', async () => {
      const p = sut.rescheduleEvent('e1', {
        startsOn: '2026-04-29T11:00:00Z',
        endsOn: '2026-04-29T13:00:00Z',
        rowVersion: 'v',
      });
      const req = http.expectOne('/api/admin/events/e1/reschedule');
      expect(req.request.method).toBe('POST');
      req.flush({});
      await p;
    });
  });

  describe('pages', () => {
    it('createPage POSTs body', async () => {
      const p = sut.createPage({
        slug: 'about', pageType: 'AboutPlatform',
        titleAr: 'a', titleEn: 'b', contentAr: 'c', contentEn: 'd',
      });
      const req = http.expectOne('/api/admin/pages');
      req.flush({});
      await p;
    });
    it('updatePage PUTs with rowVersion', async () => {
      const p = sut.updatePage('p1', { titleAr: '', titleEn: '', contentAr: '', contentEn: '', rowVersion: 'v' });
      const req = http.expectOne('/api/admin/pages/p1');
      expect(req.request.method).toBe('PUT');
      req.flush({});
      await p;
    });
  });

  describe('homepage sections', () => {
    it('listHomepageSections GETs without paging', async () => {
      const p = sut.listHomepageSections();
      const req = http.expectOne('/api/admin/homepage-sections');
      req.flush([]);
      await p;
    });
    it('reorderHomepageSections POSTs to /reorder', async () => {
      const body = { assignments: [{ id: 'h1', orderIndex: 0 }, { id: 'h2', orderIndex: 1 }] };
      const p = sut.reorderHomepageSections(body);
      const req = http.expectOne('/api/admin/homepage-sections/reorder');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(body);
      req.flush(null, { status: 204, statusText: 'No Content' });
      await p;
    });
  });

  it('returns FeatureError on 500', async () => {
    const promise = sut.listNews();
    http.expectOne('/api/admin/news').flush('', { status: 500, statusText: 'Server Error' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('server');
  });
});

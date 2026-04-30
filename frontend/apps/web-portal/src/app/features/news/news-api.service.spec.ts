import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NewsApiService } from './news-api.service';
import type { NewsArticle } from './news.types';

const SAMPLE: NewsArticle = {
  id: 'n1',
  titleAr: 'عنوان', titleEn: 'Title',
  contentAr: 'محتوى', contentEn: 'content',
  slug: 'hello-world',
  authorId: 'a1',
  featuredImageUrl: null,
  publishedOn: '2026-04-29',
  isFeatured: true,
  isPublished: true,
};

describe('NewsApiService', () => {
  let sut: NewsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(NewsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('listNews builds query string with all params', async () => {
    const promise = sut.listNews({ page: 2, pageSize: 50, isFeatured: true });
    const req = http.expectOne((r) => r.url === '/api/news');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.get('isFeatured')).toBe('true');
    req.flush({ items: [SAMPLE], page: 2, pageSize: 50, total: 1 });
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('getBySlug GETs /api/news/{slug} and URL-encodes special chars', async () => {
    const promise = sut.getBySlug('hello world/foo');
    const req = http.expectOne('/api/news/hello%20world%2Ffoo');
    expect(req.request.method).toBe('GET');
    req.flush(SAMPLE);
    const res = await promise;
    if (res.ok) expect(res.value.slug).toBe('hello-world');
  });

  it('getBySlug returns not-found on 404', async () => {
    const promise = sut.getBySlug('missing');
    http.expectOne('/api/news/missing').flush('', { status: 404, statusText: 'Not Found' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('not-found');
  });
});

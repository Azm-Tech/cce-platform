import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SearchApiService } from './search-api.service';
import type { PagedResult, SearchHit } from './search.types';

const SAMPLE_HIT: SearchHit = {
  id: 'h1',
  type: 'News',
  titleAr: 'عنوان', titleEn: 'Title',
  excerptAr: 'مقتطف', excerptEn: 'Excerpt',
  score: 0.95,
};
const PAGED: PagedResult<SearchHit> = { items: [SAMPLE_HIT], page: 1, pageSize: 20, total: 1 };

describe('SearchApiService', () => {
  let sut: SearchApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(SearchApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('search({ q }) GETs /api/search with only q param', async () => {
    const promise = sut.search({ q: 'circular' });
    const req = http.expectOne((r) => r.url === '/api/search');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('q')).toBe('circular');
    expect(req.request.params.has('type')).toBe(false);
    expect(req.request.params.has('page')).toBe(false);
    expect(req.request.params.has('pageSize')).toBe(false);
    req.flush(PAGED);
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('adds type=<value> when type is provided', async () => {
    const promise = sut.search({ q: 'carbon', type: 'News' });
    const req = http.expectOne((r) => r.url === '/api/search');
    expect(req.request.params.get('q')).toBe('carbon');
    expect(req.request.params.get('type')).toBe('News');
    req.flush(PAGED);
    await promise;
  });

  it('adds page + pageSize when provided', async () => {
    const promise = sut.search({ q: 'a', page: 2, pageSize: 50 });
    const req = http.expectOne((r) => r.url === '/api/search');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('50');
    req.flush(PAGED);
    await promise;
  });

  it('returns PagedResult on 200', async () => {
    const promise = sut.search({ q: 'x' });
    http.expectOne((r) => r.url === '/api/search').flush(PAGED);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) {
      expect(res.value.items).toHaveLength(1);
      expect(res.value.total).toBe(1);
    }
  });

  it('returns server feature error on 500', async () => {
    const promise = sut.search({ q: 'x' });
    http
      .expectOne((r) => r.url === '/api/search')
      .flush('', { status: 500, statusText: 'Server Error' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('server');
  });
});

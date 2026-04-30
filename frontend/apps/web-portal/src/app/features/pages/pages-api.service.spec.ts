import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PagesApiService } from './pages-api.service';
import type { PublicPage } from './page.types';

const SAMPLE_PAGE: PublicPage = {
  id: 'page-1',
  slug: 'about',
  pageType: 'AboutPlatform',
  titleAr: 'عن المنصة',
  titleEn: 'About the Platform',
  contentAr: '<p>محتوى عربي</p>',
  contentEn: '<p>English content</p>',
};

describe('PagesApiService', () => {
  let sut: PagesApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(PagesApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it("getBySlug('about') GETs /api/pages/about and returns { ok: true, value: page }", async () => {
    const promise = sut.getBySlug('about');
    const req = http.expectOne('/api/pages/about');
    expect(req.request.method).toBe('GET');
    req.flush(SAMPLE_PAGE);
    const res = await promise;
    expect(res).toEqual({ ok: true, value: SAMPLE_PAGE });
  });

  it("returns { ok: false, error: { kind: 'not-found' } } on 404", async () => {
    const promise = sut.getBySlug('about');
    http.expectOne('/api/pages/about').flush('Not Found', { status: 404, statusText: 'Not Found' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('not-found');
  });

  it("URL-encodes slugs with special characters: getBySlug('foo/bar') hits /api/pages/foo%2Fbar", async () => {
    const promise = sut.getBySlug('foo/bar');
    const req = http.expectOne('/api/pages/foo%2Fbar');
    expect(req.request.method).toBe('GET');
    req.flush(SAMPLE_PAGE);
    const res = await promise;
    expect(res.ok).toBe(true);
  });
});

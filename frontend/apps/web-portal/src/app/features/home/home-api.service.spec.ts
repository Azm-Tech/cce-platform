import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HomeApiService } from './home-api.service';
import type { HomepageSection } from './home.types';

const SAMPLE_SECTION: HomepageSection = {
  id: 'sec-1',
  sectionType: 'Hero',
  orderIndex: 0,
  contentAr: '<h1>مرحبا</h1>',
  contentEn: '<h1>Welcome</h1>',
  isActive: true,
};

describe('HomeApiService', () => {
  let sut: HomeApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(HomeApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('listSections() GETs /api/homepage-sections and returns { ok: true, value: array }', async () => {
    const promise = sut.listSections();
    const req = http.expectOne('/api/homepage-sections');
    expect(req.request.method).toBe('GET');
    req.flush([SAMPLE_SECTION]);
    const res = await promise;
    expect(res).toEqual({ ok: true, value: [SAMPLE_SECTION] });
  });

  it('returns { ok: false, error: { kind: "server" } } on a 500 flush', async () => {
    const promise = sut.listSections();
    http.expectOne('/api/homepage-sections').flush('Internal Server Error', {
      status: 500,
      statusText: 'Server Error',
    });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('server');
  });

  it('returns { ok: false, error: { kind: "network" } } on a network error', async () => {
    const promise = sut.listSections();
    http.expectOne('/api/homepage-sections').error(new ProgressEvent('error'), { status: 0 });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('network');
  });
});

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { KnowledgeMapsApiService } from './knowledge-maps-api.service';
import type { KnowledgeMap } from './knowledge-maps.types';

const SAMPLE: KnowledgeMap = {
  id: 'm1',
  nameAr: 'خريطة', nameEn: 'Map',
  descriptionAr: 'وصف', descriptionEn: 'Description',
  slug: 'main',
  isActive: true,
};

describe('KnowledgeMapsApiService', () => {
  let sut: KnowledgeMapsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(KnowledgeMapsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('listMaps GETs /api/knowledge-maps', async () => {
    const promise = sut.listMaps();
    const req = http.expectOne('/api/knowledge-maps');
    expect(req.request.method).toBe('GET');
    req.flush([SAMPLE]);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toEqual([SAMPLE]);
  });

  it('returns server error on 500', async () => {
    const promise = sut.listMaps();
    http.expectOne('/api/knowledge-maps').flush('', { status: 500, statusText: 'Server Error' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('server');
  });
});

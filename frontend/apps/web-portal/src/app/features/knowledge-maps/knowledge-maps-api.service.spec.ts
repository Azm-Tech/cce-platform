import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { KnowledgeMapsApiService } from './knowledge-maps-api.service';
import type { InteractiveMap } from './knowledge-maps.types';

const SAMPLE: InteractiveMap = {
  id: 'm1',
  nameAr: 'خريطة', nameEn: 'Map',
  descriptionAr: 'وصف', descriptionEn: 'Description',
  nodes: [],
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

  it('listMaps GETs /api/interactive-maps', async () => {
    const promise = sut.listMaps();
    const req = http.expectOne('/api/interactive-maps');
    expect(req.request.method).toBe('GET');
    req.flush({ data: [SAMPLE] });
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toEqual([SAMPLE]);
  });

  it('returns server error on 500', async () => {
    const promise = sut.listMaps();
    http.expectOne('/api/interactive-maps').flush('', { status: 500, statusText: 'Server Error' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('server');
  });

  it('getMap GETs /api/interactive-maps/{id}', async () => {
    const promise = sut.getMap('m1');
    const req = http.expectOne('/api/interactive-maps/m1');
    expect(req.request.method).toBe('GET');
    req.flush({ data: SAMPLE });
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value.id).toBe('m1');
  });

  it('getMap returns not-found on 404', async () => {
    const promise = sut.getMap('missing');
    http
      .expectOne('/api/interactive-maps/missing')
      .flush('', { status: 404, statusText: 'Not Found' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('not-found');
  });

  it('getMap returns server error on 500', async () => {
    const promise = sut.getMap('m1');
    http
      .expectOne('/api/interactive-maps/m1')
      .flush('', { status: 500, statusText: 'Server Error' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('server');
  });
});

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { KnowledgeMapsApiService } from './knowledge-maps-api.service';
import type {
  KnowledgeMap,
  KnowledgeMapEdge,
  KnowledgeMapNode,
} from './knowledge-maps.types';

const SAMPLE: KnowledgeMap = {
  id: 'm1',
  nameAr: 'خريطة', nameEn: 'Map',
  descriptionAr: 'وصف', descriptionEn: 'Description',
  slug: 'main',
  isActive: true,
};

const NODE: KnowledgeMapNode = {
  id: 'n1',
  mapId: 'm1',
  nameAr: 'تقنية', nameEn: 'Technology',
  nodeType: 'Technology',
  descriptionAr: null,
  descriptionEn: null,
  iconUrl: null,
  layoutX: 100,
  layoutY: 200,
  orderIndex: 0,
};

const EDGE: KnowledgeMapEdge = {
  id: 'e1',
  mapId: 'm1',
  fromNodeId: 'n1',
  toNodeId: 'n2',
  relationshipType: 'ParentOf',
  orderIndex: 0,
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

  it('getMap GETs /api/knowledge-maps/{id}', async () => {
    const promise = sut.getMap('m1');
    const req = http.expectOne('/api/knowledge-maps/m1');
    expect(req.request.method).toBe('GET');
    req.flush(SAMPLE);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value.id).toBe('m1');
  });

  it('getMap returns not-found on 404', async () => {
    const promise = sut.getMap('missing');
    http
      .expectOne('/api/knowledge-maps/missing')
      .flush('', { status: 404, statusText: 'Not Found' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('not-found');
  });

  it('getNodes GETs /api/knowledge-maps/{id}/nodes', async () => {
    const promise = sut.getNodes('m1');
    const req = http.expectOne('/api/knowledge-maps/m1/nodes');
    expect(req.request.method).toBe('GET');
    req.flush([NODE]);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toEqual([NODE]);
  });

  it('getEdges GETs /api/knowledge-maps/{id}/edges', async () => {
    const promise = sut.getEdges('m1');
    const req = http.expectOne('/api/knowledge-maps/m1/edges');
    expect(req.request.method).toBe('GET');
    req.flush([EDGE]);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toEqual([EDGE]);
  });

  it('getNodes returns server error on 500', async () => {
    const promise = sut.getNodes('m1');
    http
      .expectOne('/api/knowledge-maps/m1/nodes')
      .flush('', { status: 500, statusText: 'Server Error' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('server');
  });
});

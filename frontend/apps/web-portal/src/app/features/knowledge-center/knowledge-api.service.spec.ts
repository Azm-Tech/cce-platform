import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { KnowledgeApiService } from './knowledge-api.service';
import type { PagedResult, Resource, ResourceCategory, ResourceListItem } from './knowledge.types';

const SAMPLE_CATEGORY: ResourceCategory = {
  id: 'cat-1',
  nameAr: 'تصنيف',
  nameEn: 'Category',
  slug: 'category',
  parentId: null,
  orderIndex: 0,
};

const SAMPLE_LIST_ITEM: ResourceListItem = {
  id: 'r1',
  titleAr: 'مورد',
  titleEn: 'Resource',
  resourceType: 'Pdf',
  categoryId: 'cat-1',
  countryId: null,
  publishedOn: '2026-01-01',
  viewCount: 42,
};

const SAMPLE_RESOURCE: Resource = {
  ...SAMPLE_LIST_ITEM,
  descriptionAr: 'وصف',
  descriptionEn: 'Description',
  uploadedById: 'user-1',
  assetFileId: 'file-1',
  isCenterManaged: true,
};

const SAMPLE_PAGED: PagedResult<ResourceListItem> = {
  items: [SAMPLE_LIST_ITEM],
  page: 2,
  pageSize: 50,
  total: 100,
};

describe('KnowledgeApiService', () => {
  let sut: KnowledgeApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(KnowledgeApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('listCategories() GETs /api/categories and returns { ok: true, value: array }', async () => {
    const promise = sut.listCategories();
    const req = http.expectOne('/api/categories');
    expect(req.request.method).toBe('GET');
    req.flush([SAMPLE_CATEGORY]);
    const res = await promise;
    expect(res).toEqual({ ok: true, value: [SAMPLE_CATEGORY] });
  });

  it('listResources() builds query string with all 5 params', async () => {
    const promise = sut.listResources({
      page: 2,
      pageSize: 50,
      categoryId: 'c1',
      countryId: 'co1',
      resourceType: 'Pdf',
    });
    const req = http.expectOne((r) => r.url === '/api/resources');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.get('categoryId')).toBe('c1');
    expect(req.request.params.get('countryId')).toBe('co1');
    expect(req.request.params.get('resourceType')).toBe('Pdf');
    req.flush(SAMPLE_PAGED);
    const res = await promise;
    expect(res).toEqual({ ok: true, value: SAMPLE_PAGED });
  });

  it('getResource("r1") GETs /api/resources/r1', async () => {
    const promise = sut.getResource('r1');
    const req = http.expectOne('/api/resources/r1');
    expect(req.request.method).toBe('GET');
    req.flush(SAMPLE_RESOURCE);
    const res = await promise;
    expect(res).toEqual({ ok: true, value: SAMPLE_RESOURCE });
  });

  it('download("r1") GETs /api/resources/r1/download with responseType blob and returns { ok: true, value: Blob }', async () => {
    const promise = sut.download('r1');
    const req = http.expectOne('/api/resources/r1/download');
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob(['data'], { type: 'application/pdf' }));
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toBeInstanceOf(Blob);
  });

  it('getResource("missing") returns { ok: false, error: { kind: "not-found" } } on a 404 flush', async () => {
    const promise = sut.getResource('missing');
    http.expectOne('/api/resources/missing').flush('Not Found', {
      status: 404,
      statusText: 'Not Found',
    });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('not-found');
  });
});

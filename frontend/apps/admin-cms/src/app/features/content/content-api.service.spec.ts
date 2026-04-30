import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ContentApiService } from './content-api.service';
import type { AssetFile, CountryResourceRequest, Resource } from './content.types';

const RESOURCE: Resource = {
  id: 'r1',
  titleAr: 'العنوان',
  titleEn: 'Title',
  descriptionAr: 'وصف',
  descriptionEn: 'desc',
  resourceType: 'Pdf',
  categoryId: 'cat1',
  countryId: null,
  uploadedById: 'admin',
  assetFileId: 'asset1',
  publishedOn: null,
  viewCount: 0,
  isCenterManaged: true,
  isPublished: false,
  rowVersion: 'AAAAAAAAAAA=',
};

const ASSET: AssetFile = {
  id: 'asset1',
  url: 'https://cdn.example.com/asset1.pdf',
  originalFileName: 'doc.pdf',
  sizeBytes: 1024,
  mimeType: 'application/pdf',
  uploadedById: 'admin',
  uploadedOn: '2026-04-29',
  virusScanStatus: 'Clean',
  scannedOn: '2026-04-29',
};

describe('ContentApiService', () => {
  let sut: ContentApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(ContentApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('listResources passes filter params', async () => {
    const promise = sut.listResources({
      page: 2,
      pageSize: 50,
      search: 'q',
      categoryId: 'cat',
      countryId: 'co',
      isPublished: true,
    });
    const req = http.expectOne((r) => r.url === '/api/admin/resources');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.get('search')).toBe('q');
    expect(req.request.params.get('categoryId')).toBe('cat');
    expect(req.request.params.get('countryId')).toBe('co');
    expect(req.request.params.get('isPublished')).toBe('true');
    req.flush({ items: [RESOURCE], page: 2, pageSize: 50, total: 1 });
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('createResource POSTs body', async () => {
    const body = {
      titleAr: 'a',
      titleEn: 'b',
      descriptionAr: 'c',
      descriptionEn: 'd',
      resourceType: 'Pdf' as const,
      categoryId: 'cat',
      countryId: null,
      assetFileId: 'asset1',
    };
    const promise = sut.createResource(body);
    const req = http.expectOne('/api/admin/resources');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush(RESOURCE);
    const res = await promise;
    if (res.ok) expect(res.value.id).toBe('r1');
  });

  it('updateResource PUTs with rowVersion', async () => {
    const body = {
      titleAr: 'a',
      titleEn: 'b',
      descriptionAr: 'c',
      descriptionEn: 'd',
      resourceType: 'Pdf' as const,
      categoryId: 'cat',
      rowVersion: 'AAAA=',
    };
    const promise = sut.updateResource('r1', body);
    const req = http.expectOne('/api/admin/resources/r1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush(RESOURCE);
    await promise;
  });

  it('updateResource maps 409 concurrency to FeatureError', async () => {
    const promise = sut.updateResource('r1', {
      titleAr: '',
      titleEn: '',
      descriptionAr: '',
      descriptionEn: '',
      resourceType: 'Pdf',
      categoryId: 'cat',
      rowVersion: 'old',
    });
    http.expectOne('/api/admin/resources/r1').flush(
      { type: 'urn:cce:errors/concurrency', title: 'Concurrent edit' },
      { status: 409, statusText: 'Conflict' },
    );
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('concurrency');
  });

  it('publishResource POSTs to /publish', async () => {
    const promise = sut.publishResource('r1');
    const req = http.expectOne('/api/admin/resources/r1/publish');
    expect(req.request.method).toBe('POST');
    req.flush({ ...RESOURCE, isPublished: true });
    const res = await promise;
    if (res.ok) expect(res.value.isPublished).toBe(true);
  });

  it('uploadAsset POSTs FormData', async () => {
    const file = new File(['hello'], 'hello.txt', { type: 'text/plain' });
    const promise = sut.uploadAsset(file);
    const req = http.expectOne('/api/admin/assets');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeInstanceOf(FormData);
    req.flush(ASSET);
    const res = await promise;
    if (res.ok) expect(res.value.id).toBe('asset1');
  });

  it('approveCountryResourceRequest POSTs to /approve', async () => {
    const promise = sut.approveCountryResourceRequest('crr1', { adminNotesEn: 'ok' });
    const req = http.expectOne('/api/admin/country-resource-requests/crr1/approve');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ adminNotesEn: 'ok' });
    const dto: CountryResourceRequest = {
      id: 'crr1',
      countryId: 'co',
      requestedById: 'sr',
      status: 'Approved',
      proposedTitleAr: '',
      proposedTitleEn: '',
      proposedDescriptionAr: '',
      proposedDescriptionEn: '',
      proposedResourceType: 'Pdf',
      proposedAssetFileId: 'a',
      submittedOn: '2026-04-29',
      adminNotesAr: null,
      adminNotesEn: 'ok',
      processedById: 'admin',
      processedOn: '2026-04-29',
    };
    req.flush(dto);
    const res = await promise;
    if (res.ok) expect(res.value.status).toBe('Approved');
  });

  it('rejectCountryResourceRequest POSTs to /reject with notes', async () => {
    const promise = sut.rejectCountryResourceRequest('crr1', {
      adminNotesAr: 'سبب',
      adminNotesEn: 'reason',
    });
    const req = http.expectOne('/api/admin/country-resource-requests/crr1/reject');
    expect(req.request.body).toEqual({ adminNotesAr: 'سبب', adminNotesEn: 'reason' });
    req.flush({
      id: 'crr1',
      countryId: 'co',
      requestedById: 'sr',
      status: 'Rejected',
      proposedTitleAr: '',
      proposedTitleEn: '',
      proposedDescriptionAr: '',
      proposedDescriptionEn: '',
      proposedResourceType: 'Pdf',
      proposedAssetFileId: 'a',
      submittedOn: '2026-04-29',
      adminNotesAr: 'سبب',
      adminNotesEn: 'reason',
      processedById: 'admin',
      processedOn: '2026-04-29',
    } satisfies CountryResourceRequest);
    await promise;
  });
});

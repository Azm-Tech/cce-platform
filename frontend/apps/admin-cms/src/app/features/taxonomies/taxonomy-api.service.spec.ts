import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TaxonomyApiService } from './taxonomy-api.service';

describe('TaxonomyApiService', () => {
  let sut: TaxonomyApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(TaxonomyApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('listCategories builds query params', async () => {
    const p = sut.listCategories({ page: 2, pageSize: 50, parentId: 'p1', isActive: false });
    const req = http.expectOne((r) => r.url === '/api/admin/resource-categories');
    expect(req.request.params.get('parentId')).toBe('p1');
    expect(req.request.params.get('isActive')).toBe('false');
    req.flush({ items: [], page: 2, pageSize: 50, total: 0 });
    await p;
  });

  it('createCategory POSTs body', async () => {
    const p = sut.createCategory({ nameAr: 'a', nameEn: 'b', slug: 's', orderIndex: 0 });
    const req = http.expectOne('/api/admin/resource-categories');
    expect(req.request.method).toBe('POST');
    req.flush({});
    await p;
  });

  it('updateCategory PUTs', async () => {
    const p = sut.updateCategory('c1', { nameAr: 'a', nameEn: 'b', orderIndex: 0, isActive: true });
    const req = http.expectOne('/api/admin/resource-categories/c1');
    expect(req.request.method).toBe('PUT');
    req.flush({});
    await p;
  });

  it('deleteCategory DELETEs', async () => {
    const p = sut.deleteCategory('c1');
    const req = http.expectOne('/api/admin/resource-categories/c1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
    await p;
  });

  it('listTopics builds search query', async () => {
    const p = sut.listTopics({ search: 'q' });
    const req = http.expectOne((r) => r.url === '/api/admin/topics');
    expect(req.request.params.get('search')).toBe('q');
    req.flush({ items: [], page: 1, pageSize: 20, total: 0 });
    await p;
  });

  it('createTopic POSTs body', async () => {
    const p = sut.createTopic({
      nameAr: 'a', nameEn: 'b', descriptionAr: 'c', descriptionEn: 'd',
      slug: 's', orderIndex: 0,
    });
    const req = http.expectOne('/api/admin/topics');
    expect(req.request.method).toBe('POST');
    req.flush({});
    await p;
  });

  it('softDeletePost DELETEs', async () => {
    const p = sut.softDeletePost('post1');
    const req = http.expectOne('/api/admin/community/posts/post1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
    await p;
  });

  it('softDeleteReply DELETEs', async () => {
    const p = sut.softDeleteReply('reply1');
    const req = http.expectOne('/api/admin/community/replies/reply1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
    await p;
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { KnowledgeApiService, type Result } from './knowledge-api.service';
import type { PagedResult, ResourceCategory, ResourceListItem } from './knowledge.types';
import { ResourcesListPage } from './resources-list.page';

const SAMPLE: ResourceListItem = {
  id: 'r1',
  titleAr: 'عنوان', titleEn: 'Title',
  resourceType: 'Pdf',
  categoryId: 'cat-1',
  countryId: null,
  publishedOn: null,
  viewCount: 0,
};
const CATEGORIES: ResourceCategory[] = [
  { id: 'cat-1', nameAr: 'أ', nameEn: 'A', slug: 'a', parentId: null, orderIndex: 0 },
];

describe('ResourcesListPage', () => {
  let fixture: ComponentFixture<ResourcesListPage>;
  let page: ResourcesListPage;
  let listResources: jest.Mock;
  let listCategories: jest.Mock;
  let routerNavigate: jest.Mock;
  let queryParamGet: jest.Mock;

  function ok(value: PagedResult<ResourceListItem>): Result<PagedResult<ResourceListItem>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listResources = jest.fn().mockResolvedValue(ok({ items: [SAMPLE], page: 1, pageSize: 20, total: 1 }));
    listCategories = jest.fn().mockResolvedValue({ ok: true, value: CATEGORIES });
    routerNavigate = jest.fn();
    queryParamGet = jest.fn().mockReturnValue(null);

    const localeSig = signal<'ar' | 'en'>('en');
    const localeStub = { locale: localeSig.asReadonly() };

    await TestBed.configureTestingModule({
      imports: [ResourcesListPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: KnowledgeApiService, useValue: { listResources, listCategories } },
        { provide: LocaleService, useValue: localeStub },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: queryParamGet } } } },
        { provide: Router, useValue: { navigate: routerNavigate } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ResourcesListPage);
    page = fixture.componentInstance;
  });

  it('loads categories + resources on init with default paging', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listCategories).toHaveBeenCalled();
    expect(listResources).toHaveBeenCalledWith({
      page: 1, pageSize: 20,
      categoryId: undefined, countryId: undefined, resourceType: undefined,
    });
  });

  it('reads query params on init (page=2, categoryId, resourceType)', async () => {
    queryParamGet.mockImplementation((k: string) => {
      const m: Record<string, string> = { page: '2', categoryId: 'cat-1', resourceType: 'Pdf' };
      return m[k] ?? null;
    });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.page()).toBe(2);
    expect(page.categoryId()).toBe('cat-1');
    expect(page.resourceType()).toBe('Pdf');
    expect(listResources).toHaveBeenCalledWith({
      page: 2, pageSize: 20, categoryId: 'cat-1', countryId: undefined, resourceType: 'Pdf',
    });
  });

  it('onCategoryChange resets page to 1 and reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.page.set(3);
    listResources.mockClear();
    page.onCategoryChange('cat-2');
    await Promise.resolve();
    expect(page.page()).toBe(1);
    expect(page.categoryId()).toBe('cat-2');
    expect(listResources).toHaveBeenCalledWith(expect.objectContaining({ page: 1, categoryId: 'cat-2' }));
  });

  it('onPage updates page and pageSize and reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listResources.mockClear();
    page.onPage({ pageIndex: 2, pageSize: 50, length: 1, previousPageIndex: 0 });
    await Promise.resolve();
    expect(page.page()).toBe(3);
    expect(page.pageSize()).toBe(50);
  });

  it('updates URL query params on filter change', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    routerNavigate.mockClear();
    page.onCategoryChange('cat-2');
    await Promise.resolve();
    expect(routerNavigate).toHaveBeenCalled();
  });

  it('renders error banner when api fails', async () => {
    listResources.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.errorKind()).toBe('server');
  });

  it('empty result triggers empty() computed', async () => {
    listResources.mockResolvedValueOnce(ok({ items: [], page: 1, pageSize: 20, total: 0 }));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.empty()).toBe(true);
  });

  it('onCountryChange and onResourceTypeChange both reset page + reload', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.page.set(5);
    listResources.mockClear();

    page.onCountryChange('SAU');
    await Promise.resolve();
    expect(page.page()).toBe(1);
    expect(page.countryId()).toBe('SAU');

    page.page.set(5);
    listResources.mockClear();
    page.onResourceTypeChange('Video');
    await Promise.resolve();
    expect(page.page()).toBe(1);
    expect(page.resourceType()).toBe('Video');
  });
});

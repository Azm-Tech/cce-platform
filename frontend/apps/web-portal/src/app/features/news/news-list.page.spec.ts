import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { NewsApiService, type Result } from './news-api.service';
import type { NewsArticle, PagedResult } from './news.types';
import { NewsListPage } from './news-list.page';

const SAMPLE: NewsArticle = {
  id: 'n1',
  titleAr: 'عنوان', titleEn: 'Title',
  contentAr: 'محتوى', contentEn: 'content',
  slug: 's1',
  authorId: 'a',
  featuredImageUrl: null,
  publishedOn: '2026-04-29',
  isFeatured: true,
  isPublished: true,
};

describe('NewsListPage', () => {
  let fixture: ComponentFixture<NewsListPage>;
  let page: NewsListPage;
  let listNews: jest.Mock;
  let routerNavigate: jest.Mock;
  let queryParamGet: jest.Mock;

  function ok(value: PagedResult<NewsArticle>): Result<PagedResult<NewsArticle>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listNews = jest.fn().mockResolvedValue(ok({ items: [SAMPLE], page: 1, pageSize: 12, total: 1 }));
    queryParamGet = jest.fn().mockReturnValue(null);

    const localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [NewsListPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: NewsApiService, useValue: { listNews } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: queryParamGet } } } },
      ],
    }).compileComponents();
    // Spy on the real Router instance (RouterLink needs the real one for its events stream).
    const router = TestBed.inject(Router);
    routerNavigate = jest.spyOn(router, 'navigate').mockResolvedValue(true) as unknown as jest.Mock;
    fixture = TestBed.createComponent(NewsListPage);
    page = fixture.componentInstance;
  });

  it('loads on init with default paging', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listNews).toHaveBeenCalledWith({ page: 1, pageSize: 12, isFeatured: undefined });
  });

  it('reads featured query param on init', async () => {
    queryParamGet.mockImplementation((k: string) => (k === 'featured' ? 'true' : null));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.featuredOnly()).toBe(true);
    expect(listNews).toHaveBeenCalledWith({ page: 1, pageSize: 12, isFeatured: true });
  });

  it('onFeaturedToggle resets page + reloads with isFeatured', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.page.set(3);
    listNews.mockClear();
    page.onFeaturedToggle(true);
    await Promise.resolve();
    expect(page.page()).toBe(1);
    expect(listNews).toHaveBeenCalledWith({ page: 1, pageSize: 12, isFeatured: true });
  });

  it('onPage updates page + size and reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listNews.mockClear();
    page.onPage({ pageIndex: 2, pageSize: 24, length: 1, previousPageIndex: 0 });
    await Promise.resolve();
    expect(page.page()).toBe(3);
    expect(page.pageSize()).toBe(24);
  });

  it('updates URL on filter change', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    routerNavigate.mockClear();
    page.onFeaturedToggle(true);
    await Promise.resolve();
    expect(routerNavigate).toHaveBeenCalled();
  });

  it('renders error banner when api fails', async () => {
    listNews.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.errorKind()).toBe('server');
  });

  it('empty result triggers empty() computed', async () => {
    listNews.mockResolvedValueOnce(ok({ items: [], page: 1, pageSize: 12, total: 0 }));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.empty()).toBe(true);
  });

  it('renders one card per article', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('cce-news-card')).toHaveLength(1);
  });
});

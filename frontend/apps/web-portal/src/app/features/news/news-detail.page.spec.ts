import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { NewsApiService, type Result } from './news-api.service';
import type { NewsArticle } from './news.types';
import { NewsDetailPage } from './news-detail.page';

const SAMPLE: NewsArticle = {
  id: 'n1',
  titleAr: 'العنوان', titleEn: 'Title',
  contentAr: '<p>محتوى</p>', contentEn: '<p>content</p>',
  slug: 'hello',
  authorId: 'a',
  featuredImageUrl: null,
  publishedOn: '2026-04-29',
  isFeatured: false,
  isPublished: true,
};

describe('NewsDetailPage', () => {
  let fixture: ComponentFixture<NewsDetailPage>;
  let page: NewsDetailPage;
  let getBySlug: jest.Mock;
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;
  let paramMapGet: jest.Mock;

  function ok<T>(value: T): Result<T> { return { ok: true, value }; }

  beforeEach(async () => {
    getBySlug = jest.fn().mockResolvedValue(ok(SAMPLE));
    localeSig = signal<'ar' | 'en'>('en');
    paramMapGet = jest.fn().mockReturnValue('hello');

    await TestBed.configureTestingModule({
      imports: [NewsDetailPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: NewsApiService, useValue: { getBySlug } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: paramMapGet } } } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(NewsDetailPage);
    page = fixture.componentInstance;
  });

  it('loads on init from slug', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(getBySlug).toHaveBeenCalledWith('hello');
    expect(page.article()).toEqual(SAMPLE);
  });

  it('sets errorKind on 404', async () => {
    getBySlug.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.errorKind()).toBe('not-found');
  });

  it('sets errorKind:not-found when slug param is null', async () => {
    paramMapGet.mockReturnValueOnce(null);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.errorKind()).toBe('not-found');
    expect(getBySlug).not.toHaveBeenCalled();
  });

  it('title/content computed reflects locale', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.title()).toBe('Title');
    expect(page.content()).toBe('<p>content</p>');
    localeSig.set('ar');
    expect(page.title()).toBe('العنوان');
    expect(page.content()).toBe('<p>محتوى</p>');
  });

  it('renders back link to /news', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const back = fixture.nativeElement.querySelector('.cce-news-detail__back');
    expect(back).not.toBeNull();
    expect(back.getAttribute('href')).toBe('/news');
  });
});

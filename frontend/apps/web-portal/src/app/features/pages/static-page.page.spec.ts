import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { PagesApiService, type Result } from './pages-api.service';
import type { PublicPage } from './page.types';
import { StaticPagePage } from './static-page.page';

const SAMPLE_PAGE: PublicPage = {
  id: 'page-1',
  slug: 'about',
  pageType: 'AboutPlatform',
  titleAr: 'عن المنصة',
  titleEn: 'About the Platform',
  contentAr: '<p>محتوى عربي</p>',
  contentEn: '<p>English content</p>',
};

function ok(value: PublicPage): Result<PublicPage> {
  return { ok: true, value };
}

describe('StaticPagePage', () => {
  let fixture: ComponentFixture<StaticPagePage>;
  let component: StaticPagePage;
  let getBySlug: jest.Mock;
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;

  async function setup(slugValue: string | null = 'about') {
    getBySlug = jest.fn().mockResolvedValue(ok(SAMPLE_PAGE));
    localeSig = signal<'ar' | 'en'>('en');
    const localeStub = { locale: localeSig.asReadonly() };

    await TestBed.configureTestingModule({
      imports: [StaticPagePage, TranslateModule.forRoot()],
      providers: [
        { provide: PagesApiService, useValue: { getBySlug } },
        { provide: LocaleService, useValue: localeStub },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => slugValue } } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StaticPagePage);
    component = fixture.componentInstance;
  }

  it('loads page on init using slug from ActivatedRoute.paramMap', async () => {
    await setup('about');

    fixture.detectChanges();
    await fixture.whenStable();

    expect(getBySlug).toHaveBeenCalledWith('about');
    expect(component.page()).toEqual(SAMPLE_PAGE);
    expect(component.errorKind()).toBeNull();
  });

  it("sets errorKind 'not-found' when route paramMap returns null", async () => {
    await setup(null);

    fixture.detectChanges();
    await fixture.whenStable();

    expect(getBySlug).not.toHaveBeenCalled();
    expect(component.errorKind()).toBe('not-found');
  });

  it("sets errorKind from api error (404 → 'not-found')", async () => {
    await setup('about');
    getBySlug.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });

    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.errorKind()).toBe('not-found');
    expect(component.page()).toBeNull();
  });

  it("title() computed returns titleAr in 'ar' locale and titleEn in 'en' locale", async () => {
    await setup('about');

    fixture.detectChanges();
    await fixture.whenStable();

    // Default locale is 'en'
    expect(component.title()).toBe('About the Platform');

    // Switch to Arabic
    localeSig.set('ar');
    expect(component.title()).toBe('عن المنصة');

    // Switch back to English
    localeSig.set('en');
    expect(component.title()).toBe('About the Platform');
  });

  it("content() computed returns contentAr in 'ar' locale and contentEn in 'en' locale", async () => {
    await setup('about');

    fixture.detectChanges();
    await fixture.whenStable();

    // Default locale is 'en'
    expect(component.content()).toBe('<p>English content</p>');

    // Switch to Arabic
    localeSig.set('ar');
    expect(component.content()).toBe('<p>محتوى عربي</p>');

    // Switch back to English
    localeSig.set('en');
    expect(component.content()).toBe('<p>English content</p>');
  });
});

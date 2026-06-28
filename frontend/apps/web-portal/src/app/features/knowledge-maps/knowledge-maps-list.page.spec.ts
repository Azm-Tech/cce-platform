import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { KnowledgeMapsApiService, type Result } from './knowledge-maps-api.service';
import type { InteractiveMap } from './knowledge-maps.types';
import { InteractiveMapsListPage } from './knowledge-maps-list.page';

const SAMPLE: InteractiveMap = {
  id: 'm1',
  nameAr: 'خريطة', nameEn: 'Map',
  descriptionAr: 'وصف', descriptionEn: 'Description',
  nodes: [],
};

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('InteractiveMapsListPage', () => {
  let fixture: ComponentFixture<InteractiveMapsListPage>;
  let page: InteractiveMapsListPage;
  let listMaps: jest.Mock;

  beforeEach(async () => {
    listMaps = jest.fn().mockResolvedValue(ok([SAMPLE]));

    await TestBed.configureTestingModule({
      imports: [InteractiveMapsListPage, TranslocoTestingModule.forRoot({ langs: { en: {}, ar: {} }, translocoConfig: { availableLangs: ['en', 'ar'], defaultLang: 'en' } })],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: KnowledgeMapsApiService, useValue: { listMaps } },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(InteractiveMapsListPage);
    page = fixture.componentInstance;
  });

  it('init load renders one card per map', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(listMaps).toHaveBeenCalled();
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('Map');
  });

  it('renders each map as a routerLink to /knowledge-maps/:id', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const link = fixture.nativeElement.querySelector(
      `a[href$="/knowledge-maps/${SAMPLE.id}"]`,
    );
    expect(link).toBeTruthy();
  });

  it('empty result triggers empty() computed', async () => {
    listMaps.mockResolvedValueOnce(ok([]));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.empty()).toBe(true);
  });
});

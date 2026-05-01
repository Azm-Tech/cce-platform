import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { InteractiveCityApiService, type Result } from './interactive-city-api.service';
import type { CityTechnology } from './interactive-city.types';
import { InteractiveCityPage } from './interactive-city.page';

const SAMPLE: CityTechnology = {
  id: 't1',
  nameAr: 'تقنية', nameEn: 'Tech',
  descriptionAr: 'وصف', descriptionEn: 'Description',
  categoryAr: 'فئة', categoryEn: 'Category',
  carbonImpactKgPerYear: 100, costUsd: 1000,
  iconUrl: null,
};

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('InteractiveCityPage', () => {
  let fixture: ComponentFixture<InteractiveCityPage>;
  let page: InteractiveCityPage;
  let listTechnologies: jest.Mock;

  beforeEach(async () => {
    listTechnologies = jest.fn().mockResolvedValue(ok([SAMPLE]));

    await TestBed.configureTestingModule({
      imports: [InteractiveCityPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: InteractiveCityApiService, useValue: { listTechnologies } },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(InteractiveCityPage);
    page = fixture.componentInstance;
  });

  it('init load renders one chip per technology', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(listTechnologies).toHaveBeenCalled();
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('Tech');
  });

  it('renders the "coming in Sub-7" notice', async () => {
    fixture.detectChanges();
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('interactiveCity.comingSoon');
  });

  it('empty result triggers empty() computed', async () => {
    listTechnologies.mockResolvedValueOnce(ok([]));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.empty()).toBe(true);
  });
});

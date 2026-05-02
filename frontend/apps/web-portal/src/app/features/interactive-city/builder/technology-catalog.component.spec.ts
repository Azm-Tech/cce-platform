import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { AuthService } from '../../../core/auth/auth.service';
import { InteractiveCityApiService } from '../interactive-city-api.service';
import type { CityTechnology } from '../interactive-city.types';
import { ScenarioBuilderStore } from './scenario-builder-store.service';
import { TechnologyCatalogComponent } from './technology-catalog.component';

const TECH_A: CityTechnology = {
  id: 'a',
  nameAr: 'ألف', nameEn: 'Alpha',
  descriptionAr: '', descriptionEn: '',
  categoryAr: 'تقنية', categoryEn: 'Tech',
  carbonImpactKgPerYear: -1000, costUsd: 5000,
  iconUrl: null,
};
const TECH_B: CityTechnology = {
  ...TECH_A, id: 'b', nameEn: 'Bravo',
  categoryEn: 'Tech',
  carbonImpactKgPerYear: 200,
};
const TECH_C: CityTechnology = {
  ...TECH_A, id: 'c', nameEn: 'Charlie',
  categoryEn: 'Energy', categoryAr: 'طاقة',
};

describe('TechnologyCatalogComponent', () => {
  let fixture: ComponentFixture<TechnologyCatalogComponent>;
  let store: ScenarioBuilderStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TechnologyCatalogComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        ScenarioBuilderStore,
        {
          provide: InteractiveCityApiService,
          useValue: {
            listTechnologies: jest.fn(),
            runScenario: jest.fn(),
            listMyScenarios: jest.fn(),
            saveScenario: jest.fn(),
            deleteMyScenario: jest.fn(),
          },
        },
        { provide: AuthService, useValue: { isAuthenticated: signal<boolean>(false).asReadonly() } },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(TechnologyCatalogComponent);
    store = fixture.debugElement.injector.get(ScenarioBuilderStore);
    store._seedCatalog([TECH_A, TECH_B, TECH_C]);
  });

  it('renders one card per technology, grouped by category', () => {
    fixture.detectChanges();
    const cards = fixture.nativeElement.querySelectorAll('.cce-catalog__card');
    expect(cards.length).toBe(3);
    const groupTitles = Array.from(
      fixture.nativeElement.querySelectorAll('.cce-catalog__group-title') as NodeListOf<HTMLElement>,
    ).map((el) => el.textContent?.trim());
    expect(groupTitles).toEqual(['Tech', 'Energy']);
  });

  it('clicking a card calls store.toggle', () => {
    fixture.detectChanges();
    const firstCard = fixture.nativeElement.querySelector('.cce-catalog__card') as HTMLButtonElement;
    firstCard.click();
    fixture.detectChanges();
    expect(store.selectedIds().has('a')).toBe(true);
  });

  it('selected card has aria-pressed="true" and a visual ring', () => {
    store.toggle('a');
    fixture.detectChanges();
    const cards = Array.from(fixture.nativeElement.querySelectorAll('.cce-catalog__card') as NodeListOf<HTMLButtonElement>);
    const cardA = cards.find((c) => (c.getAttribute('aria-label') ?? '').includes('Alpha'));
    expect(cardA?.getAttribute('aria-pressed')).toBe('true');
    expect(cardA?.classList.contains('cce-catalog__card--selected')).toBe(true);
  });

  it('search input narrows the list (debounced 200ms)', fakeAsync(() => {
    fixture.detectChanges();
    fixture.componentInstance.searchControl.setValue('Charlie');
    tick(250);
    fixture.detectChanges();
    const cards = fixture.nativeElement.querySelectorAll('.cce-catalog__card');
    expect(cards.length).toBe(1);
    expect((cards[0] as HTMLElement).textContent).toContain('Charlie');
  }));

  it('shows the empty-search message when no rows match', fakeAsync(() => {
    fixture.detectChanges();
    fixture.componentInstance.searchControl.setValue('zzzzz-no-match');
    tick(250);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('interactiveCity.catalog.empty');
  }));
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { AuthService } from '../../../core/auth/auth.service';
import { InteractiveCityApiService } from '../interactive-city-api.service';
import type { CityTechnology } from '../interactive-city.types';
import { ScenarioBuilderStore } from './scenario-builder-store.service';
import { SelectedListComponent } from './selected-list.component';

const TECH_A: CityTechnology = {
  id: 'a',
  nameAr: 'ألف', nameEn: 'Alpha',
  descriptionAr: '', descriptionEn: '',
  categoryAr: 'تقنية', categoryEn: 'Tech',
  carbonImpactKgPerYear: -1000, costUsd: 5000,
  iconUrl: null,
};
const TECH_B: CityTechnology = { ...TECH_A, id: 'b', nameEn: 'Bravo' };

describe('SelectedListComponent', () => {
  let fixture: ComponentFixture<SelectedListComponent>;
  let store: ScenarioBuilderStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SelectedListComponent, TranslateModule.forRoot()],
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
    fixture = TestBed.createComponent(SelectedListComponent);
    store = fixture.debugElement.injector.get(ScenarioBuilderStore);
    store._seedCatalog([TECH_A, TECH_B]);
  });

  it('renders the empty state when no selection', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('interactiveCity.selected.empty');
  });

  it('renders one row per selected technology', () => {
    store.toggle('a');
    store.toggle('b');
    fixture.detectChanges();
    const items = fixture.nativeElement.querySelectorAll('.cce-selected__item');
    expect(items.length).toBe(2);
    expect(fixture.nativeElement.textContent).toContain('Alpha');
    expect(fixture.nativeElement.textContent).toContain('Bravo');
  });

  it('clicking remove × calls store.toggle on that id', () => {
    store.toggle('a');
    fixture.detectChanges();
    const removeBtn = fixture.nativeElement.querySelector(
      '.cce-selected__item button[aria-label]',
    ) as HTMLButtonElement;
    expect(removeBtn).toBeTruthy();
    removeBtn.click();
    expect(store.selectedIds().has('a')).toBe(false);
  });

  it('Clear all button calls store.clear', () => {
    store.toggle('a');
    store.toggle('b');
    fixture.detectChanges();
    const clearBtn = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((b) => (b.textContent ?? '').includes('interactiveCity.selected.clear'));
    expect(clearBtn).toBeTruthy();
    clearBtn?.click();
    expect(store.selectedIds().size).toBe(0);
  });

  it('Clear all button is disabled when nothing is selected', () => {
    fixture.detectChanges();
    const clearBtn = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((b) => (b.textContent ?? '').includes('interactiveCity.selected.clear'));
    expect(clearBtn?.disabled).toBe(true);
  });
});

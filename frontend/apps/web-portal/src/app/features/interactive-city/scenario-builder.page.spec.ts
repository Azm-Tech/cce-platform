import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { InteractiveCityApiService, type Result } from './interactive-city-api.service';
import type { CityTechnology } from './interactive-city.types';
import { ScenarioBuilderPage } from './scenario-builder.page';

const VALID_GUID_A = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

const TECH: CityTechnology = {
  id: VALID_GUID_A,
  nameAr: 'أ', nameEn: 'A',
  descriptionAr: 'وصف', descriptionEn: 'desc',
  categoryAr: 'فئة', categoryEn: 'cat',
  carbonImpactKgPerYear: -1000, costUsd: 5000,
  iconUrl: null,
};

function ok<T>(value: T): Result<T> { return { ok: true, value }; }

describe('ScenarioBuilderPage', () => {
  let fixture: ComponentFixture<ScenarioBuilderPage>;
  let api: jest.Mocked<InteractiveCityApiService>;
  let routerNavigate: jest.SpyInstance;

  function setUp(queryParams: Record<string, string>): void {
    api = {
      listTechnologies: jest.fn().mockResolvedValue(ok([TECH])),
      runScenario: jest.fn(),
      listMyScenarios: jest.fn().mockResolvedValue(ok([])),
      saveScenario: jest.fn(),
      deleteMyScenario: jest.fn(),
    } as unknown as jest.Mocked<InteractiveCityApiService>;

    TestBed.configureTestingModule({
      imports: [ScenarioBuilderPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: InteractiveCityApiService, useValue: api },
        { provide: AuthService, useValue: { isAuthenticated: signal<boolean>(false).asReadonly() } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap(queryParams) },
          },
        },
      ],
    });
    const router = TestBed.inject(Router);
    routerNavigate = jest.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(ScenarioBuilderPage);
  }

  it('hydrates from URL and calls store.init()', async () => {
    setUp({ city: 'Industrial', year: '2035', t: VALID_GUID_A, name: 'My City' });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(api.listTechnologies).toHaveBeenCalled();
    const store = fixture.componentInstance.store;
    expect(store.cityType()).toBe('Industrial');
    expect(store.targetYear()).toBe(2035);
    expect(store.name()).toBe('My City');
    expect(Array.from(store.selectedIds())).toEqual([VALID_GUID_A]);
  });

  it('hydration does not mark state dirty', async () => {
    setUp({ city: 'Coastal' });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.componentInstance.store.dirty()).toBe(false);
  });

  it('debounces URL sync after edits', fakeAsync(() => {
    setUp({});
    fixture.detectChanges();
    routerNavigate.mockClear();
    const store = fixture.componentInstance.store;
    store.setCityType('Industrial');
    fixture.detectChanges();
    // Immediately after edit, no navigate yet (200ms debounce).
    expect(routerNavigate).not.toHaveBeenCalled();
    tick(250);
    expect(routerNavigate).toHaveBeenCalled();
    expect(routerNavigate.mock.calls[0][1].queryParams.city).toBe('Industrial');
  }));
});

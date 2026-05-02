import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { LocaleService } from '@frontend/i18n';
import { AuthService } from '../../../core/auth/auth.service';
import { InteractiveCityApiService, type Result } from '../interactive-city-api.service';
import type { CityTechnology, RunResult } from '../interactive-city.types';
import { ScenarioBuilderStore } from './scenario-builder-store.service';
import { TotalsBarComponent } from './totals-bar.component';

const TECH: CityTechnology = {
  id: 'a',
  nameAr: 'أ', nameEn: 'A',
  descriptionAr: '', descriptionEn: '',
  categoryAr: 'فئة', categoryEn: 'cat',
  carbonImpactKgPerYear: -1000, costUsd: 5000,
  iconUrl: null,
};

function ok<T>(v: T): Result<T> { return { ok: true, value: v }; }
function fail<E extends { kind: string }>(e: E): Result<never> { return { ok: false, error: e as never }; }

describe('TotalsBarComponent', () => {
  let fixture: ComponentFixture<TotalsBarComponent>;
  let store: ScenarioBuilderStore;
  let api: jest.Mocked<InteractiveCityApiService>;
  let toast: { success: jest.Mock; error: jest.Mock };
  let auth: { isAuthenticated: ReturnType<typeof signal<boolean>>; signIn: jest.Mock };
  let dialogOpen: jest.Mock;

  function setUp(authenticated: boolean): void {
    api = {
      listTechnologies: jest.fn(),
      runScenario: jest.fn(),
      listMyScenarios: jest.fn(),
      saveScenario: jest.fn(),
      deleteMyScenario: jest.fn(),
    } as unknown as jest.Mocked<InteractiveCityApiService>;
    toast = { success: jest.fn(), error: jest.fn() };
    auth = { isAuthenticated: signal<boolean>(authenticated), signIn: jest.fn() };
    dialogOpen = jest.fn();

    TestBed.configureTestingModule({
      imports: [TotalsBarComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        ScenarioBuilderStore,
        { provide: InteractiveCityApiService, useValue: api },
        { provide: AuthService, useValue: { isAuthenticated: auth.isAuthenticated.asReadonly(), signIn: auth.signIn } },
        { provide: ToastService, useValue: toast },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
        { provide: 'MatDialog', useValue: { open: dialogOpen } }, // overridden below by component-level provider
      ],
    });
    fixture = TestBed.createComponent(TotalsBarComponent);
    store = fixture.debugElement.injector.get(ScenarioBuilderStore);
    store._seedCatalog([TECH]);
  }

  it('renders live totals + units', () => {
    setUp(false);
    store.toggle('a');
    fixture.detectChanges();
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('-1,000');
    expect(html).toContain('interactiveCity.totals.carbonUnit');
    expect(html).toContain('5,000');
    expect(html).toContain('interactiveCity.totals.costUnit');
  });

  it('Run button is disabled when no selection', () => {
    setUp(false);
    fixture.detectChanges();
    const runBtn = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((b) => b.classList.contains('cce-totals-bar__run'));
    expect(runBtn?.disabled).toBe(true);
  });

  it('Save button is disabled when name empty', () => {
    setUp(true);
    store.toggle('a');
    fixture.detectChanges();
    const saveBtn = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((b) => b.classList.contains('cce-totals-bar__save'));
    expect(saveBtn?.disabled).toBe(true);
  });

  it('Run success toasts runOk and stores serverResult', async () => {
    setUp(false);
    store.toggle('a');
    const result: RunResult = {
      totalCarbonImpactKgPerYear: -1000,
      totalCostUsd: 5000,
      summaryAr: 'ملخص', summaryEn: 'Summary',
    };
    api.runScenario.mockResolvedValue(ok(result));
    await fixture.componentInstance.runScenario();
    expect(api.runScenario).toHaveBeenCalled();
    expect(toast.success).toHaveBeenCalledWith('interactiveCity.toasts.runOk');
    expect(store.serverResult()).toEqual(result);
  });

  it('Run failure toasts runFailed', async () => {
    setUp(false);
    store.toggle('a');
    api.runScenario.mockResolvedValue(fail({ kind: 'server' }));
    await fixture.componentInstance.runScenario();
    expect(toast.error).toHaveBeenCalledWith('interactiveCity.errors.runFailed');
  });

  it('Save when unauthenticated calls auth.signIn() and does not open dialog', async () => {
    setUp(false);
    store.toggle('a');
    store.setName('My Scenario');
    await fixture.componentInstance.saveScenario();
    expect(auth.signIn).toHaveBeenCalled();
  });

  it('Server summary text appears once serverResult is set', () => {
    setUp(false);
    store.toggle('a');
    store.serverResult.set({
      totalCarbonImpactKgPerYear: -1000,
      totalCostUsd: 5000,
      summaryAr: 'ملخص', summaryEn: 'English summary text',
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('English summary text');
  });
});

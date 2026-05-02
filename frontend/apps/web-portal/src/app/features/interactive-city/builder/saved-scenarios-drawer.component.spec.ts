import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { LocaleService } from '@frontend/i18n';
import { AuthService } from '../../../core/auth/auth.service';
import { InteractiveCityApiService, type Result } from '../interactive-city-api.service';
import {
  buildConfigurationJson,
  type SavedScenario,
} from '../interactive-city.types';
import { ScenarioBuilderStore } from './scenario-builder-store.service';
import { SavedScenariosDrawerComponent } from './saved-scenarios-drawer.component';

const SCENARIO_A: SavedScenario = {
  id: 's1',
  nameAr: 'سيناريو', nameEn: 'My Scenario',
  cityType: 'Industrial',
  targetYear: 2035,
  configurationJson: buildConfigurationJson(['t1']),
  createdOn: '2026-05-02T12:00:00Z',
};

function ok<T>(v: T): Result<T> { return { ok: true, value: v }; }

describe('SavedScenariosDrawerComponent', () => {
  let fixture: ComponentFixture<SavedScenariosDrawerComponent>;
  let store: ScenarioBuilderStore;
  let toast: { success: jest.Mock; error: jest.Mock };
  let auth: { isAuthenticated: ReturnType<typeof signal<boolean>>; signIn: jest.Mock };
  let api: jest.Mocked<InteractiveCityApiService>;
  let dialog: { open: jest.Mock };

  function setUp(authenticated: boolean): void {
    api = {
      listTechnologies: jest.fn().mockResolvedValue(ok([])),
      runScenario: jest.fn(),
      listMyScenarios: jest.fn().mockResolvedValue(ok([SCENARIO_A])),
      saveScenario: jest.fn(),
      deleteMyScenario: jest.fn().mockResolvedValue(ok(undefined)),
    } as unknown as jest.Mocked<InteractiveCityApiService>;
    toast = { success: jest.fn(), error: jest.fn() };
    auth = { isAuthenticated: signal<boolean>(authenticated), signIn: jest.fn() };
    dialog = { open: jest.fn() };

    TestBed.configureTestingModule({
      imports: [SavedScenariosDrawerComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        ScenarioBuilderStore,
        { provide: InteractiveCityApiService, useValue: api },
        { provide: AuthService, useValue: { isAuthenticated: auth.isAuthenticated.asReadonly(), signIn: auth.signIn } },
        { provide: ToastService, useValue: toast },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
        { provide: MatDialog, useValue: dialog },
      ],
    });
    fixture = TestBed.createComponent(SavedScenariosDrawerComponent);
    store = fixture.debugElement.injector.get(ScenarioBuilderStore);
  }

  it('shows the sign-in CTA card when unauthenticated', () => {
    setUp(false);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('interactiveCity.saved.signInToSaveTitle');
    expect(fixture.nativeElement.querySelector('.cce-saved-drawer__list')).toBeNull();
  });

  it('Sign in button calls auth.signIn', () => {
    setUp(false);
    fixture.detectChanges();
    fixture.componentInstance.signIn();
    expect(auth.signIn).toHaveBeenCalled();
  });

  it('renders the empty-state when authenticated and no saved scenarios', () => {
    setUp(true);
    store.savedScenarios.set([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('interactiveCity.saved.empty');
  });

  it('renders one item per saved scenario', () => {
    setUp(true);
    store.savedScenarios.set([SCENARIO_A]);
    fixture.detectChanges();
    const items = fixture.nativeElement.querySelectorAll('.cce-saved-drawer__item');
    expect(items.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('My Scenario');
    expect(fixture.nativeElement.textContent).toContain('2035');
  });

  it('load() short-circuits to loadFromSaved when not dirty (no confirm dialog)', async () => {
    setUp(true);
    store.savedScenarios.set([SCENARIO_A]);
    const spy = jest.spyOn(store, 'loadFromSaved');
    await fixture.componentInstance.load(SCENARIO_A);
    expect(spy).toHaveBeenCalledWith(SCENARIO_A);
    expect(dialog.open).not.toHaveBeenCalled();
  });

  it('load() opens confirm dialog when state is dirty; on confirm calls loadFromSaved', async () => {
    setUp(true);
    store.savedScenarios.set([SCENARIO_A]);
    // Make the store dirty by toggling something.
    store._seedCatalog([{
      id: 't1', nameAr: '', nameEn: '', descriptionAr: '', descriptionEn: '',
      categoryAr: '', categoryEn: '', carbonImpactKgPerYear: 0, costUsd: 0, iconUrl: null,
    }]);
    store.toggle('t1');
    expect(store.dirty()).toBe(true);
    dialog.open.mockReturnValue({ afterClosed: () => ({ subscribe: (cb: (v: boolean) => void) => cb(true), then: () => Promise.resolve(true) }) });
    // Use a Promise-shaped afterClosed for firstValueFrom.
    dialog.open.mockReturnValue({
      afterClosed: () => ({
        subscribe(observer: { next: (v: boolean) => void; complete: () => void }) {
          observer.next(true);
          observer.complete();
          return { unsubscribe: () => undefined };
        },
      }),
    });
    const spy = jest.spyOn(store, 'loadFromSaved');
    await fixture.componentInstance.load(SCENARIO_A);
    expect(dialog.open).toHaveBeenCalled();
    expect(spy).toHaveBeenCalledWith(SCENARIO_A);
  });

  it('remove() opens confirm dialog; on confirm calls store.delete and toasts success', async () => {
    setUp(true);
    store.savedScenarios.set([SCENARIO_A]);
    dialog.open.mockReturnValue({
      afterClosed: () => ({
        subscribe(observer: { next: (v: boolean) => void; complete: () => void }) {
          observer.next(true);
          observer.complete();
          return { unsubscribe: () => undefined };
        },
      }),
    });
    await fixture.componentInstance.remove(SCENARIO_A);
    expect(dialog.open).toHaveBeenCalled();
    expect(api.deleteMyScenario).toHaveBeenCalledWith('s1');
    expect(toast.success).toHaveBeenCalledWith('interactiveCity.toasts.deleteOk');
  });

  it('remove() does nothing when user cancels the confirm dialog', async () => {
    setUp(true);
    store.savedScenarios.set([SCENARIO_A]);
    dialog.open.mockReturnValue({
      afterClosed: () => ({
        subscribe(observer: { next: (v: boolean) => void; complete: () => void }) {
          observer.next(false);
          observer.complete();
          return { unsubscribe: () => undefined };
        },
      }),
    });
    await fixture.componentInstance.remove(SCENARIO_A);
    expect(api.deleteMyScenario).not.toHaveBeenCalled();
  });
});

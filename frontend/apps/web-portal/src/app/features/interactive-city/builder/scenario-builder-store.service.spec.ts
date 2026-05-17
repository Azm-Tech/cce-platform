import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { AuthService } from '../../../core/auth/auth.service';
import { InteractiveCityApiService, type Result } from '../interactive-city-api.service';
import {
  buildConfigurationJson,
  type CityTechnology,
  type RunResult,
  type SavedScenario,
} from '../interactive-city.types';
import { ScenarioBuilderStore } from './scenario-builder-store.service';

const VALID_GUID_A = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const VALID_GUID_B = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const VALID_GUID_C = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

const TECH_A: CityTechnology = {
  id: VALID_GUID_A,
  nameAr: 'أ', nameEn: 'A',
  descriptionAr: 'وصف أ', descriptionEn: 'desc a',
  categoryAr: 'فئة', categoryEn: 'cat',
  carbonImpactKgPerYear: -1000, costUsd: 5000,
  iconUrl: null,
};
const TECH_B: CityTechnology = {
  ...TECH_A,
  id: VALID_GUID_B, nameEn: 'B',
  carbonImpactKgPerYear: -500, costUsd: 2000,
};
const TECH_C: CityTechnology = {
  ...TECH_A,
  id: VALID_GUID_C, nameEn: 'C',
  carbonImpactKgPerYear: 100, costUsd: 1500,
};

const SAMPLE_SAVED: SavedScenario = {
  id: 'scenario-1',
  nameAr: 'سيناريو', nameEn: 'Scenario',
  cityType: 'Industrial',
  targetYear: 2035,
  configurationJson: buildConfigurationJson([VALID_GUID_A, VALID_GUID_B]),
  createdOn: '2026-05-02T12:00:00Z',
};

function ok<T>(value: T): Result<T> { return { ok: true, value }; }
function fail<E extends { kind: string }>(error: E): Result<never> { return { ok: false, error: error as never }; }

describe('ScenarioBuilderStore', () => {
  let store: ScenarioBuilderStore;
  let api: jest.Mocked<InteractiveCityApiService>;
  let authIsAuthenticated: ReturnType<typeof signal<boolean>>;

  function createStore(authenticated: boolean): void {
    TestBed.resetTestingModule();
    authIsAuthenticated = signal(authenticated);
    api = {
      listTechnologies: jest.fn().mockResolvedValue(ok([TECH_A, TECH_B, TECH_C])),
      runScenario: jest.fn(),
      listMyScenarios: jest.fn().mockResolvedValue(ok([SAMPLE_SAVED])),
      saveScenario: jest.fn(),
      deleteMyScenario: jest.fn().mockResolvedValue(ok(undefined)),
    } as unknown as jest.Mocked<InteractiveCityApiService>;

    TestBed.configureTestingModule({
      providers: [
        ScenarioBuilderStore,
        { provide: InteractiveCityApiService, useValue: api },
        { provide: AuthService, useValue: { isAuthenticated: authIsAuthenticated.asReadonly() } },
      ],
    });
    store = TestBed.inject(ScenarioBuilderStore);
  }

  beforeEach(() => createStore(false));

  // ─── Computed signals ───
  it('liveTotals sums carbon + cost over selected', () => {
    store['_seedCatalog']([TECH_A, TECH_B, TECH_C]);
    store.toggle(VALID_GUID_A);
    store.toggle(VALID_GUID_B);
    expect(store.liveTotals()).toEqual({
      totalCarbonImpactKgPerYear: -1500,
      totalCostUsd: 7000,
    });
  });

  it('liveTotals returns zero on empty selection', () => {
    expect(store.liveTotals()).toEqual({ totalCarbonImpactKgPerYear: 0, totalCostUsd: 0 });
  });

  it('liveTotals ignores selected ids absent from the catalog', () => {
    store['_seedCatalog']([TECH_A]);
    store.toggle(VALID_GUID_A);
    store.toggle(VALID_GUID_B); // not in catalog
    expect(store.liveTotals()).toEqual({
      totalCarbonImpactKgPerYear: -1000,
      totalCostUsd: 5000,
    });
  });

  it('selectedTechnologies returns rows in catalog order', () => {
    store['_seedCatalog']([TECH_A, TECH_B, TECH_C]);
    // toggle in reverse order
    store.toggle(VALID_GUID_C);
    store.toggle(VALID_GUID_A);
    expect(store.selectedTechnologies().map((t) => t.id)).toEqual([VALID_GUID_A, VALID_GUID_C]);
  });

  it('canRun reflects size > 0 AND !running', () => {
    store['_seedCatalog']([TECH_A]);
    expect(store.canRun()).toBe(false);
    store.toggle(VALID_GUID_A);
    expect(store.canRun()).toBe(true);
    store['_setRunning'](true);
    expect(store.canRun()).toBe(false);
  });

  it('canSave reflects size + non-empty name + !saving', () => {
    store['_seedCatalog']([TECH_A]);
    store.toggle(VALID_GUID_A);
    expect(store.canSave()).toBe(false); // name empty
    store.setName('My Scenario');
    expect(store.canSave()).toBe(true);
    store['_setSaving'](true);
    expect(store.canSave()).toBe(false);
  });

  it('dirty: false initially, true after toggle, reset by loadFromSaved', () => {
    expect(store.dirty()).toBe(false);
    store['_seedCatalog']([TECH_A]);
    store.toggle(VALID_GUID_A);
    expect(store.dirty()).toBe(true);
    store.loadFromSaved(SAMPLE_SAVED);
    expect(store.dirty()).toBe(false);
  });

  it('dirty stays false during markHydrating(true)', () => {
    store.markHydrating(true);
    store['_seedCatalog']([TECH_A]);
    store.toggle(VALID_GUID_A);
    expect(store.dirty()).toBe(false);
    store.markHydrating(false);
    // After hydration window closes, the toggle is still part of the baseline
    expect(store.dirty()).toBe(false);
  });

  // ─── init() ───
  it('init() loads catalog when not authenticated', async () => {
    await store.init();
    expect(api.listTechnologies).toHaveBeenCalled();
    expect(api.listMyScenarios).not.toHaveBeenCalled();
    expect(store.technologies()).toEqual([TECH_A, TECH_B, TECH_C]);
    expect(store.catalogLoading()).toBe(false);
    expect(store.catalogError()).toBeNull();
  });

  it('init() loads saved scenarios when authenticated', async () => {
    createStore(true);
    await store.init();
    expect(api.listMyScenarios).toHaveBeenCalled();
    expect(store.savedScenarios()).toEqual([SAMPLE_SAVED]);
  });

  it('init() sets catalogError on listTechnologies failure', async () => {
    api.listTechnologies.mockResolvedValue(fail({ kind: 'network' }));
    await store.init();
    expect(store.catalogError()).toBe('network');
  });

  // ─── Actions ───
  it('toggle adds, then removes', () => {
    store.toggle(VALID_GUID_A);
    expect(store.selectedIds().has(VALID_GUID_A)).toBe(true);
    store.toggle(VALID_GUID_A);
    expect(store.selectedIds().has(VALID_GUID_A)).toBe(false);
  });

  it('clear() empties selectedIds', () => {
    store.toggle(VALID_GUID_A);
    store.toggle(VALID_GUID_B);
    store.clear();
    expect(store.selectedIds().size).toBe(0);
  });

  it('setName / setCityType / setTargetYear update signals', () => {
    store.setName('Hello');
    store.setCityType('Coastal');
    store.setTargetYear(2040);
    expect(store.name()).toBe('Hello');
    expect(store.cityType()).toBe('Coastal');
    expect(store.targetYear()).toBe(2040);
  });

  it('loadFromSaved hydrates state from configurationJson', () => {
    store['_seedCatalog']([TECH_A, TECH_B]);
    store.loadFromSaved(SAMPLE_SAVED);
    expect(store.name()).toBe('Scenario');
    expect(store.cityType()).toBe('Industrial');
    expect(store.targetYear()).toBe(2035);
    expect(Array.from(store.selectedIds()).sort()).toEqual([VALID_GUID_A, VALID_GUID_B].sort());
    expect(store.serverResult()).toBeNull();
  });

  // ─── run() ───
  it('run() short-circuits to zero totals when selection is empty', async () => {
    const res = await store.run();
    expect(res.ok).toBe(true);
    if (res.ok) {
      expect(res.value.totalCarbonImpactKgPerYear).toBe(0);
      expect(res.value.totalCostUsd).toBe(0);
    }
    expect(api.runScenario).not.toHaveBeenCalled();
  });

  it('run() posts RunRequest from current state and stores serverResult on success', async () => {
    store['_seedCatalog']([TECH_A]);
    store.toggle(VALID_GUID_A);
    store.setCityType('Industrial');
    store.setTargetYear(2040);
    const result: RunResult = {
      totalCarbonImpactKgPerYear: -1000,
      totalCostUsd: 5000,
      summaryAr: 'ملخص', summaryEn: 'summary',
    };
    api.runScenario.mockResolvedValue(ok(result));
    const res = await store.run();
    expect(api.runScenario).toHaveBeenCalledWith({
      cityType: 'Industrial',
      targetYear: 2040,
      configurationJson: buildConfigurationJson([VALID_GUID_A]),
    });
    expect(res.ok).toBe(true);
    expect(store.serverResult()).toEqual(result);
  });

  it('run() leaves serverResult null on failure', async () => {
    store['_seedCatalog']([TECH_A]);
    store.toggle(VALID_GUID_A);
    api.runScenario.mockResolvedValue(fail({ kind: 'server' }));
    await store.run();
    expect(store.serverResult()).toBeNull();
  });

  // ─── save() ───
  it('save() posts both nameAr and nameEn from name(), prepends to savedScenarios', async () => {
    store['_seedCatalog']([TECH_A]);
    store.toggle(VALID_GUID_A);
    store.setName('My Scenario');
    api.saveScenario.mockResolvedValue(ok({ ...SAMPLE_SAVED, id: 'new-1' }));
    await store.save();
    expect(api.saveScenario).toHaveBeenCalledWith(expect.objectContaining({
      nameAr: 'My Scenario',
      nameEn: 'My Scenario',
    }));
    expect(store.savedScenarios()[0].id).toBe('new-1');
  });

  // ─── delete() ───
  it('delete() removes the row from savedScenarios', async () => {
    createStore(true);
    await store.init();
    expect(store.savedScenarios()).toHaveLength(1);
    await store.delete(SAMPLE_SAVED.id);
    expect(store.savedScenarios()).toHaveLength(0);
  });

  // ─── URL state translation ───
  it('toUrlState reflects current editable signals', () => {
    store.setCityType('Coastal');
    store.setTargetYear(2040);
    store.setName('Test');
    store.toggle(VALID_GUID_A);
    expect(store.toUrlState()).toEqual({
      cityType: 'Coastal',
      targetYear: 2040,
      name: 'Test',
      selectedIds: [VALID_GUID_A],
    });
  });

  it('applyUrlState writes editable signals; with markHydrating(true) dirty stays false', () => {
    store.markHydrating(true);
    store.applyUrlState({
      cityType: 'Coastal',
      targetYear: 2040,
      name: 'Test',
      selectedIds: [VALID_GUID_A],
    });
    store.markHydrating(false);
    expect(store.cityType()).toBe('Coastal');
    expect(store.dirty()).toBe(false);
  });
});

import { Injectable, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../../core/auth/auth.service';
import { InteractiveCityApiService, type Result } from '../interactive-city-api.service';
import {
  DEFAULT_CITY_TYPE,
  DEFAULT_TARGET_YEAR,
  buildConfigurationJson,
  parseConfigurationJson,
  type CityType,
  type CityTechnology,
  type RunResult,
  type SavedScenario,
} from '../interactive-city.types';
import type { UrlState } from '../lib/url-state';

interface DirtyBaseline {
  cityType: CityType;
  targetYear: number;
  name: string;
  selectedIds: ReadonlySet<string>;
}

/**
 * Signals-first state container for the scenario builder. Owns the
 * catalog cache, the editable scenario, the saved-scenarios list and
 * the network flags. Sub-components consume signals; the page calls
 * actions. The page also drives URL hydrate/sync via applyUrlState
 * and toUrlState.
 */
@Injectable()
export class ScenarioBuilderStore {
  private readonly api = inject(InteractiveCityApiService);
  private readonly auth = inject(AuthService);

  // ─── Catalog ───
  readonly technologies = signal<CityTechnology[]>([]);
  readonly catalogLoading = signal<boolean>(false);
  readonly catalogError = signal<string | null>(null);

  // ─── Editable scenario ───
  readonly cityType = signal<CityType>(DEFAULT_CITY_TYPE);
  readonly targetYear = signal<number>(DEFAULT_TARGET_YEAR);
  readonly name = signal<string>('');
  readonly selectedIds = signal<ReadonlySet<string>>(new Set());

  // ─── Server result (cleared by edits, populated by run()) ───
  readonly serverResult = signal<RunResult | null>(null);

  // ─── Saved scenarios ───
  readonly savedScenarios = signal<SavedScenario[]>([]);
  readonly savedLoading = signal<boolean>(false);
  readonly savedError = signal<string | null>(null);

  // ─── Network state ───
  readonly running = signal<boolean>(false);
  readonly saving = signal<boolean>(false);

  // ─── Internal: dirty baseline + hydration flag ───
  private readonly hydrating = signal<boolean>(false);
  private readonly baseline = signal<DirtyBaseline>({
    cityType: DEFAULT_CITY_TYPE,
    targetYear: DEFAULT_TARGET_YEAR,
    name: '',
    selectedIds: new Set(),
  });

  // ─── Computed signals ───
  readonly liveTotals = computed(() => {
    const techMap = new Map(this.technologies().map((t) => [t.id, t]));
    let carbon = 0;
    let cost = 0;
    for (const id of this.selectedIds()) {
      const t = techMap.get(id);
      if (!t) continue;
      carbon += t.carbonImpactKgPerYear;
      cost += t.costUsd;
    }
    return { totalCarbonImpactKgPerYear: carbon, totalCostUsd: cost };
  });

  readonly selectedTechnologies = computed(() => {
    const ids = this.selectedIds();
    return this.technologies().filter((t) => ids.has(t.id));
  });

  readonly canRun = computed(() => this.selectedIds().size > 0 && !this.running());

  readonly canSave = computed(
    () => this.selectedIds().size > 0 && this.name().trim() !== '' && !this.saving(),
  );

  readonly dirty = computed(() => {
    if (this.hydrating()) return false;
    const b = this.baseline();
    if (b.cityType !== this.cityType()) return true;
    if (b.targetYear !== this.targetYear()) return true;
    if (b.name !== this.name()) return true;
    const sel = this.selectedIds();
    if (b.selectedIds.size !== sel.size) return true;
    for (const id of sel) {
      if (!b.selectedIds.has(id)) return true;
    }
    return false;
  });

  // ─── Actions ───

  /** Loads the catalog and (when authenticated) the saved-scenarios list. */
  async init(): Promise<void> {
    this.catalogLoading.set(true);
    this.catalogError.set(null);
    const techRes = await this.api.listTechnologies();
    this.catalogLoading.set(false);
    if (techRes.ok) this.technologies.set(techRes.value);
    else this.catalogError.set(techRes.error.kind);

    if (this.auth.isAuthenticated()) {
      this.savedLoading.set(true);
      this.savedError.set(null);
      const savedRes = await this.api.listMyScenarios();
      this.savedLoading.set(false);
      if (savedRes.ok) this.savedScenarios.set(savedRes.value);
      else this.savedError.set(savedRes.error.kind);
    }
  }

  setCityType(c: CityType): void {
    this.cityType.set(c);
    this.serverResult.set(null);
  }

  setTargetYear(y: number): void {
    this.targetYear.set(y);
    this.serverResult.set(null);
  }

  setName(n: string): void {
    this.name.set(n);
  }

  toggle(id: string): void {
    this.selectedIds.update((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
    this.serverResult.set(null);
  }

  clear(): void {
    this.selectedIds.set(new Set());
    this.serverResult.set(null);
  }

  loadFromSaved(scenario: SavedScenario): void {
    this.cityType.set(scenario.cityType);
    this.targetYear.set(scenario.targetYear);
    // Sub-8 v0.1.0 uses a single name input; prefer EN as the canonical.
    this.name.set(scenario.nameEn || scenario.nameAr);
    this.selectedIds.set(new Set(parseConfigurationJson(scenario.configurationJson)));
    this.serverResult.set(null);
    this.resetBaseline();
  }

  async run(): Promise<Result<RunResult>> {
    if (this.selectedIds().size === 0) {
      return {
        ok: true,
        value: {
          totalCarbonImpactKgPerYear: 0,
          totalCostUsd: 0,
          summaryAr: '',
          summaryEn: '',
        },
      };
    }
    this.running.set(true);
    this.serverResult.set(null);
    const res = await this.api.runScenario({
      cityType: this.cityType(),
      targetYear: this.targetYear(),
      configurationJson: buildConfigurationJson(this.selectedIds()),
    });
    this.running.set(false);
    if (res.ok) this.serverResult.set(res.value);
    return res;
  }

  async save(): Promise<Result<SavedScenario>> {
    this.saving.set(true);
    const name = this.name();
    const res = await this.api.saveScenario({
      nameAr: name,
      nameEn: name,
      cityType: this.cityType(),
      targetYear: this.targetYear(),
      configurationJson: buildConfigurationJson(this.selectedIds()),
    });
    this.saving.set(false);
    if (res.ok) {
      this.savedScenarios.update((prev) => [res.value, ...prev]);
      this.resetBaseline();
    }
    return res;
  }

  async delete(id: string): Promise<Result<void>> {
    const res = await this.api.deleteMyScenario(id);
    if (res.ok) {
      this.savedScenarios.update((prev) => prev.filter((s) => s.id !== id));
    }
    return res;
  }

  // ─── URL state translation ───

  toUrlState(): UrlState {
    return {
      cityType: this.cityType(),
      targetYear: this.targetYear(),
      name: this.name(),
      selectedIds: Array.from(this.selectedIds()),
    };
  }

  applyUrlState(s: UrlState): void {
    this.cityType.set(s.cityType);
    this.targetYear.set(s.targetYear);
    this.name.set(s.name);
    this.selectedIds.set(new Set(s.selectedIds));
    if (this.hydrating()) this.resetBaseline();
  }

  /** Toggle the hydration window. While true, edits do NOT mark the
   *  state dirty; on the false transition, the current state becomes
   *  the new baseline. */
  markHydrating(value: boolean): void {
    this.hydrating.set(value);
    if (!value) this.resetBaseline();
  }

  // ─── Internal helpers ───

  private resetBaseline(): void {
    this.baseline.set({
      cityType: this.cityType(),
      targetYear: this.targetYear(),
      name: this.name(),
      selectedIds: new Set(this.selectedIds()),
    });
  }

  // ─── Test-only helpers (underscore prefix indicates "don't use from prod code") ───

  /** @internal Test-only — seed the catalog without a network call. */
  _seedCatalog(rows: CityTechnology[]): void {
    this.technologies.set(rows);
  }

  /** @internal Test-only — set running state without going through run(). */
  _setRunning(value: boolean): void {
    this.running.set(value);
  }

  /** @internal Test-only — set saving state without going through save(). */
  _setSaving(value: boolean): void {
    this.saving.set(value);
  }
}

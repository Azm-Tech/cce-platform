import { Injectable, signal } from '@angular/core';
import {
  DEFAULT_CITY_TYPE,
  DEFAULT_TARGET_YEAR,
  type CityType,
  type CityTechnology,
  type RunResult,
  type SavedScenario,
} from '../interactive-city.types';

/**
 * Signals-first state container for the scenario builder. Phase 01 will
 * fill in init / actions / computed signals. This stub exists so the rest
 * of Phase 00 can import it without a circular reference.
 */
@Injectable()
export class ScenarioBuilderStore {
  // Catalog
  readonly technologies = signal<CityTechnology[]>([]);
  readonly catalogLoading = signal<boolean>(false);
  readonly catalogError = signal<string | null>(null);

  // Editable scenario
  readonly cityType = signal<CityType>(DEFAULT_CITY_TYPE);
  readonly targetYear = signal<number>(DEFAULT_TARGET_YEAR);
  readonly name = signal<string>('');
  readonly selectedIds = signal<ReadonlySet<string>>(new Set());

  // Server result
  readonly serverResult = signal<RunResult | null>(null);

  // Saved scenarios
  readonly savedScenarios = signal<SavedScenario[]>([]);
  readonly savedLoading = signal<boolean>(false);
  readonly savedError = signal<string | null>(null);

  // Network state
  readonly running = signal<boolean>(false);
  readonly saving = signal<boolean>(false);
}

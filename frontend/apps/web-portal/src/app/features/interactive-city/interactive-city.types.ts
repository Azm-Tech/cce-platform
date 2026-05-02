/**
 * Mirrors backend DTOs from CCE.Application.InteractiveCity.Public.Dtos.
 * Sub-6 Phase 9 only consumed the technologies-list endpoint; Sub-8 adds
 * RunRequest / RunResult / SaveRequest / SavedScenario.
 */

// ─── Technology catalog (unchanged from Sub-6 Phase 9.1) ───
export interface CityTechnology {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  categoryAr: string;
  categoryEn: string;
  carbonImpactKgPerYear: number;
  costUsd: number;
  iconUrl: string | null;
}

// ─── Scenario shapes (NEW) ───
export type CityType = 'Coastal' | 'Industrial' | 'Mixed' | 'Residential';
export const CITY_TYPES: readonly CityType[] = ['Coastal', 'Industrial', 'Mixed', 'Residential'] as const;

export const DEFAULT_CITY_TYPE: CityType = 'Mixed';
export const DEFAULT_TARGET_YEAR = 2030;

/** Inclusive bounds for the targetYear input. The lower bound is computed
 *  at runtime from the system clock so the test file can clock-mock; we
 *  expose the computation rather than a static value. */
export function targetYearBounds(now: Date = new Date()): { min: number; max: number } {
  const y = now.getFullYear();
  return { min: y, max: y + 50 };
}

/** Wire-format request body for POST /api/interactive-city/scenarios/run. */
export interface RunRequest {
  cityType: CityType;
  targetYear: number;
  /** JSON-encoded payload: '{"technologyIds":["guid1","guid2"]}' */
  configurationJson: string;
}

/** Server response from /scenarios/run. Same numbers the client computes
 *  via `liveTotals`, plus a localized summary string. */
export interface RunResult {
  totalCarbonImpactKgPerYear: number;
  totalCostUsd: number;
  summaryAr: string;
  summaryEn: string;
}

/** Body for POST /api/me/interactive-city/scenarios. Auth required. */
export interface SaveRequest {
  nameAr: string;
  nameEn: string;
  cityType: CityType;
  targetYear: number;
  configurationJson: string;
}

/** Item shape for GET /api/me/interactive-city/scenarios and the create
 *  response from POST /scenarios. */
export interface SavedScenario {
  id: string;
  nameAr: string;
  nameEn: string;
  cityType: CityType;
  targetYear: number;
  configurationJson: string;
  /** ISO 8601 timestamp from server. */
  createdOn: string;
}

/** Helper to build the configurationJson field consistently. */
export function buildConfigurationJson(technologyIds: Iterable<string>): string {
  return JSON.stringify({ technologyIds: Array.from(technologyIds) });
}

/** Inverse of buildConfigurationJson. Returns [] on any parse failure
 *  rather than throwing — callers treat malformed configs as empty. */
export function parseConfigurationJson(json: string): string[] {
  try {
    const parsed: unknown = JSON.parse(json);
    if (parsed && typeof parsed === 'object' && 'technologyIds' in parsed) {
      const ids = (parsed as { technologyIds: unknown }).technologyIds;
      if (Array.isArray(ids) && ids.every((x) => typeof x === 'string')) {
        return ids as string[];
      }
    }
  } catch {
    // fall through
  }
  return [];
}

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

// ─── Environmental factors (BRD §4.1.5 / F009) ───
//
// F009 requires the user to input governorate-level environmental factor
// values (public-transport usage, transport distance, renewable energy,
// recycling, green-space, industrial-emissions intensity). The current
// city performance is then computed from those values and the system
// recommends the technologies needed to reach carbon neutrality within
// the user's targetYear.
//
// All factors are bounded percentages (0-100) except `avgTransportKmPerDay`,
// which is bounded km (0-200). Defaults represent a typical pre-decarbonization
// urban governorate baseline.

export interface EnvironmentalFactors {
  /** % of trips taken via public transport. Higher = lower emissions. */
  publicTransportPct: number;
  /** Average daily km per resident. Higher = higher emissions. */
  avgTransportKmPerDay: number;
  /** % of grid sourced from renewable energy. Higher = lower emissions. */
  renewableEnergyPct: number;
  /** % of municipal waste recycled. Higher = lower emissions. */
  wasteRecyclingPct: number;
  /** % of city area covered by green space. Higher = lower emissions. */
  greenSpacePct: number;
  /** Industrial-emissions intensity index 0-100. Higher = more emissions. */
  industrialIntensity: number;
}

export const DEFAULT_ENV_FACTORS: EnvironmentalFactors = {
  publicTransportPct: 30,
  avgTransportKmPerDay: 35,
  renewableEnergyPct: 15,
  wasteRecyclingPct: 20,
  greenSpacePct: 25,
  industrialIntensity: 60,
};

/** Inclusive bounds for each factor. */
export const ENV_FACTOR_BOUNDS = {
  publicTransportPct: { min: 0, max: 100, step: 1, unit: 'percent' as const },
  avgTransportKmPerDay: { min: 0, max: 200, step: 1, unit: 'km' as const },
  renewableEnergyPct: { min: 0, max: 100, step: 1, unit: 'percent' as const },
  wasteRecyclingPct: { min: 0, max: 100, step: 1, unit: 'percent' as const },
  greenSpacePct: { min: 0, max: 100, step: 1, unit: 'percent' as const },
  industrialIntensity: { min: 0, max: 100, step: 1, unit: 'index' as const },
} as const;

/** Per-city-type emissions baseline (kg CO2e / year for an average
 *  representative city block). Calibrated so that 1-3 well-chosen catalog
 *  technologies (typical impact -30k…-80k) can meaningfully reduce the
 *  net to near-neutral in the demonstration model. */
const CITY_BASELINES: Record<CityType, number> = {
  Residential: 50_000,
  Coastal: 70_000,
  Mixed: 90_000,
  Industrial: 130_000,
};

/**
 * Pure illustrative model that converts env factors + city type into a
 * baseline annual carbon footprint in kg CO2e. Each factor scales the
 * city baseline up or down within reasonable bounds. The formula is
 * deliberately transparent so the spec's "current city performance is
 * measured from the entered values" requirement is satisfied without
 * a backend round-trip.
 */
export function computeBaselineFootprintKgPerYear(
  factors: EnvironmentalFactors,
  cityType: CityType,
): number {
  const base = CITY_BASELINES[cityType];

  // Each scale factor moves the baseline within roughly ±50%. Defaults
  // (the values used by a "typical" city) yield ≈ baseline.
  const transport = 1 - factors.publicTransportPct / 250;        //  0% → 1.0,  100% → 0.6
  const distance = 0.7 + (factors.avgTransportKmPerDay / 200) * 0.6; // 0 → 0.7, 200 → 1.3
  const renewable = 1 - factors.renewableEnergyPct / 250;        //  0% → 1.0,  100% → 0.6
  const recycling = 1 - factors.wasteRecyclingPct / 500;         //  0% → 1.0,  100% → 0.8
  const greenSpace = 1 - factors.greenSpacePct / 500;            //  0% → 1.0,  100% → 0.8
  const industrial = 0.7 + factors.industrialIntensity / 250;    //  0  → 0.7,  100 → 1.1

  return Math.round(
    base * transport * distance * renewable * recycling * greenSpace * industrial,
  );
}

/**
 * F009 recommender. Given the current baseline footprint, the catalog
 * and the techs the user has already selected, return a set of additional
 * technology ids needed to close the gap to carbon neutrality.
 *
 * Greedy strategy:
 *   1. Pool = catalog techs with `carbonImpactKgPerYear < 0` AND not
 *      already selected.
 *   2. Sort by best carbon-reduction-per-USD (ties → larger absolute
 *      reduction first; cost==0 → treated as infinitely efficient).
 *   3. Pick one at a time until the running net is ≤ 0 or the pool
 *      is exhausted.
 */
export function recommendTechnologies(args: {
  catalog: readonly CityTechnology[];
  selectedIds: ReadonlySet<string>;
  baselineKgPerYear: number;
  currentTechSumKgPerYear: number;
}): string[] {
  const { catalog, selectedIds, baselineKgPerYear, currentTechSumKgPerYear } = args;
  let net = baselineKgPerYear + currentTechSumKgPerYear;
  if (net <= 0) return [];

  const candidates = catalog
    .filter((t) => !selectedIds.has(t.id) && t.carbonImpactKgPerYear < 0)
    .map((t) => ({
      tech: t,
      ratio: t.costUsd > 0
        ? -t.carbonImpactKgPerYear / t.costUsd
        : Number.POSITIVE_INFINITY,
    }))
    .sort((a, b) => {
      if (b.ratio !== a.ratio) return b.ratio - a.ratio;
      return a.tech.carbonImpactKgPerYear - b.tech.carbonImpactKgPerYear; // more-negative first
    });

  const picks: string[] = [];
  for (const c of candidates) {
    if (net <= 0) break;
    picks.push(c.tech.id);
    net += c.tech.carbonImpactKgPerYear;
  }
  return picks;
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

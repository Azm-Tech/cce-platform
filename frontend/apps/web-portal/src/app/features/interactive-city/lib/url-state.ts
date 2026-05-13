import type { ParamMap } from '@angular/router';
import {
  CITY_TYPES,
  DEFAULT_CITY_TYPE,
  DEFAULT_ENV_FACTORS,
  DEFAULT_TARGET_YEAR,
  ENV_FACTOR_BOUNDS,
  type CityType,
  type EnvironmentalFactors,
} from '../interactive-city.types';

/** A subset of the editable scenario state captured in the URL. */
export interface UrlState {
  cityType: CityType;
  targetYear: number;
  selectedIds: string[];
  name: string;
  /** F009: environmental factor inputs that drive the baseline. */
  envFactors: EnvironmentalFactors;
}

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Map env-factor URL keys ↔ factor field names. The short keys keep
 *  the URL compact when factors are non-default. */
const FACTOR_PARAM_KEYS: ReadonlyArray<{
  param: string;
  field: keyof EnvironmentalFactors;
}> = [
  { param: 'pt', field: 'publicTransportPct' },
  { param: 'tk', field: 'avgTransportKmPerDay' },
  { param: 're', field: 'renewableEnergyPct' },
  { param: 'rc', field: 'wasteRecyclingPct' },
  { param: 'gs', field: 'greenSpacePct' },
  { param: 'ii', field: 'industrialIntensity' },
];

function clampFactor<K extends keyof EnvironmentalFactors>(
  field: K, value: number,
): number {
  const { min, max } = ENV_FACTOR_BOUNDS[field];
  return Math.max(min, Math.min(max, Math.round(value)));
}

/** Build a `Params`-shaped patch from current state. Used inside an effect
 *  that calls `Router.navigate([], { queryParams, queryParamsHandling: 'merge', replaceUrl: true })`. */
export function buildUrlPatch(state: UrlState): Record<string, string | null> {
  const patch: Record<string, string | null> = {
    city: state.cityType === DEFAULT_CITY_TYPE ? null : state.cityType,
    year: state.targetYear === DEFAULT_TARGET_YEAR ? null : String(state.targetYear),
    t: state.selectedIds.length === 0 ? null : state.selectedIds.join(','),
    name: state.name.trim() === '' ? null : state.name,
  };

  // Env factors: only emit a key when the value differs from the default
  // so the URL stays clean for unmodified scenarios.
  for (const { param, field } of FACTOR_PARAM_KEYS) {
    const value = state.envFactors[field];
    patch[param] = value === DEFAULT_ENV_FACTORS[field] ? null : String(value);
  }
  return patch;
}

/** Parse a `ParamMap` into a partial `UrlState`. Drops unknown city types,
 *  clamps target year, drops non-GUID `t` entries, falls back to defaults. */
export function parseUrlState(params: ParamMap, now: Date = new Date()): UrlState {
  const cityRaw = params.get('city');
  const cityType: CityType =
    cityRaw && (CITY_TYPES as readonly string[]).includes(cityRaw)
      ? (cityRaw as CityType)
      : DEFAULT_CITY_TYPE;

  const yearRaw = params.get('year');
  const yearParsed = yearRaw ? Number.parseInt(yearRaw, 10) : NaN;
  const yearMin = now.getFullYear();
  const yearMax = yearMin + 50;
  const targetYear =
    Number.isFinite(yearParsed) && yearParsed >= yearMin && yearParsed <= yearMax
      ? yearParsed
      : DEFAULT_TARGET_YEAR;

  const tRaw = params.get('t');
  const selectedIds = tRaw
    ? tRaw.split(',').map((s) => s.trim()).filter((s) => GUID_RE.test(s))
    : [];

  const name = (params.get('name') ?? '').slice(0, 200); // safety cap

  // Env factors: pull each short key, clamp to bounds, fall back to default
  // when missing or unparseable.
  const envFactors: EnvironmentalFactors = { ...DEFAULT_ENV_FACTORS };
  for (const { param, field } of FACTOR_PARAM_KEYS) {
    const raw = params.get(param);
    if (raw === null) continue;
    const parsed = Number.parseInt(raw, 10);
    if (!Number.isFinite(parsed)) continue;
    envFactors[field] = clampFactor(field, parsed);
  }

  return { cityType, targetYear, selectedIds, name, envFactors };
}

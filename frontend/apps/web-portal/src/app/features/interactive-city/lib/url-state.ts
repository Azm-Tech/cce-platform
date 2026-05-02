import type { ParamMap } from '@angular/router';
import { CITY_TYPES, DEFAULT_CITY_TYPE, DEFAULT_TARGET_YEAR, type CityType } from '../interactive-city.types';

/** A subset of the editable scenario state captured in the URL. */
export interface UrlState {
  cityType: CityType;
  targetYear: number;
  selectedIds: string[];
  name: string;
}

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Build a `Params`-shaped patch from current state. Used inside an effect
 *  that calls `Router.navigate([], { queryParams, queryParamsHandling: 'merge', replaceUrl: true })`. */
export function buildUrlPatch(state: UrlState): Record<string, string | null> {
  return {
    city: state.cityType === DEFAULT_CITY_TYPE ? null : state.cityType,
    year: state.targetYear === DEFAULT_TARGET_YEAR ? null : String(state.targetYear),
    t: state.selectedIds.length === 0 ? null : state.selectedIds.join(','),
    name: state.name.trim() === '' ? null : state.name,
  };
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

  return { cityType, targetYear, selectedIds, name };
}

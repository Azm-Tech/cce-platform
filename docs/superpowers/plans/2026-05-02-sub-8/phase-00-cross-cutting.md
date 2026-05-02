# Phase 00 — Cross-cutting (Sub-8)

> Parent: [`../2026-05-02-sub-8.md`](../2026-05-02-sub-8.md) · Spec: [`../../specs/2026-05-02-sub-8-design.md`](../../specs/2026-05-02-sub-8-design.md) §3 (data contracts), §6 (URL state), §9 (i18n + RTL)

**Phase goal:** Lay the foundation for the scenario builder without surfacing any user-visible UI changes. Add the new TypeScript types, extend `InteractiveCityApiService` with the four new methods (`runScenario`, `listMyScenarios`, `saveScenario`, `deleteMyScenario`), build the `lib/url-state.ts` parse/build helpers, and add every i18n key Sub-8 will reference. Phase 01 starts wiring these into a real page.

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Sub-7 closed (`web-portal-v0.2.0` tag exists; main branch at the post-Sub-7 commit or later).
- web-portal Jest baseline: 73 suites · 362 tests passing; lint + build clean.

---

## Task 0.1: Extended types

**Files:**
- Modify: `frontend/apps/web-portal/src/app/features/interactive-city/interactive-city.types.ts`.

**Final state of the file** (replace existing contents):

```ts
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
```

- [ ] **Step 1: Write failing tests for the helpers**

Create `frontend/apps/web-portal/src/app/features/interactive-city/interactive-city.types.spec.ts`:

```ts
import {
  buildConfigurationJson,
  parseConfigurationJson,
  targetYearBounds,
  DEFAULT_CITY_TYPE,
  DEFAULT_TARGET_YEAR,
} from './interactive-city.types';

describe('interactive-city types helpers', () => {
  it('buildConfigurationJson serializes ids to a stable shape', () => {
    expect(buildConfigurationJson(['a', 'b'])).toBe('{"technologyIds":["a","b"]}');
    expect(buildConfigurationJson(new Set(['x']))).toBe('{"technologyIds":["x"]}');
    expect(buildConfigurationJson([])).toBe('{"technologyIds":[]}');
  });

  it('parseConfigurationJson is the inverse of buildConfigurationJson', () => {
    expect(parseConfigurationJson(buildConfigurationJson(['a', 'b']))).toEqual(['a', 'b']);
    expect(parseConfigurationJson('{"technologyIds":["x"]}')).toEqual(['x']);
  });

  it('parseConfigurationJson returns [] on malformed input', () => {
    expect(parseConfigurationJson('not json')).toEqual([]);
    expect(parseConfigurationJson('{}')).toEqual([]);
    expect(parseConfigurationJson('{"technologyIds":"oops"}')).toEqual([]);
    expect(parseConfigurationJson('{"technologyIds":[1,2,3]}')).toEqual([]);
  });

  it('targetYearBounds returns [year, year+50]', () => {
    const bounds = targetYearBounds(new Date('2030-06-01T00:00:00Z'));
    expect(bounds).toEqual({ min: 2030, max: 2080 });
  });

  it('exposes sensible defaults', () => {
    expect(DEFAULT_CITY_TYPE).toBe('Mixed');
    expect(DEFAULT_TARGET_YEAR).toBe(2030);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd frontend && bun nx test web-portal --watch=false --testPathPattern=interactive-city.types.spec
```

Expected: 5 failing tests with "module not found" or "function not defined".

- [ ] **Step 3: Replace `interactive-city.types.ts` with the final state shown above**

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd frontend && bun nx test web-portal --watch=false --testPathPattern=interactive-city.types.spec
```

Expected: 5 passing tests.

- [ ] **Step 5: Commit**

```bash
git add frontend/apps/web-portal/src/app/features/interactive-city/interactive-city.types.ts frontend/apps/web-portal/src/app/features/interactive-city/interactive-city.types.spec.ts
git -c commit.gpgsign=false commit -m "feat(interactive-city): extend types for scenario builder

Add CityType, RunRequest, RunResult, SaveRequest, SavedScenario, plus
helpers buildConfigurationJson / parseConfigurationJson / targetYearBounds
and DEFAULT_CITY_TYPE / DEFAULT_TARGET_YEAR constants. Sub-7 / Sub-8.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 0.2: Extended `InteractiveCityApiService`

**Files:**
- Modify: `frontend/apps/web-portal/src/app/features/interactive-city/interactive-city-api.service.ts`.
- Modify: `frontend/apps/web-portal/src/app/features/interactive-city/interactive-city-api.service.spec.ts`.

**Final state of `interactive-city-api.service.ts`:**

```ts
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  CityTechnology,
  RunRequest,
  RunResult,
  SaveRequest,
  SavedScenario,
} from './interactive-city.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class InteractiveCityApiService {
  private readonly http = inject(HttpClient);

  // ─── Anonymous-allowed ───
  async listTechnologies(): Promise<Result<CityTechnology[]>> {
    try {
      const value = await firstValueFrom(
        this.http.get<CityTechnology[]>('/api/interactive-city/technologies'),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  async runScenario(req: RunRequest): Promise<Result<RunResult>> {
    try {
      const value = await firstValueFrom(
        this.http.post<RunResult>('/api/interactive-city/scenarios/run', req),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  // ─── Authenticated ───
  async listMyScenarios(): Promise<Result<SavedScenario[]>> {
    try {
      const value = await firstValueFrom(
        this.http.get<SavedScenario[]>('/api/me/interactive-city/scenarios'),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  async saveScenario(req: SaveRequest): Promise<Result<SavedScenario>> {
    try {
      const value = await firstValueFrom(
        this.http.post<SavedScenario>('/api/me/interactive-city/scenarios', req),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  async deleteMyScenario(id: string): Promise<Result<void>> {
    try {
      await firstValueFrom(
        this.http.delete<void>(
          `/api/me/interactive-city/scenarios/${encodeURIComponent(id)}`,
        ),
      );
      return { ok: true, value: undefined };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
```

- [ ] **Step 1: Add failing tests for the four new methods**

Append to `interactive-city-api.service.spec.ts` (keep the existing two `listTechnologies` tests intact):

```ts
import type {
  RunRequest,
  RunResult,
  SaveRequest,
  SavedScenario,
} from './interactive-city.types';

describe('InteractiveCityApiService — scenario methods', () => {
  let sut: InteractiveCityApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(InteractiveCityApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('runScenario POSTs the request to /scenarios/run', async () => {
    const req: RunRequest = {
      cityType: 'Mixed',
      targetYear: 2030,
      configurationJson: '{"technologyIds":["t1"]}',
    };
    const result: RunResult = {
      totalCarbonImpactKgPerYear: -1500,
      totalCostUsd: 12000,
      summaryAr: 'ملخص',
      summaryEn: 'Summary',
    };
    const promise = sut.runScenario(req);
    const httpReq = http.expectOne('/api/interactive-city/scenarios/run');
    expect(httpReq.request.method).toBe('POST');
    expect(httpReq.request.body).toEqual(req);
    httpReq.flush(result);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toEqual(result);
  });

  it('listMyScenarios GETs /api/me/interactive-city/scenarios', async () => {
    const promise = sut.listMyScenarios();
    const req = http.expectOne('/api/me/interactive-city/scenarios');
    expect(req.request.method).toBe('GET');
    req.flush([]);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toEqual([]);
  });

  it('saveScenario POSTs to /api/me/interactive-city/scenarios and returns the created row', async () => {
    const body: SaveRequest = {
      nameAr: 'سيناريو',
      nameEn: 'Scenario',
      cityType: 'Industrial',
      targetYear: 2035,
      configurationJson: '{"technologyIds":["t1","t2"]}',
    };
    const created: SavedScenario = {
      id: 'scenario-1',
      ...body,
      createdOn: '2026-05-02T12:00:00Z',
    };
    const promise = sut.saveScenario(body);
    const req = http.expectOne('/api/me/interactive-city/scenarios');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush(created, { status: 201, statusText: 'Created' });
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value.id).toBe('scenario-1');
  });

  it('deleteMyScenario DELETEs the right URL and returns ok on 204', async () => {
    const promise = sut.deleteMyScenario('scenario-1');
    const req = http.expectOne('/api/me/interactive-city/scenarios/scenario-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('runScenario maps server errors to a FeatureError', async () => {
    const req: RunRequest = {
      cityType: 'Mixed',
      targetYear: 2030,
      configurationJson: '{"technologyIds":[]}',
    };
    const promise = sut.runScenario(req);
    http.expectOne('/api/interactive-city/scenarios/run').flush(
      'fail',
      { status: 500, statusText: 'Server Error' },
    );
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('serverError');
  });

  it('saveScenario maps 401 to an unauthorized FeatureError', async () => {
    const body: SaveRequest = {
      nameAr: 'سيناريو',
      nameEn: 'Scenario',
      cityType: 'Mixed',
      targetYear: 2030,
      configurationJson: '{"technologyIds":[]}',
    };
    const promise = sut.saveScenario(body);
    http.expectOne('/api/me/interactive-city/scenarios').flush(
      'unauthorized',
      { status: 401, statusText: 'Unauthorized' },
    );
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('unauthorized');
  });

  it('deleteMyScenario URL-encodes id', async () => {
    const promise = sut.deleteMyScenario('a/b');
    const req = http.expectOne('/api/me/interactive-city/scenarios/a%2Fb');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd frontend && bun nx test web-portal --watch=false --testPathPattern=interactive-city-api.service.spec
```

Expected: the 7 new tests fail because the methods don't exist yet; the 2 existing ones still pass.

- [ ] **Step 3: Replace the API service file with the final state shown above**

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd frontend && bun nx test web-portal --watch=false --testPathPattern=interactive-city-api.service.spec
```

Expected: 9 passing tests.

- [ ] **Step 5: Commit**

```bash
git add frontend/apps/web-portal/src/app/features/interactive-city/interactive-city-api.service.ts frontend/apps/web-portal/src/app/features/interactive-city/interactive-city-api.service.spec.ts
git -c commit.gpgsign=false commit -m "feat(interactive-city): add scenario CRUD methods to API service

runScenario (anonymous-OK), listMyScenarios + saveScenario +
deleteMyScenario (auth required). All return Result<T> via toFeatureError.
401 surfaces as { kind: 'unauthorized' } so the page can kick off sign-in.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 0.3: URL state helpers

**Files:**
- Create: `frontend/apps/web-portal/src/app/features/interactive-city/lib/url-state.ts`.
- Create: `frontend/apps/web-portal/src/app/features/interactive-city/lib/url-state.spec.ts`.

**Purpose:** Translate between `?city=&year=&t=&name=` query params and the store's editable scenario fields. Defensive on parse, deterministic on build, round-trips for valid input.

**Final state of `url-state.ts`:**

```ts
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
```

**Final state of `url-state.spec.ts`:**

```ts
import { convertToParamMap } from '@angular/router';
import { buildUrlPatch, parseUrlState } from './url-state';

const VALID_GUID_A = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const VALID_GUID_B = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const FIXED_NOW = new Date('2026-05-02T00:00:00Z');

describe('parseUrlState', () => {
  it('returns defaults for empty params', () => {
    const s = parseUrlState(convertToParamMap({}), FIXED_NOW);
    expect(s).toEqual({
      cityType: 'Mixed',
      targetYear: 2030,
      selectedIds: [],
      name: '',
    });
  });

  it('parses a fully populated URL', () => {
    const s = parseUrlState(
      convertToParamMap({
        city: 'Industrial',
        year: '2035',
        t: `${VALID_GUID_A},${VALID_GUID_B}`,
        name: 'My City',
      }),
      FIXED_NOW,
    );
    expect(s).toEqual({
      cityType: 'Industrial',
      targetYear: 2035,
      selectedIds: [VALID_GUID_A, VALID_GUID_B],
      name: 'My City',
    });
  });

  it('falls back to default city when value is unknown', () => {
    const s = parseUrlState(convertToParamMap({ city: 'NotACity' }), FIXED_NOW);
    expect(s.cityType).toBe('Mixed');
  });

  it('falls back to default year when value is non-numeric', () => {
    const s = parseUrlState(convertToParamMap({ year: 'soon' }), FIXED_NOW);
    expect(s.targetYear).toBe(2030);
  });

  it('falls back to default year when value is out of bounds', () => {
    const sLow = parseUrlState(convertToParamMap({ year: '1999' }), FIXED_NOW);
    const sHigh = parseUrlState(convertToParamMap({ year: '3000' }), FIXED_NOW);
    expect(sLow.targetYear).toBe(2030);
    expect(sHigh.targetYear).toBe(2030);
  });

  it('drops non-GUID t entries', () => {
    const s = parseUrlState(
      convertToParamMap({ t: `${VALID_GUID_A},not-a-guid,${VALID_GUID_B}` }),
      FIXED_NOW,
    );
    expect(s.selectedIds).toEqual([VALID_GUID_A, VALID_GUID_B]);
  });

  it('caps name length at 200 chars', () => {
    const long = 'x'.repeat(500);
    const s = parseUrlState(convertToParamMap({ name: long }), FIXED_NOW);
    expect(s.name.length).toBe(200);
  });
});

describe('buildUrlPatch', () => {
  it('emits null for default values so the URL stays clean', () => {
    const patch = buildUrlPatch({
      cityType: 'Mixed',
      targetYear: 2030,
      selectedIds: [],
      name: '',
    });
    expect(patch).toEqual({ city: null, year: null, t: null, name: null });
  });

  it('emits values for non-defaults', () => {
    const patch = buildUrlPatch({
      cityType: 'Industrial',
      targetYear: 2035,
      selectedIds: [VALID_GUID_A, VALID_GUID_B],
      name: 'My City',
    });
    expect(patch).toEqual({
      city: 'Industrial',
      year: '2035',
      t: `${VALID_GUID_A},${VALID_GUID_B}`,
      name: 'My City',
    });
  });
});

describe('parse / build round-trip', () => {
  it('survives a populated URL → state → URL', () => {
    const s1 = parseUrlState(
      convertToParamMap({
        city: 'Coastal',
        year: '2040',
        t: `${VALID_GUID_A}`,
        name: 'Test',
      }),
      FIXED_NOW,
    );
    const patch = buildUrlPatch(s1);
    const s2 = parseUrlState(
      convertToParamMap({
        city: patch.city ?? '',
        year: patch.year ?? '',
        t: patch.t ?? '',
        name: patch.name ?? '',
      }),
      FIXED_NOW,
    );
    expect(s2).toEqual(s1);
  });
});
```

- [ ] **Step 1: Create the spec file with the contents above**

- [ ] **Step 2: Run tests to verify they fail (module not found)**

```bash
cd frontend && bun nx test web-portal --watch=false --testPathPattern=url-state.spec
```

Expected: failing because `url-state.ts` doesn't exist yet.

- [ ] **Step 3: Create `url-state.ts` with the contents above**

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd frontend && bun nx test web-portal --watch=false --testPathPattern=url-state.spec
```

Expected: 10 passing tests.

- [ ] **Step 5: Commit**

```bash
git add frontend/apps/web-portal/src/app/features/interactive-city/lib/url-state.ts frontend/apps/web-portal/src/app/features/interactive-city/lib/url-state.spec.ts
git -c commit.gpgsign=false commit -m "feat(interactive-city): URL state parse/build helpers

Captures ?city=&year=&t=&name= round-trip. Defensive on parse — drops
unknown city, clamps year to [now, now+50], drops non-GUID t entries,
caps name at 200 chars. Defaults emit as null so the URL stays clean
when the user hasn't strayed from the defaults.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 0.4: i18n keys (EN + AR)

**Files:**
- Modify: `frontend/libs/i18n/src/lib/i18n/en.json`.
- Modify: `frontend/libs/i18n/src/lib/i18n/ar.json`.

**Purpose:** Every visible string Sub-8 will use lives in the i18n bundles before any component touches them. Component tests later in the plan can spy on `TranslateService.instant` against these keys.

**Add the following block to BOTH `en.json` and `ar.json` under the existing top-level `interactiveCity` key**, replacing whatever's currently there for that key (Sub-6 Phase 9 left a small stub that we extend wholesale):

**EN — `interactiveCity` section:**
```json
"interactiveCity": {
  "title": "Interactive city",
  "comingSoon": "",
  "empty": "No technologies catalogued yet.",
  "builder": {
    "title": "Build your scenario",
    "subtitle": "Pick technologies and see live carbon and cost totals.",
    "name": "Scenario name",
    "namePlaceholder": "Untitled scenario",
    "cityType": "City type",
    "targetYear": "Target year",
    "unsavedChangesTitle": "Discard current changes?",
    "unsavedChangesBody": "Loading another scenario will replace your current selection.",
    "unsavedChangesConfirm": "Discard and load",
    "unsavedChangesCancel": "Keep editing"
  },
  "cityType": {
    "Coastal": "Coastal",
    "Industrial": "Industrial",
    "Mixed": "Mixed",
    "Residential": "Residential"
  },
  "catalog": {
    "title": "Technology catalog",
    "search": "Search technologies…",
    "empty": "No technologies match your search.",
    "addLabel": "Add to scenario",
    "categoryAll": "All categories"
  },
  "selected": {
    "title": "Selected ({{count}})",
    "empty": "Pick technologies from the catalog to start.",
    "clear": "Clear all",
    "clearConfirmTitle": "Clear all selections?",
    "clearConfirmBody": "Removes all {{count}} technologies from this scenario.",
    "clearConfirm": "Clear all",
    "clearCancel": "Keep them",
    "remove": "Remove"
  },
  "totals": {
    "carbon": "Carbon impact",
    "carbonUnit": "kg CO₂ / year",
    "cost": "Cost",
    "costUnit": "USD",
    "run": "Run",
    "running": "Running…",
    "save": "Save",
    "saving": "Saving…",
    "summaryEmpty": "Run the scenario to see the server summary."
  },
  "saved": {
    "title": "Saved scenarios",
    "empty": "Save a scenario to see it here.",
    "load": "Load",
    "delete": "Delete",
    "confirmDeleteTitle": "Delete saved scenario?",
    "confirmDeleteBody": "\"{{name}}\" will be permanently removed.",
    "confirmDelete": "Delete",
    "confirmCancel": "Cancel",
    "signInToSaveTitle": "Sign in to save scenarios",
    "signInToSaveBody": "Save scenarios to your account so you can return to them later.",
    "signIn": "Sign in"
  },
  "errors": {
    "loadCatalog": "Couldn't load technologies.",
    "loadSaved": "Couldn't load your saved scenarios.",
    "runFailed": "Couldn't run the scenario.",
    "saveFailed": "Couldn't save the scenario.",
    "deleteFailed": "Couldn't delete the scenario.",
    "retry": "Retry"
  }
}
```

**AR — `interactiveCity` section** (same key shape, Arabic strings):
```json
"interactiveCity": {
  "title": "المدينة التفاعلية",
  "comingSoon": "",
  "empty": "لا توجد تقنيات بعد.",
  "builder": {
    "title": "ابنِ السيناريو",
    "subtitle": "اختر التقنيات وشاهد إجمالي الكربون والتكلفة مباشرة.",
    "name": "اسم السيناريو",
    "namePlaceholder": "سيناريو بدون عنوان",
    "cityType": "نوع المدينة",
    "targetYear": "السنة المستهدفة",
    "unsavedChangesTitle": "هل تريد تجاهل التغييرات الحالية؟",
    "unsavedChangesBody": "تحميل سيناريو آخر سيستبدل اختياراتك الحالية.",
    "unsavedChangesConfirm": "تجاهل وتحميل",
    "unsavedChangesCancel": "متابعة التحرير"
  },
  "cityType": {
    "Coastal": "ساحلية",
    "Industrial": "صناعية",
    "Mixed": "مختلطة",
    "Residential": "سكنية"
  },
  "catalog": {
    "title": "كتالوج التقنيات",
    "search": "ابحث في التقنيات…",
    "empty": "لا توجد تقنيات تطابق البحث.",
    "addLabel": "أضِف إلى السيناريو",
    "categoryAll": "كل الفئات"
  },
  "selected": {
    "title": "المختارة ({{count}})",
    "empty": "اختر تقنيات من الكتالوج للبدء.",
    "clear": "مسح الكل",
    "clearConfirmTitle": "مسح كل الاختيارات؟",
    "clearConfirmBody": "سيتم إزالة جميع التقنيات الـ {{count}} من هذا السيناريو.",
    "clearConfirm": "مسح الكل",
    "clearCancel": "إبقاء",
    "remove": "إزالة"
  },
  "totals": {
    "carbon": "تأثير الكربون",
    "carbonUnit": "كغ CO₂ / سنة",
    "cost": "التكلفة",
    "costUnit": "دولار",
    "run": "تشغيل",
    "running": "جارٍ التشغيل…",
    "save": "حفظ",
    "saving": "جارٍ الحفظ…",
    "summaryEmpty": "شغّل السيناريو لرؤية ملخص الخادم."
  },
  "saved": {
    "title": "السيناريوهات المحفوظة",
    "empty": "احفظ سيناريو ليظهر هنا.",
    "load": "تحميل",
    "delete": "حذف",
    "confirmDeleteTitle": "هل تريد حذف السيناريو المحفوظ؟",
    "confirmDeleteBody": "سيتم حذف \"{{name}}\" نهائياً.",
    "confirmDelete": "حذف",
    "confirmCancel": "إلغاء",
    "signInToSaveTitle": "سجّل الدخول لحفظ السيناريوهات",
    "signInToSaveBody": "احفظ السيناريوهات في حسابك للعودة إليها لاحقاً.",
    "signIn": "تسجيل الدخول"
  },
  "errors": {
    "loadCatalog": "تعذّر تحميل التقنيات.",
    "loadSaved": "تعذّر تحميل السيناريوهات المحفوظة.",
    "runFailed": "تعذّر تشغيل السيناريو.",
    "saveFailed": "تعذّر حفظ السيناريو.",
    "deleteFailed": "تعذّر حذف السيناريو.",
    "retry": "إعادة المحاولة"
  }
}
```

(`comingSoon` is intentionally left as `""` — the Sub-7 list page uses the same key for backward compat; the Sub-8 page won't render it, but admin-cms or other consumers might.)

- [ ] **Step 1: Edit `en.json` to replace the existing `"interactiveCity"` block with the EN block above**

Use the `Edit` tool with the existing block as `old_string`. Verify the rest of the file is untouched.

- [ ] **Step 2: Edit `ar.json` the same way**

- [ ] **Step 3: Verify both files are valid JSON**

```bash
cd /Users/m/CCE && python3 -c "import json; json.load(open('frontend/libs/i18n/src/lib/i18n/en.json')); json.load(open('frontend/libs/i18n/src/lib/i18n/ar.json')); print('ok')"
```

Expected: `ok`.

- [ ] **Step 4: Verify both files declare the same key shape** (catches typos in nested keys)

```bash
cd /Users/m/CCE && python3 -c "
import json
def keys(d, prefix=''):
    out = []
    for k, v in d.items():
        full = prefix + ('.' if prefix else '') + k
        if isinstance(v, dict):
            out.extend(keys(v, full))
        else:
            out.append(full)
    return sorted(out)
en = json.load(open('frontend/libs/i18n/src/lib/i18n/en.json'))
ar = json.load(open('frontend/libs/i18n/src/lib/i18n/ar.json'))
en_ic = keys(en['interactiveCity'])
ar_ic = keys(ar['interactiveCity'])
missing_in_ar = set(en_ic) - set(ar_ic)
missing_in_en = set(ar_ic) - set(en_ic)
assert not missing_in_ar, f'missing in AR: {missing_in_ar}'
assert not missing_in_en, f'missing in EN: {missing_in_en}'
print('parity ok')
"
```

Expected: `parity ok`.

- [ ] **Step 5: Run web-portal tests to make sure nothing else broke**

```bash
cd frontend && bun nx test web-portal --watch=false
```

Expected: all existing tests still pass; nothing references the new keys yet.

- [ ] **Step 6: Commit**

```bash
git add frontend/libs/i18n/src/lib/i18n/en.json frontend/libs/i18n/src/lib/i18n/ar.json
git -c commit.gpgsign=false commit -m "feat(interactive-city): add Sub-8 i18n keys (EN + AR)

interactiveCity.{builder, cityType, catalog, selected, totals, saved,
errors} with full RTL Arabic translations. Parity verified.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 0.5: Component file structure (placeholders)

**Files:**
- Create: `frontend/apps/web-portal/src/app/features/interactive-city/builder/scenario-builder-store.service.ts` — stub class.
- Create: `frontend/apps/web-portal/src/app/features/interactive-city/builder/scenario-header.component.ts` (+ `.html`, `.scss`) — empty standalone component.
- Create: same shape for `technology-catalog`, `selected-list`, `totals-bar`, `saved-scenarios-drawer` components.
- Create: `frontend/apps/web-portal/src/app/features/interactive-city/scenario-builder.page.ts` (+ `.html`, `.scss`) — empty standalone component (replaces the existing `interactive-city.page.*` files).
- Delete: `interactive-city.page.{ts,html,scss,spec.ts}` (Sub-6 Phase 9 stub).
- Modify: `frontend/apps/web-portal/src/app/features/interactive-city/routes.ts` to load `ScenarioBuilderPage`.

**Purpose:** Pre-create every file each later phase will fill in. Each placeholder is a syntactically valid Angular standalone component / service that compiles + lints clean. Phase 01+ replaces the stubs with real code. This step exists so that later phases can edit existing files (cleaner diffs, smaller commits) and so the import graph compiles end-to-end before any feature work lands.

**Stub `scenario-builder-store.service.ts`:**

```ts
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
```

**Stub component shape** (use this template for each of the 5 sub-components — `scenario-header`, `technology-catalog`, `selected-list`, `totals-bar`, `saved-scenarios-drawer`):

```ts
// scenario-header.component.ts (example — repeat for the other 4)
import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'cce-scenario-header',
  standalone: true,
  imports: [],
  templateUrl: './scenario-header.component.html',
  styleUrl: './scenario-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScenarioHeaderComponent {}
```

**Stub `.html`** (one line so prettier doesn't fight us): `<!-- TODO Phase 02 -->`

**Stub `.scss`**: `:host { display: block; }`

(For `saved-scenarios-drawer.component.ts` use selector `cce-saved-scenarios-drawer` and TODO Phase 04.)

**Stub `scenario-builder.page.ts`:**

```ts
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ScenarioBuilderStore } from './builder/scenario-builder-store.service';

/**
 * Top-level page for /interactive-city. Phase 01 fills in the layout and
 * URL hydration; Phase 02–04 wire the sub-components into the slots.
 */
@Component({
  selector: 'cce-scenario-builder-page',
  standalone: true,
  imports: [TranslateModule],
  providers: [ScenarioBuilderStore],
  templateUrl: './scenario-builder.page.html',
  styleUrl: './scenario-builder.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScenarioBuilderPage {}
```

**Stub `scenario-builder.page.html`:**

```html
<section class="cce-scenario-builder">
  <h1>{{ 'interactiveCity.builder.title' | translate }}</h1>
  <p class="cce-scenario-builder__subtitle">
    {{ 'interactiveCity.builder.subtitle' | translate }}
  </p>
  <!-- Phase 01: hydrate URL → store. Phase 02: header + catalog + selected. Phase 03: totals bar. Phase 04: drawer. -->
</section>
```

**Stub `scenario-builder.page.scss`:**

```scss
:host { display: block; padding: 1.5rem; max-width: 1280px; margin: 0 auto; }

.cce-scenario-builder__subtitle {
  margin: 0.5rem 0 1.5rem 0;
  color: rgba(0, 0, 0, 0.6);
}
```

**`routes.ts` final state:**

```ts
import { Routes } from '@angular/router';

export const INTERACTIVE_CITY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./scenario-builder.page').then((m) => m.ScenarioBuilderPage),
  },
];
```

- [ ] **Step 1: Create the store stub** — write `builder/scenario-builder-store.service.ts` with the contents above.

- [ ] **Step 2: Create each of the 5 sub-component stubs**

For each of `scenario-header`, `technology-catalog`, `selected-list`, `totals-bar` (each in `builder/`):
- Create `<name>.component.ts` using the template above (adjust selector + class name).
- Create `<name>.component.html` with `<!-- TODO Phase NN -->` (NN = 02 for the first three, 03 for totals-bar).
- Create `<name>.component.scss` with `:host { display: block; }`.

For `saved-scenarios-drawer.component.{ts,html,scss}` in `builder/`: same template, selector `cce-saved-scenarios-drawer`, TODO Phase 04.

- [ ] **Step 3: Create the page stub** — write `scenario-builder.page.{ts,html,scss}` with the contents above.

- [ ] **Step 4: Replace `routes.ts` with the final state above**

- [ ] **Step 5: Delete the Sub-6 Phase 9 stub files**

```bash
cd /Users/m/CCE && rm \
  frontend/apps/web-portal/src/app/features/interactive-city/interactive-city.page.ts \
  frontend/apps/web-portal/src/app/features/interactive-city/interactive-city.page.html \
  frontend/apps/web-portal/src/app/features/interactive-city/interactive-city.page.scss \
  frontend/apps/web-portal/src/app/features/interactive-city/interactive-city.page.spec.ts
```

- [ ] **Step 6: Run lint to make sure stubs compile + match style**

```bash
cd frontend && bun nx run web-portal:lint
```

Expected: no errors. (Empty components emit `unused parameter` warnings sometimes — if any new warnings appear that weren't in baseline, fix them inline; never silence with `eslint-disable`.)

- [ ] **Step 7: Run web-portal tests** to make sure nothing imports the deleted page

```bash
cd frontend && bun nx test web-portal --watch=false
```

Expected: all tests pass (we didn't reference the deleted page from anywhere; the route loader points at the new stub).

- [ ] **Step 8: Run a production build** to verify the full graph compiles

```bash
cd frontend && bun nx build web-portal
```

Expected: success; bundle size unchanged within the existing budget.

- [ ] **Step 9: Commit**

```bash
git add frontend/apps/web-portal/src/app/features/interactive-city/
git -c commit.gpgsign=false commit -m "feat(interactive-city): scaffold scenario-builder file structure

Empty stub components for ScenarioBuilderPage + 5 sub-components +
ScenarioBuilderStore. Routes.ts now loads the new page; the Sub-6 Phase 9
interactive-city.page.* files are removed. Phases 01–04 fill in the stubs.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 00 close-out

After Task 0.5 commits cleanly:

- [ ] **Run the full check** to make sure Phase 00 leaves the repo green:

```bash
cd frontend && bun nx test web-portal --watch=false && bun nx run web-portal:lint && bun nx build web-portal
```

Expected: all three succeed. Test count is `362 + (5 type helpers) + (10 url-state) + (7 new API tests) = 384` (give or take).

- [ ] **Smoke-check the route** (if dev server is running): visit `http://localhost:4200/interactive-city`. The page should render the title + subtitle from the new i18n keys; everything else is the empty stub. Header link still goes to `/interactive-city`.

- [ ] **Hand off to Phase 01.** Phase 01 fills in `ScenarioBuilderStore.init()`, the URL hydration effect, and the actual layout slots inside `scenario-builder.page.html`. Plan file: `phase-01-store-and-shell.md` (to be written when we're ready to start it).

**Phase 00 done when:**
- `web-portal-v0.2.0` test count grows by ~22 to ~384.
- 5 commits land on `main` (one per task), each with green CI.
- The new i18n keys exist in both EN and AR with parity.
- `routes.ts` points at `ScenarioBuilderPage` and the old Phase 9 page is deleted.

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { InteractiveCityApiService } from './interactive-city-api.service';
import type {
  CityTechnology,
  RunRequest,
  RunResult,
  SaveRequest,
  SavedScenario,
} from './interactive-city.types';

const SAMPLE: CityTechnology = {
  id: 't1',
  nameAr: 'تقنية', nameEn: 'Tech',
  descriptionAr: 'وصف', descriptionEn: 'Description',
  categoryAr: 'فئة', categoryEn: 'Category',
  carbonImpactKgPerYear: 100, costUsd: 1000,
  iconUrl: null,
};

describe('InteractiveCityApiService', () => {
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

  it('listTechnologies GETs /api/interactive-city/technologies', async () => {
    const promise = sut.listTechnologies();
    const req = http.expectOne('/api/interactive-city/technologies');
    expect(req.request.method).toBe('GET');
    req.flush([SAMPLE]);
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('returns server error on 500', async () => {
    const promise = sut.listTechnologies();
    http.expectOne('/api/interactive-city/technologies').flush('', { status: 500, statusText: 'Server Error' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('server');
  });
});

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
    if (!res.ok) expect(res.error.kind).toBe('server');
  });

  it('saveScenario maps 401 to { kind: "unknown", status: 401 } so the page can sign in', async () => {
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
    if (!res.ok) {
      expect(res.error.kind).toBe('unknown');
      if (res.error.kind === 'unknown') expect(res.error.status).toBe(401);
    }
  });

  it('deleteMyScenario URL-encodes id', async () => {
    const promise = sut.deleteMyScenario('a/b');
    const req = http.expectOne('/api/me/interactive-city/scenarios/a%2Fb');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
    await promise;
  });
});

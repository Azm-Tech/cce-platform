import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { InteractiveCityApiService } from './interactive-city-api.service';
import type { CityTechnology } from './interactive-city.types';

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

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CountriesApiService } from './countries-api.service';
import type { Country, CountryProfile } from './country.types';

const SAMPLE_COUNTRY: Country = {
  id: 'c1',
  isoAlpha3: 'JOR',
  isoAlpha2: 'JO',
  nameAr: 'الأردن',
  nameEn: 'Jordan',
  regionAr: 'المشرق',
  regionEn: 'Levant',
  flagUrl: 'https://example.test/jo.svg',
};

const SAMPLE_PROFILE: CountryProfile = {
  id: 'p1',
  countryId: 'c1',
  descriptionAr: 'وصف',
  descriptionEn: 'Description',
  keyInitiativesAr: 'مبادرات',
  keyInitiativesEn: 'Initiatives',
  contactInfoAr: null,
  contactInfoEn: null,
  lastUpdatedOn: '2026-04-29T12:00:00Z',
};

describe('CountriesApiService', () => {
  let sut: CountriesApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(CountriesApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('listCountries with no opts GETs /api/countries with no query string', async () => {
    const promise = sut.listCountries();
    const req = http.expectOne((r) => r.url === '/api/countries');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys()).toEqual([]);
    req.flush([SAMPLE_COUNTRY]);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toEqual([SAMPLE_COUNTRY]);
  });

  it('listCountries({ search }) builds ?search= query string', async () => {
    const promise = sut.listCountries({ search: 'jo' });
    const req = http.expectOne((r) => r.url === '/api/countries');
    expect(req.request.params.get('search')).toBe('jo');
    req.flush([SAMPLE_COUNTRY]);
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('getProfile GETs /api/countries/{id}/profile', async () => {
    const promise = sut.getProfile('c1');
    const req = http.expectOne('/api/countries/c1/profile');
    expect(req.request.method).toBe('GET');
    req.flush(SAMPLE_PROFILE);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value.countryId).toBe('c1');
  });

  it('getProfile returns not-found on 404', async () => {
    const promise = sut.getProfile('missing');
    http
      .expectOne('/api/countries/missing/profile')
      .flush('', { status: 404, statusText: 'Not Found' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('not-found');
  });
});

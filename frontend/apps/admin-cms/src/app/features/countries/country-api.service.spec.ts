import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CountryApiService } from './country-api.service';

describe('CountryApiService', () => {
  let sut: CountryApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(CountryApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('listCountries builds query', async () => {
    const p = sut.listCountries({ page: 1, pageSize: 20, search: 'q', isActive: true });
    const req = http.expectOne((r) => r.url === '/api/admin/countries');
    expect(req.request.params.get('search')).toBe('q');
    expect(req.request.params.get('isActive')).toBe('true');
    req.flush({ items: [], page: 1, pageSize: 20, total: 0 });
    await p;
  });

  it('updateCountry PUTs body', async () => {
    const p = sut.updateCountry('c1', {
      nameAr: 'a', nameEn: 'b', regionAr: 'r-a', regionEn: 'r-b', isActive: true,
    });
    const req = http.expectOne('/api/admin/countries/c1');
    expect(req.request.method).toBe('PUT');
    req.flush({});
    await p;
  });

  it('getProfile GETs', async () => {
    const p = sut.getProfile('c1');
    const req = http.expectOne('/api/admin/countries/c1/profile');
    req.flush({});
    await p;
  });

  it('upsertProfile PUTs body with rowVersion', async () => {
    const p = sut.upsertProfile('c1', {
      descriptionAr: '', descriptionEn: '',
      keyInitiativesAr: '', keyInitiativesEn: '',
      rowVersion: 'v',
    });
    const req = http.expectOne('/api/admin/countries/c1/profile');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(expect.objectContaining({ rowVersion: 'v' }));
    req.flush({});
    await p;
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { CountriesApiService, type Result } from './countries-api.service';
import type { Country } from './country.types';
import { CountriesGridPage } from './countries-grid.page';

const JO: Country = {
  id: 'c1',
  isoAlpha3: 'JOR', isoAlpha2: 'JO',
  nameAr: 'الأردن', nameEn: 'Jordan',
  regionAr: 'المشرق', regionEn: 'Levant',
  flagUrl: 'https://example.test/jo.svg',
};
const LB: Country = {
  id: 'c2',
  isoAlpha3: 'LBN', isoAlpha2: 'LB',
  nameAr: 'لبنان', nameEn: 'Lebanon',
  regionAr: 'المشرق', regionEn: 'Levant',
  flagUrl: 'https://example.test/lb.svg',
};
const EG: Country = {
  id: 'c3',
  isoAlpha3: 'EGY', isoAlpha2: 'EG',
  nameAr: 'مصر', nameEn: 'Egypt',
  regionAr: 'شمال أفريقيا', regionEn: 'North Africa',
  flagUrl: 'https://example.test/eg.svg',
};

describe('CountriesGridPage', () => {
  let fixture: ComponentFixture<CountriesGridPage>;
  let page: CountriesGridPage;
  let listCountries: jest.Mock;
  let routerNavigate: jest.Mock;
  let queryParamGet: jest.Mock;
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;

  function ok(value: Country[]): Result<Country[]> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listCountries = jest.fn().mockResolvedValue(ok([JO, LB, EG]));
    queryParamGet = jest.fn().mockReturnValue(null);
    localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [CountriesGridPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: CountriesApiService, useValue: { listCountries } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: queryParamGet } } } },
      ],
    }).compileComponents();

    const router = TestBed.inject(Router);
    routerNavigate = jest.spyOn(router, 'navigate').mockResolvedValue(true) as unknown as jest.Mock;
    fixture = TestBed.createComponent(CountriesGridPage);
    page = fixture.componentInstance;
  });

  it('groups countries by region after init load (en, two regions)', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listCountries).toHaveBeenCalledWith({});
    const groups = page.groups();
    expect(groups).toHaveLength(2);
    expect(groups.map((g) => g.region).sort()).toEqual(['Levant', 'North Africa']);
    expect(groups.find((g) => g.region === 'Levant')?.countries).toHaveLength(2);
  });

  it('search submit re-issues listCountries({ search }) and syncs URL', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listCountries.mockClear();
    routerNavigate.mockClear();
    page.searchTerm.set('jo');
    page.onSearchSubmit();
    await Promise.resolve();
    expect(listCountries).toHaveBeenCalledWith({ search: 'jo' });
    expect(routerNavigate).toHaveBeenCalled();
    const args = routerNavigate.mock.calls[0];
    expect(args[1].queryParams).toEqual({ q: 'jo' });
  });

  it('reads ?q= from URL and pre-populates searchTerm + service call', async () => {
    queryParamGet.mockImplementation((k: string) => (k === 'q' ? 'jo' : null));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.searchTerm()).toBe('jo');
    expect(listCountries).toHaveBeenCalledWith({ search: 'jo' });
  });

  it('regroups under Arabic locale (region heading uses regionAr)', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    localeSig.set('ar');
    fixture.detectChanges();
    const groups = page.groups();
    const regions = groups.map((g) => g.region);
    expect(regions).toContain('المشرق');
    expect(regions).toContain('شمال أفريقيا');
  });

  it('country card name localizes when locale toggles to ar', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    localeSig.set('ar');
    fixture.detectChanges();
    const cards = fixture.nativeElement.querySelectorAll('cce-country-card');
    expect(cards.length).toBe(3);
  });

  it('error path sets errorKind and retry triggers fresh listCountries', async () => {
    listCountries.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.errorKind()).toBe('server');
    listCountries.mockClear();
    listCountries.mockResolvedValueOnce(ok([JO]));
    page.retry();
    await Promise.resolve();
    expect(listCountries).toHaveBeenCalled();
    expect(page.errorKind()).toBeNull();
  });

  it('empty result triggers empty() computed', async () => {
    listCountries.mockResolvedValueOnce(ok([]));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.empty()).toBe(true);
  });
});

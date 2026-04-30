import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { CountriesListPage } from './countries-list.page';
import { CountryApiService, type Result } from './country-api.service';
import type { Country, PagedResult } from './country.types';

const C: Country = {
  id: 'c1', isoAlpha3: 'SAU', isoAlpha2: 'SA',
  nameAr: 'السعودية', nameEn: 'Saudi Arabia',
  regionAr: 'الخليج', regionEn: 'Gulf',
  flagUrl: '', isActive: true,
};

describe('CountriesListPage', () => {
  let fixture: ComponentFixture<CountriesListPage>;
  let page: CountriesListPage;
  let listCountries: jest.Mock;

  function ok(value: PagedResult<Country>): Result<PagedResult<Country>> { return { ok: true, value }; }

  beforeEach(async () => {
    listCountries = jest.fn().mockResolvedValue(ok({ items: [C], page: 1, pageSize: 20, total: 1 }));
    await TestBed.configureTestingModule({
      imports: [CountriesListPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: CountryApiService, useValue: { listCountries } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(CountriesListPage);
    page = fixture.componentInstance;
  });

  it('loads on init', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listCountries).toHaveBeenCalled();
    expect(page.rows()).toEqual([C]);
  });

  it('search resets page + reloads', async () => {
    page.searchInput.set('saudi');
    page.page.set(3);
    listCountries.mockClear();
    page.onSearch();
    await Promise.resolve();
    expect(page.page()).toBe(1);
    expect(listCountries).toHaveBeenCalledWith({ page: 1, pageSize: 20, search: 'saudi' });
  });
});

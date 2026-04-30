import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '../../core/ui/toast.service';
import { CountryApiService, type Result } from './country-api.service';
import { CountryDetailPage } from './country-detail.page';
import type { Country, CountryProfile } from './country.types';

const C: Country = {
  id: 'c1', isoAlpha3: 'SAU', isoAlpha2: 'SA',
  nameAr: 'السعودية', nameEn: 'Saudi Arabia',
  regionAr: 'الخليج', regionEn: 'Gulf',
  flagUrl: '', isActive: true,
};
const P: CountryProfile = {
  id: 'p1', countryId: 'c1',
  descriptionAr: 'a', descriptionEn: 'b',
  keyInitiativesAr: 'c', keyInitiativesEn: 'd',
  contactInfoAr: null, contactInfoEn: null,
  lastUpdatedById: 'admin', lastUpdatedOn: '2026-04-29',
  rowVersion: 'v',
};

describe('CountryDetailPage', () => {
  let fixture: ComponentFixture<CountryDetailPage>;
  let page: CountryDetailPage;
  let getCountry: jest.Mock;
  let getProfile: jest.Mock;
  let updateCountry: jest.Mock;
  let upsertProfile: jest.Mock;
  let toast: { success: jest.Mock; error: jest.Mock };

  function ok<T>(value: T): Result<T> { return { ok: true, value }; }

  beforeEach(async () => {
    getCountry = jest.fn().mockResolvedValue(ok(C));
    getProfile = jest.fn().mockResolvedValue(ok(P));
    updateCountry = jest.fn().mockResolvedValue(ok(C));
    upsertProfile = jest.fn().mockResolvedValue(ok(P));
    toast = { success: jest.fn(), error: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [CountryDetailPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: CountryApiService, useValue: { getCountry, getProfile, updateCountry, upsertProfile } },
        { provide: ToastService, useValue: toast },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'c1' } } } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(CountryDetailPage);
    page = fixture.componentInstance;
  });

  it('loads country + profile on init', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(getCountry).toHaveBeenCalledWith('c1');
    expect(getProfile).toHaveBeenCalledWith('c1');
    expect(page.country()).toEqual(C);
    expect(page.profile()).toEqual(P);
  });

  it('marks profileMissing when 404 on profile', async () => {
    getProfile.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.profileMissing()).toBe(true);
    expect(page.profile()).toBeNull();
  });

  it('saveCountry PUTs the form values', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.countryForm.patchValue({ nameAr: 'new-ar' });
    await page.saveCountry();
    expect(updateCountry).toHaveBeenCalledWith('c1', expect.objectContaining({ nameAr: 'new-ar' }));
    expect(toast.success).toHaveBeenCalledWith('countries.edit.toast');
  });

  it('saveProfile PUTs with rowVersion from current profile', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.profileForm.patchValue({
      descriptionAr: 'a', descriptionEn: 'b', keyInitiativesAr: 'c', keyInitiativesEn: 'd',
    });
    await page.saveProfile();
    expect(upsertProfile).toHaveBeenCalledWith('c1', expect.objectContaining({ rowVersion: 'v' }));
    expect(toast.success).toHaveBeenCalledWith('countries.profile.toast');
  });
});

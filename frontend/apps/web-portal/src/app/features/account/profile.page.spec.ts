import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { AccountApiService, type Result } from './account-api.service';
import { CountriesApiService } from '../countries/countries-api.service';
import type { Country } from '../countries/country.types';
import type { UserProfile } from './account.types';
import { ProfilePage } from './profile.page';

const PROFILE: UserProfile = {
  id: 'u1',
  email: 'jane@example.test',
  userName: 'jane',
  localePreference: 'en',
  knowledgeLevel: 'Beginner',
  interests: ['waste'],
  countryId: 'c1',
  avatarUrl: null,
};

const JO: Country = {
  id: 'c1',
  isoAlpha3: 'JOR', isoAlpha2: 'JO',
  nameAr: 'الأردن', nameEn: 'Jordan',
  regionAr: 'المشرق', regionEn: 'Levant',
  flagUrl: 'https://example.test/jo.svg',
};

describe('ProfilePage', () => {
  let fixture: ComponentFixture<ProfilePage>;
  let page: ProfilePage;
  let getProfile: jest.Mock;
  let listCountries: jest.Mock;
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;

  function ok<T>(value: T): Result<T> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    getProfile = jest.fn().mockResolvedValue(ok(PROFILE));
    listCountries = jest.fn().mockResolvedValue(ok([JO]));
    localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [ProfilePage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: AccountApiService,
          useValue: { getProfile, updateProfile: jest.fn() },
        },
        { provide: CountriesApiService, useValue: { listCountries } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ProfilePage);
    page = fixture.componentInstance;
  });

  it('loads profile + countries on init and binds DOM', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(getProfile).toHaveBeenCalled();
    expect(listCountries).toHaveBeenCalledWith({});
    expect(page.profile()).toEqual(PROFILE);
    expect(page.countryName()).toBe('Jordan');
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('jane');
    expect(html).toContain('jane@example.test');
    expect(html).toContain('waste');
  });

  it('404 path renders the not-provisioned hint and retry triggers fresh fetch', async () => {
    getProfile.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.notProvisioned()).toBe(true);
    expect(page.profile()).toBeNull();
    getProfile.mockClear();
    getProfile.mockResolvedValueOnce(ok(PROFILE));
    page.retry();
    await Promise.resolve();
    expect(getProfile).toHaveBeenCalledTimes(1);
  });

  it('locale toggle re-resolves country name to ar', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.countryName()).toBe('Jordan');
    localeSig.set('ar');
    expect(page.countryName()).toBe('الأردن');
  });
});

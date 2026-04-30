import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
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
  let updateProfile: jest.Mock;
  let listCountries: jest.Mock;
  let toastSuccess: jest.Mock;
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;

  function ok<T>(value: T): Result<T> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    getProfile = jest.fn().mockResolvedValue(ok(PROFILE));
    updateProfile = jest.fn().mockResolvedValue(ok(PROFILE));
    listCountries = jest.fn().mockResolvedValue(ok([JO]));
    toastSuccess = jest.fn();
    localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [ProfilePage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: AccountApiService,
          useValue: { getProfile, updateProfile },
        },
        { provide: CountriesApiService, useValue: { listCountries } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ToastService, useValue: { success: toastSuccess, error: jest.fn() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ProfilePage);
    page = fixture.componentInstance;
  });

  // ====== Read mode (Phase 6.3) ======

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

  // ====== Edit mode (Phase 6.4) ======

  it('enterEditMode patches the form from the current profile and switches mode', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.mode()).toBe('view');
    page.enterEditMode();
    expect(page.mode()).toBe('edit');
    const v = page.form.getRawValue();
    expect(v.localePreference).toBe('en');
    expect(v.knowledgeLevel).toBe('Beginner');
    expect(v.interests).toBe('waste');
    expect(v.countryId).toBe('c1');
    expect(v.avatarUrl).toBeNull();
  });

  it('save() with valid form calls updateProfile with the parsed payload', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.enterEditMode();
    page.form.patchValue({
      localePreference: 'ar',
      knowledgeLevel: 'Intermediate',
      interests: 'waste, water,  carbon , water', // dup + whitespace test
      countryId: null,
      avatarUrl: 'https://example.test/a.png',
    });
    await page.save();
    expect(updateProfile).toHaveBeenCalledWith({
      localePreference: 'ar',
      knowledgeLevel: 'Intermediate',
      interests: ['waste', 'water', 'carbon'],
      avatarUrl: 'https://example.test/a.png',
      countryId: null,
    });
  });

  it('on success: profile updates, mode reverts to view, toast.success fired', async () => {
    const updated: UserProfile = { ...PROFILE, knowledgeLevel: 'Advanced' };
    updateProfile.mockResolvedValueOnce(ok(updated));
    fixture.detectChanges();
    await fixture.whenStable();
    page.enterEditMode();
    page.form.patchValue({ knowledgeLevel: 'Advanced' });
    await page.save();
    expect(page.profile()).toEqual(updated);
    expect(page.mode()).toBe('view');
    expect(toastSuccess).toHaveBeenCalledWith('account.profile.toast.saved');
  });

  it('on error: error banner renders, mode stays edit, profile is unchanged', async () => {
    updateProfile.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    page.enterEditMode();
    await page.save();
    expect(page.saveErrorKind()).toBe('server');
    expect(page.mode()).toBe('edit');
    expect(page.profile()).toEqual(PROFILE);
    expect(toastSuccess).not.toHaveBeenCalled();
  });

  it('cancelEdit reverts to view mode without saving', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.enterEditMode();
    page.form.patchValue({ knowledgeLevel: 'Advanced' });
    page.cancelEdit();
    expect(page.mode()).toBe('view');
    expect(page.profile()).toEqual(PROFILE);
    expect(updateProfile).not.toHaveBeenCalled();
  });
});

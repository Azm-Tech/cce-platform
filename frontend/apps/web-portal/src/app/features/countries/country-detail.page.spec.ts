import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { CountriesApiService, type Result } from './countries-api.service';
import { KapsarcApiService } from './kapsarc-api.service';
import type { Country, CountryProfile, KapsarcSnapshot } from './country.types';
import { CountryDetailPage } from './country-detail.page';

const COUNTRY: Country = {
  id: 'c1',
  isoAlpha3: 'JOR', isoAlpha2: 'JO',
  nameAr: 'الأردن', nameEn: 'Jordan',
  regionAr: 'المشرق', regionEn: 'Levant',
  flagUrl: 'https://example.test/jo.svg',
};
const PROFILE: CountryProfile = {
  id: 'p1',
  countryId: 'c1',
  descriptionAr: 'وصف الأردن',
  descriptionEn: 'Jordan description',
  keyInitiativesAr: 'مبادرات',
  keyInitiativesEn: 'Initiatives',
  contactInfoAr: null,
  contactInfoEn: null,
  lastUpdatedOn: '2026-04-29T12:00:00Z',
};
const SNAPSHOT: KapsarcSnapshot = {
  id: 's1',
  countryId: 'c1',
  classification: 'Advanced',
  performanceScore: 85.5,
  totalIndex: 92.1,
  snapshotTakenOn: '2026-03-15T00:00:00Z',
  sourceVersion: 'v2025.4',
};

describe('CountryDetailPage', () => {
  let fixture: ComponentFixture<CountryDetailPage>;
  let page: CountryDetailPage;
  let getProfile: jest.Mock;
  let listCountries: jest.Mock;
  let getLatestSnapshot: jest.Mock;
  let paramGet: jest.Mock;
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;

  function ok<T>(value: T): Result<T> {
    return { ok: true, value };
  }

  async function setup(opts: { id?: string | null } = {}) {
    paramGet = jest.fn().mockReturnValue(opts.id === undefined ? 'c1' : opts.id);
    localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [CountryDetailPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: CountriesApiService,
          useValue: { getProfile, listCountries },
        },
        { provide: KapsarcApiService, useValue: { getLatestSnapshot } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: paramGet } } } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(CountryDetailPage);
    page = fixture.componentInstance;
  }

  beforeEach(() => {
    getProfile = jest.fn().mockResolvedValue(ok(PROFILE));
    listCountries = jest.fn().mockResolvedValue(ok([COUNTRY]));
    getLatestSnapshot = jest.fn().mockResolvedValue(ok(SNAPSHOT));
  });

  it('loads profile + snapshot + country header on init from :id', async () => {
    await setup();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(getProfile).toHaveBeenCalledWith('c1');
    expect(getLatestSnapshot).toHaveBeenCalledWith('c1');
    expect(page.profile()).toEqual(PROFILE);
    expect(page.snapshot()).toEqual(SNAPSHOT);
    expect(page.country()?.id).toBe('c1');
    expect(page.description()).toBe('Jordan description');
  });

  it('errorKind=not-found on profile 404 (renders not-found block)', async () => {
    getProfile.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    await setup();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.errorKind()).toBe('not-found');
  });

  it('locale toggle updates description + key initiatives', async () => {
    await setup();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.description()).toBe('Jordan description');
    expect(page.keyInitiatives()).toBe('Initiatives');
    localeSig.set('ar');
    expect(page.description()).toBe('وصف الأردن');
    expect(page.keyInitiatives()).toBe('مبادرات');
    expect(page.headerName()).toBe('الأردن');
  });

  it('renders inline empty message when KAPSARC snapshot is null (profile still visible)', async () => {
    getLatestSnapshot.mockResolvedValueOnce(ok(null));
    await setup();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.snapshot()).toBeNull();
    expect(page.snapshotErrorKind()).toBeNull();
    expect(page.profile()).toEqual(PROFILE);
  });

  it('renders inline error note when KAPSARC fetch fails (profile still visible)', async () => {
    getLatestSnapshot.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    await setup();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.snapshotErrorKind()).toBe('server');
    expect(page.profile()).toEqual(PROFILE);
  });

  it('back link points to /countries', async () => {
    await setup();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const back = fixture.nativeElement.querySelector('.cce-country-detail__back') as HTMLAnchorElement;
    expect(back).not.toBeNull();
    expect(back.getAttribute('href')).toBe('/countries');
  });
});

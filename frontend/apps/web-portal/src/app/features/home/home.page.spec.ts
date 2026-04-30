import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { HomeApiService, type Result } from './home-api.service';
import type { HomepageSection } from './home.types';
import { HomePage } from './home.page';

const makeSection = (overrides: Partial<HomepageSection> = {}): HomepageSection => ({
  id: 'sec-1',
  sectionType: 'Hero',
  orderIndex: 0,
  contentAr: '<h1>مرحبا</h1>',
  contentEn: '<h1>Welcome</h1>',
  isActive: true,
  ...overrides,
});

describe('HomePage', () => {
  let fixture: ComponentFixture<HomePage>;
  let page: HomePage;
  let listSections: jest.Mock;
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;

  function ok(value: HomepageSection[]): Result<HomepageSection[]> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listSections = jest.fn().mockResolvedValue(ok([makeSection()]));
    localeSig = signal<'ar' | 'en'>('en');
    const localeStub = { locale: localeSig.asReadonly() };

    await TestBed.configureTestingModule({
      imports: [HomePage, TranslateModule.forRoot()],
      providers: [
        { provide: HomeApiService, useValue: { listSections } },
        { provide: LocaleService, useValue: localeStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HomePage);
    page = fixture.componentInstance;
  });

  it('ngOnInit calls listSections and populates sections signal on success', async () => {
    const sections = [makeSection({ id: 'sec-1' }), makeSection({ id: 'sec-2', orderIndex: 1 })];
    listSections.mockResolvedValueOnce(ok(sections));

    fixture.detectChanges();
    await fixture.whenStable();

    expect(listSections).toHaveBeenCalledTimes(1);
    expect(page.sections()).toEqual(sections);
    expect(page.errorKind()).toBeNull();
  });

  it('sets errorKind when api returns { ok: false, error: { kind: "server" } }', async () => {
    listSections.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });

    fixture.detectChanges();
    await fixture.whenStable();

    expect(page.errorKind()).toBe('server');
    expect(page.sections()).toEqual([]);
  });

  it('localizedSections() filters out isActive: false entries', async () => {
    const active = makeSection({ id: 'active-1', isActive: true });
    const inactive = makeSection({ id: 'inactive-1', isActive: false });
    listSections.mockResolvedValueOnce(ok([active, inactive]));

    fixture.detectChanges();
    await fixture.whenStable();

    const result = page.localizedSections();
    expect(result.map((s) => s.id)).toEqual(['active-1']);
  });

  it('localizedSections() sorts by orderIndex ascending', async () => {
    const s3 = makeSection({ id: 's3', orderIndex: 3 });
    const s1 = makeSection({ id: 's1', orderIndex: 1 });
    const s2 = makeSection({ id: 's2', orderIndex: 2 });
    listSections.mockResolvedValueOnce(ok([s3, s1, s2]));

    fixture.detectChanges();
    await fixture.whenStable();

    expect(page.localizedSections().map((s) => s.id)).toEqual(['s1', 's2', 's3']);
  });

  it('localizedSections() returns contentAr when locale is ar, contentEn when en', async () => {
    const section = makeSection({ contentAr: '<p>عربي</p>', contentEn: '<p>English</p>' });
    listSections.mockResolvedValueOnce(ok([section]));

    fixture.detectChanges();
    await fixture.whenStable();

    // Default locale is 'en'
    expect(page.localizedSections()[0].content).toBe('<p>English</p>');

    // Switch to Arabic
    localeSig.set('ar');
    expect(page.localizedSections()[0].content).toBe('<p>عربي</p>');

    // Switch back to English
    localeSig.set('en');
    expect(page.localizedSections()[0].content).toBe('<p>English</p>');
  });
});

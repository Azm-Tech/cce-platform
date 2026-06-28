import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { EventsApiService, type Result } from './events-api.service';
import type { Event } from './event.types';
import { EventDetailPage } from './event-detail.page';

const SAMPLE: Event = {
  id: 'e1',
  titleAr: 'فعالية', titleEn: 'Event',
  descriptionAr: 'وصف', descriptionEn: 'description',
  startsOn: '2026-05-01T10:00:00Z',
  endsOn: '2026-05-01T12:00:00Z',
  locationAr: null, locationEn: 'HQ',
  onlineMeetingUrl: null, featuredImageUrl: null,
  iCalUid: 'uid',
};

const OTHER: Event = { ...SAMPLE, id: 'e2', titleEn: 'Other', titleAr: 'أخرى' };

describe('EventDetailPage', () => {
  let fixture: ComponentFixture<EventDetailPage>;
  let page: EventDetailPage;
  let getEvent: jest.Mock;
  let listEvents: jest.Mock;
  let downloadIcs: jest.Mock;
  let toast: { success: jest.Mock; error: jest.Mock };
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;

  function ok<T>(value: T): Result<T> { return { ok: true, value }; }

  beforeEach(async () => {
    getEvent = jest.fn().mockResolvedValue(ok(SAMPLE));
    listEvents = jest.fn().mockResolvedValue(ok({ items: [SAMPLE, OTHER], total: 2, page: 1, pageSize: 6 }));
    downloadIcs = jest.fn();
    toast = { success: jest.fn(), error: jest.fn() };
    localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [
        EventDetailPage,
        TranslocoTestingModule.forRoot({ langs: { en: {}, ar: {} }, translocoConfig: { availableLangs: ['en', 'ar'], defaultLang: 'en' } }),
      ],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: EventsApiService, useValue: { getEvent, listEvents, downloadIcs } },
        { provide: ToastService, useValue: toast },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'e1' } } } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(EventDetailPage);
    page = fixture.componentInstance;
  });

  it('loads event on init', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(getEvent).toHaveBeenCalledWith('e1');
    expect(page.event()).toEqual(SAMPLE);
  });

  it('sets errorKind on 404', async () => {
    getEvent.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.errorKind()).toBe('not-found');
  });

  it('locale toggles title/description/venue computed', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.title()).toBe('Event');
    expect(page.description()).toBe('description');
    expect(page.venue()).toBe('HQ');
    localeSig.set('ar');
    expect(page.title()).toBe('فعالية');
    expect(page.description()).toBe('وصف');
    expect(page.venue()).toBeNull();
  });

  it('loads other events excluding the current one', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    // loadOtherEvents is fire-and-forget — flush its promise chain.
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(listEvents).toHaveBeenCalled();
    expect(page.otherEvents().map((e) => e.id)).toEqual(['e2']);
  });

  it('hides speakers and outcomes when the API does not send them', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.speakers()).toEqual([]);
    expect(page.outcomes()).toBeNull();
  });

  it('exposes locale-aware speakers and outcomes when present', async () => {
    getEvent.mockResolvedValueOnce(ok({
      ...SAMPLE,
      speakers: [{ nameAr: 'أحمد', nameEn: 'Ahmed', roleAr: 'خبير', roleEn: 'Expert', imageUrl: null }],
      outcomesAr: 'مخرجات', outcomesEn: 'Outcomes',
    }));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.speakers()).toEqual([{ name: 'Ahmed', role: 'Expert', imageUrl: null }]);
    expect(page.outcomes()).toBe('Outcomes');
    localeSig.set('ar');
    expect(page.speakers()[0].name).toBe('أحمد');
    expect(page.outcomes()).toBe('مخرجات');
  });

  it('exportToCalendar materializes blob and toasts success', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    downloadIcs.mockResolvedValueOnce(ok(new Blob(['BEGIN:VCALENDAR'], { type: 'text/calendar' })));
    Object.defineProperty(URL, 'createObjectURL', { value: () => 'blob:x', configurable: true });
    Object.defineProperty(URL, 'revokeObjectURL', { value: jest.fn(), configurable: true });
    const a = document.createElement('a');
    Object.defineProperty(a, 'click', { value: jest.fn() });
    jest.spyOn(document, 'createElement').mockReturnValueOnce(a);

    await page.exportToCalendar();

    expect(downloadIcs).toHaveBeenCalledWith('e1');
    expect(toast.success).toHaveBeenCalledWith('events.export.toast');
  });

  it('exportToCalendar surfaces error via toast.error', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    downloadIcs.mockResolvedValueOnce({ ok: false, error: { kind: 'forbidden' } });
    await page.exportToCalendar();
    expect(toast.error).toHaveBeenCalledWith('errors.forbidden');
  });
});

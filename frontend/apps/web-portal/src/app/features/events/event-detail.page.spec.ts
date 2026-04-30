import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslateModule } from '@ngx-translate/core';
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

describe('EventDetailPage', () => {
  let fixture: ComponentFixture<EventDetailPage>;
  let page: EventDetailPage;
  let getEvent: jest.Mock;
  let downloadIcs: jest.Mock;
  let toast: { success: jest.Mock; error: jest.Mock };
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;

  function ok<T>(value: T): Result<T> { return { ok: true, value }; }

  beforeEach(async () => {
    getEvent = jest.fn().mockResolvedValue(ok(SAMPLE));
    downloadIcs = jest.fn();
    toast = { success: jest.fn(), error: jest.fn() };
    localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [EventDetailPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: EventsApiService, useValue: { getEvent, downloadIcs } },
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

  it('locale toggles title/description/location computed', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.title()).toBe('Event');
    expect(page.description()).toBe('description');
    expect(page.location()).toBe('HQ');
    localeSig.set('ar');
    expect(page.title()).toBe('فعالية');
    expect(page.description()).toBe('وصف');
    expect(page.location()).toBeNull();
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

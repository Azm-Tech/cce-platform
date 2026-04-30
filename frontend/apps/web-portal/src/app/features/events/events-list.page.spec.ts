import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { EventsApiService, type Result } from './events-api.service';
import type { Event, PagedResult } from './event.types';
import { EventsListPage } from './events-list.page';

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

describe('EventsListPage', () => {
  let fixture: ComponentFixture<EventsListPage>;
  let page: EventsListPage;
  let listEvents: jest.Mock;
  let queryParamGet: jest.Mock;

  function ok(value: PagedResult<Event>): Result<PagedResult<Event>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listEvents = jest.fn().mockResolvedValue(ok({ items: [SAMPLE], page: 1, pageSize: 12, total: 1 }));
    queryParamGet = jest.fn().mockReturnValue(null);
    const localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [EventsListPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: EventsApiService, useValue: { listEvents } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: queryParamGet } } } },
      ],
    }).compileComponents();
    const router = TestBed.inject(Router);
    jest.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(EventsListPage);
    page = fixture.componentInstance;
  });

  it('loads on init with default paging', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listEvents).toHaveBeenCalledWith({ page: 1, pageSize: 12, from: undefined, to: undefined });
  });

  it('reads from + to query params on init', async () => {
    queryParamGet.mockImplementation((k: string) => {
      const m: Record<string, string> = { from: '2026-01-01', to: '2026-12-31' };
      return m[k] ?? null;
    });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.from()).toBe('2026-01-01');
    expect(page.to()).toBe('2026-12-31');
    expect(listEvents).toHaveBeenCalledWith({ page: 1, pageSize: 12, from: '2026-01-01', to: '2026-12-31' });
  });

  it('onFromChange resets page + reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.page.set(3);
    listEvents.mockClear();
    page.onFromChange('2026-06-01');
    await Promise.resolve();
    expect(page.page()).toBe(1);
    expect(listEvents).toHaveBeenCalledWith(expect.objectContaining({ from: '2026-06-01' }));
  });

  it('onPage updates page + size and reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listEvents.mockClear();
    page.onPage({ pageIndex: 2, pageSize: 24, length: 1, previousPageIndex: 0 });
    await Promise.resolve();
    expect(page.page()).toBe(3);
    expect(page.pageSize()).toBe(24);
  });

  it('renders error banner when api fails', async () => {
    listEvents.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.errorKind()).toBe('server');
  });

  it('empty result triggers empty() computed', async () => {
    listEvents.mockResolvedValueOnce(ok({ items: [], page: 1, pageSize: 12, total: 0 }));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.empty()).toBe(true);
  });

  it('locationOf prefers onlineMeetingUrl, falls back to locale-specific location', () => {
    expect(page.locationOf({ ...SAMPLE, onlineMeetingUrl: 'https://meet.example/x' })).toBe('https://meet.example/x');
    expect(page.locationOf({ ...SAMPLE, onlineMeetingUrl: null, locationEn: 'HQ' })).toBe('HQ');
  });
});

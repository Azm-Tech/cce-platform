import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { EventsApiService } from './events-api.service';
import type { Event } from './event.types';

const SAMPLE: Event = {
  id: 'e1',
  titleAr: 'فعالية', titleEn: 'Event',
  descriptionAr: 'وصف', descriptionEn: 'description',
  startsOn: '2026-05-01T10:00:00Z',
  endsOn: '2026-05-01T12:00:00Z',
  locationAr: null, locationEn: 'HQ',
  onlineMeetingUrl: null,
  featuredImageUrl: null,
  iCalUid: 'uid-1',
};

describe('EventsApiService', () => {
  let sut: EventsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(EventsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('listEvents GETs /api/events with default empty params', async () => {
    const promise = sut.listEvents();
    const req = http.expectOne((r) => r.url === '/api/events');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys()).toEqual([]);
    req.flush({ items: [], page: 1, pageSize: 20, total: 0 });
    await promise;
  });

  it('listEvents builds from + to query string', async () => {
    const promise = sut.listEvents({ from: '2026-01-01', to: '2026-12-31' });
    const req = http.expectOne((r) => r.url === '/api/events');
    expect(req.request.params.get('from')).toBe('2026-01-01');
    expect(req.request.params.get('to')).toBe('2026-12-31');
    req.flush({ items: [SAMPLE], page: 1, pageSize: 20, total: 1 });
    await promise;
  });

  it('getEvent GETs /api/events/{id}', async () => {
    const promise = sut.getEvent('e1');
    const req = http.expectOne('/api/events/e1');
    req.flush(SAMPLE);
    const res = await promise;
    if (res.ok) expect(res.value.id).toBe('e1');
  });

  it('downloadIcs GETs /api/events/{id}.ics with responseType:blob', async () => {
    const promise = sut.downloadIcs('e1');
    const req = http.expectOne('/api/events/e1.ics');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob(['BEGIN:VCALENDAR'], { type: 'text/calendar' }));
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toBeInstanceOf(Blob);
  });

  it('getEvent returns not-found on 404', async () => {
    const promise = sut.getEvent('missing');
    http.expectOne('/api/events/missing').flush('', { status: 404, statusText: 'Not Found' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('not-found');
  });
});

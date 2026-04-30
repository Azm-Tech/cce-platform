import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { KapsarcApiService } from './kapsarc-api.service';
import type { KapsarcSnapshot } from './country.types';

const SAMPLE_SNAPSHOT: KapsarcSnapshot = {
  id: 's1',
  countryId: 'c1',
  classification: 'Advanced',
  performanceScore: 85.5,
  totalIndex: 92.1,
  snapshotTakenOn: '2026-03-15T00:00:00Z',
  sourceVersion: 'v2025.4',
};

describe('KapsarcApiService', () => {
  let sut: KapsarcApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(KapsarcApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getLatestSnapshot GETs /api/kapsarc/snapshots/{countryId}', async () => {
    const promise = sut.getLatestSnapshot('c1');
    const req = http.expectOne('/api/kapsarc/snapshots/c1');
    expect(req.request.method).toBe('GET');
    req.flush(SAMPLE_SNAPSHOT);
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('returns the snapshot on 200', async () => {
    const promise = sut.getLatestSnapshot('c1');
    http.expectOne('/api/kapsarc/snapshots/c1').flush(SAMPLE_SNAPSHOT);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) {
      expect(res.value).not.toBeNull();
      expect(res.value?.classification).toBe('Advanced');
    }
  });

  it('returns ok with null value on 404 (no snapshot is a valid empty state)', async () => {
    const promise = sut.getLatestSnapshot('c1');
    http
      .expectOne('/api/kapsarc/snapshots/c1')
      .flush('', { status: 404, statusText: 'Not Found' });
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toBeNull();
  });

  it('returns server feature error on 500', async () => {
    const promise = sut.getLatestSnapshot('c1');
    http
      .expectOne('/api/kapsarc/snapshots/c1')
      .flush('', { status: 500, statusText: 'Server Error' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('server');
  });

  it('returns network feature error on transport failure', async () => {
    const promise = sut.getLatestSnapshot('c1');
    http.expectOne('/api/kapsarc/snapshots/c1').error(new ProgressEvent('error'), { status: 0 });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('network');
  });
});

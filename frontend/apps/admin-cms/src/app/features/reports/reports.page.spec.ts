import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '@frontend/ui-kit';
import { ReportsApiService, type Result } from './reports-api.service';
import { ReportsPage } from './reports.page';
import { REPORTS } from './reports-config';

/**
 * No fixture.detectChanges() in these tests — we exercise the controller
 * methods directly. Rendering the 8 mat-cards in jsdom requires shimming
 * Material's avatar measurements and is overkill for the logic surface
 * exercised here. Service-level tests cover the HTTP contract;
 * service tests + integration smoke (admin-cms-e2e) cover rendering.
 */
describe('ReportsPage', () => {
  let page: ReportsPage;
  let download: jest.Mock;
  let toast: { success: jest.Mock; error: jest.Mock };

  function ok(value: Blob): Result<Blob> { return { ok: true, value }; }

  beforeEach(async () => {
    download = jest.fn();
    toast = { success: jest.fn(), error: jest.fn() };
    await TestBed.configureTestingModule({
      imports: [ReportsPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ReportsApiService, useValue: { download } },
        { provide: ToastService, useValue: toast },
        { provide: AuthService, useValue: { currentUser: () => null, isAuthenticated: () => false, hasPermission: () => true } },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(ReportsPage);
    page = fixture.componentInstance;
  });

  it('exposes 8 reports', () => {
    expect(page.reports).toHaveLength(8);
    expect(REPORTS.map((r) => r.slug)).toContain('users-registrations');
  });

  it('setFrom + setTo update the input maps', () => {
    page.setFrom('news', '2026-01-01');
    page.setTo('news', '2026-04-29');
    expect(page.fromOf('news')).toBe('2026-01-01');
    expect(page.toOf('news')).toBe('2026-04-29');
  });

  it('fromOf / toOf default to "" for unknown slugs', () => {
    expect(page.fromOf('events')).toBe('');
    expect(page.toOf('events')).toBe('');
  });

  it('download calls api with the date range', async () => {
    download.mockResolvedValueOnce(ok(new Blob([''], { type: 'text/csv' })));
    page.setFrom('news', '2026-01-01');
    page.setTo('news', '2026-04-29');

    // Stub the download path so jsdom does not need to materialise the blob link.
    Object.defineProperty(URL, 'createObjectURL', { value: () => 'blob:x', configurable: true });
    Object.defineProperty(URL, 'revokeObjectURL', { value: jest.fn(), configurable: true });
    const click = jest.fn();
    const a = document.createElement('a');
    Object.defineProperty(a, 'click', { value: click });
    jest.spyOn(document, 'createElement').mockReturnValueOnce(a);

    const newsReport = REPORTS.find((r) => r.slug === 'news')!;
    await page.download(newsReport);

    expect(download).toHaveBeenCalledWith('news', { from: '2026-01-01', to: '2026-04-29' });
    expect(click).toHaveBeenCalled();
    expect(a.download).toMatch(/^news-\d{4}-\d{2}-\d{2}\.csv$/);
    expect(toast.success).toHaveBeenCalledWith('reports.download.toast');
  });

  it('download omits date params when none are set', async () => {
    download.mockResolvedValueOnce(ok(new Blob([''], { type: 'text/csv' })));
    Object.defineProperty(URL, 'createObjectURL', { value: () => 'blob:x', configurable: true });
    Object.defineProperty(URL, 'revokeObjectURL', { value: jest.fn(), configurable: true });
    const a = document.createElement('a');
    Object.defineProperty(a, 'click', { value: jest.fn() });
    jest.spyOn(document, 'createElement').mockReturnValueOnce(a);

    await page.download(REPORTS[0]);

    expect(download).toHaveBeenCalledWith(REPORTS[0].slug, { from: undefined, to: undefined });
  });

  it('download surfaces api error via toast.error', async () => {
    download.mockResolvedValueOnce({ ok: false, error: { kind: 'forbidden' } });
    await page.download(REPORTS[0]);
    expect(toast.error).toHaveBeenCalledWith('errors.forbidden');
    expect(toast.success).not.toHaveBeenCalled();
  });

  it('busy signal tracks in-flight download per slug', async () => {
    let resolve!: (v: Result<Blob>) => void;
    download.mockReturnValueOnce(new Promise<Result<Blob>>((r) => { resolve = r; }));
    Object.defineProperty(URL, 'createObjectURL', { value: () => 'blob:x', configurable: true });
    Object.defineProperty(URL, 'revokeObjectURL', { value: jest.fn(), configurable: true });
    const a = document.createElement('a');
    Object.defineProperty(a, 'click', { value: jest.fn() });
    jest.spyOn(document, 'createElement').mockReturnValueOnce(a);

    const promise = page.download(REPORTS[0]);
    expect(page.busy()).toBe(REPORTS[0].slug);

    resolve(ok(new Blob([''], { type: 'text/csv' })));
    await promise;
    expect(page.busy()).toBeNull();
  });
});

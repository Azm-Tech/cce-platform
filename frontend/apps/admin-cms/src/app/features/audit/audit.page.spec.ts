import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TranslateModule } from '@ngx-translate/core';
import { AuditApiService, type Result } from './audit-api.service';
import type { AuditEvent, PagedResult } from './audit.types';
import { AuditPage } from './audit.page';

const E: AuditEvent = {
  id: 'e1', occurredOn: '2026-04-29T10:00:00Z',
  actor: 'admin@cce.local', action: 'Resource.Created',
  resource: 'Resource:abc', correlationId: 'cid-1', diff: '{"a":1}',
};

describe('AuditPage', () => {
  let fixture: ComponentFixture<AuditPage>;
  let page: AuditPage;
  let list: jest.Mock;

  function ok(value: PagedResult<AuditEvent>): Result<PagedResult<AuditEvent>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    list = jest.fn().mockResolvedValue(ok({ items: [E], page: 1, pageSize: 50, total: 1 }));
    await TestBed.configureTestingModule({
      imports: [AuditPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuditApiService, useValue: { list } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AuditPage);
    page = fixture.componentInstance;
  });

  it('loads on init with default paging (page=1, pageSize=50)', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(list).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 50 }));
    expect(page.rows()).toEqual([E]);
  });

  it('applyFilters resets page to 1 + reloads with filter values', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.page.set(3);
    page.actor.set('admin@cce.local');
    page.actionPrefix.set('Resource.');
    list.mockClear();
    page.applyFilters();
    await Promise.resolve();
    expect(page.page()).toBe(1);
    expect(list).toHaveBeenCalledWith(expect.objectContaining({
      actor: 'admin@cce.local',
      actionPrefix: 'Resource.',
    }));
  });

  it('clearFilters wipes input signals + reloads', async () => {
    page.actor.set('x');
    page.actionPrefix.set('Y.');
    page.from.set('2026-01-01');
    list.mockClear();
    page.clearFilters();
    await Promise.resolve();
    expect(page.actor()).toBe('');
    expect(page.actionPrefix()).toBe('');
    expect(page.from()).toBe('');
    expect(list).toHaveBeenCalled();
  });

  it('onPage updates page (1-based) + size and reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    list.mockClear();
    page.onPage({ pageIndex: 2, pageSize: 100, length: 1, previousPageIndex: 0 });
    await Promise.resolve();
    expect(page.page()).toBe(3);
    expect(page.pageSize()).toBe(100);
  });

  it('errorKind is set when api fails', async () => {
    list.mockResolvedValueOnce({ ok: false, error: { kind: 'forbidden' } });
    await page.load();
    expect(page.errorKind()).toBe('forbidden');
  });
});

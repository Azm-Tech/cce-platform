import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { ToastService } from '@frontend/ui-kit';
import { ExpertApiService, type Result } from './expert-api.service';
import type { ExpertRequest, PagedResult } from './expert.types';
import { ExpertRequestsListPage } from './expert-requests-list.page';

const SAMPLE: ExpertRequest = {
  id: 'r1',
  requestedById: 'u1',
  requestedByUserName: 'alice',
  requestedBioAr: 'سيرة',
  requestedBioEn: 'bio',
  requestedTags: ['ccs', 'methane'],
  submittedOn: '2026-04-29T00:00:00Z',
  status: 'Pending',
  processedById: null,
  processedOn: null,
  rejectionReasonAr: null,
  rejectionReasonEn: null,
};

describe('ExpertRequestsListPage', () => {
  let fixture: ComponentFixture<ExpertRequestsListPage>;
  let page: ExpertRequestsListPage;
  let listRequests: jest.Mock;
  let dialog: { open: jest.Mock };
  let dialogRef: { afterClosed: jest.Mock };
  let toast: { success: jest.Mock };

  function ok(value: PagedResult<ExpertRequest>): Result<PagedResult<ExpertRequest>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listRequests = jest.fn().mockResolvedValue(ok({ items: [SAMPLE], page: 1, pageSize: 20, total: 1 }));
    dialogRef = { afterClosed: jest.fn().mockReturnValue(of(null)) };
    dialog = { open: jest.fn().mockReturnValue(dialogRef) };
    toast = { success: jest.fn() };
    await TestBed.configureTestingModule({
      imports: [ExpertRequestsListPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: ExpertApiService, useValue: { listRequests } },
        { provide: MatDialog, useValue: dialog },
        { provide: ToastService, useValue: toast },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ExpertRequestsListPage);
    page = fixture.componentInstance;
  });

  it('loads with default Pending filter', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listRequests).toHaveBeenCalledWith({ page: 1, pageSize: 20, status: 'Pending' });
    expect(page.rows()).toEqual([SAMPLE]);
  });

  it('changing status filter resets page + reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.page.set(3);
    listRequests.mockClear();
    page.onStatusFilter('Approved');
    await Promise.resolve();
    expect(page.page()).toBe(1);
    expect(page.statusFilter()).toBe('Approved');
    expect(listRequests).toHaveBeenCalledWith({ page: 1, pageSize: 20, status: 'Approved' });
  });

  it('approve() opens dialog; reloads + toasts on success', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    dialogRef.afterClosed.mockReturnValue(of({ ...SAMPLE, status: 'Approved' }));
    listRequests.mockClear();
    await page.approve(SAMPLE);
    expect(dialog.open).toHaveBeenCalled();
    expect(toast.success).toHaveBeenCalledWith('experts.approve.toast');
    expect(listRequests).toHaveBeenCalled();
  });

  it('reject() opens dialog; reloads + toasts on success', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    dialogRef.afterClosed.mockReturnValue(of({ ...SAMPLE, status: 'Rejected' }));
    listRequests.mockClear();
    await page.reject(SAMPLE);
    expect(dialog.open).toHaveBeenCalled();
    expect(toast.success).toHaveBeenCalledWith('experts.reject.toast');
    expect(listRequests).toHaveBeenCalled();
  });

  it('approve()/reject() ignore null close (cancel)', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    dialogRef.afterClosed.mockReturnValue(of(null));
    listRequests.mockClear();
    await page.approve(SAMPLE);
    expect(toast.success).not.toHaveBeenCalled();
    expect(listRequests).not.toHaveBeenCalled();
  });
});

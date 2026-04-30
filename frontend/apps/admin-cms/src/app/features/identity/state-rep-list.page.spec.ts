import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { MatDialog } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ConfirmDialogService } from '../../core/ui/confirm-dialog.service';
import { ToastService } from '../../core/ui/toast.service';
import { IdentityApiService, type Result } from './identity-api.service';
import type { PagedResult, StateRepAssignment } from './identity.types';
import { StateRepListPage } from './state-rep-list.page';

const SAMPLE: StateRepAssignment = {
  id: 'a-id',
  userId: 'u1',
  userName: 'alice',
  countryId: 'c1',
  assignedOn: '2026-04-29T00:00:00Z',
  assignedById: 'admin',
  revokedOn: null,
  revokedById: null,
  isActive: true,
};

describe('StateRepListPage', () => {
  let fixture: ComponentFixture<StateRepListPage>;
  let page: StateRepListPage;
  let listStateRepAssignments: jest.Mock;
  let revokeStateRepAssignment: jest.Mock;
  let api: { listStateRepAssignments: jest.Mock; revokeStateRepAssignment: jest.Mock };
  let confirm: { confirm: jest.Mock };
  let toast: { success: jest.Mock; error: jest.Mock };
  let dialog: { open: jest.Mock };
  let dialogRef: { afterClosed: jest.Mock };

  function ok(value: PagedResult<StateRepAssignment>): Result<PagedResult<StateRepAssignment>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listStateRepAssignments = jest
      .fn()
      .mockResolvedValue(ok({ items: [SAMPLE], page: 1, pageSize: 20, total: 1 }));
    revokeStateRepAssignment = jest.fn().mockResolvedValue({ ok: true, value: undefined });
    api = { listStateRepAssignments, revokeStateRepAssignment };
    confirm = { confirm: jest.fn().mockResolvedValue(true) };
    toast = { success: jest.fn(), error: jest.fn() };
    dialogRef = { afterClosed: jest.fn().mockReturnValue(of(null)) };
    dialog = { open: jest.fn().mockReturnValue(dialogRef) };

    const authStub = {
      currentUser: () => null,
      isAuthenticated: () => false,
      hasPermission: () => true,
    };

    await TestBed.configureTestingModule({
      imports: [StateRepListPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: IdentityApiService, useValue: api },
        { provide: ConfirmDialogService, useValue: confirm },
        { provide: ToastService, useValue: toast },
        { provide: MatDialog, useValue: dialog },
        { provide: AuthService, useValue: authStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StateRepListPage);
    page = fixture.componentInstance;
  });

  it('loads with default activeOnly=true on init', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listStateRepAssignments).toHaveBeenCalledWith({ page: 1, pageSize: 20, active: true });
    expect(page.rows()).toEqual([SAMPLE]);
    expect(page.total()).toBe(1);
  });

  it('toggling activeOnly resets page to 1 and reloads with new flag', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.page.set(3);
    listStateRepAssignments.mockClear();
    page.onActiveToggle(false);
    await Promise.resolve();
    expect(page.activeOnly()).toBe(false);
    expect(page.page()).toBe(1);
    expect(listStateRepAssignments).toHaveBeenCalledWith({ page: 1, pageSize: 20, active: false });
  });

  it('onPage updates page (1-based) + pageSize and reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listStateRepAssignments.mockClear();
    page.onPage({ pageIndex: 2, pageSize: 50, length: 1, previousPageIndex: 0 });
    await Promise.resolve();
    expect(page.page()).toBe(3);
    expect(page.pageSize()).toBe(50);
    expect(listStateRepAssignments).toHaveBeenCalledWith({ page: 3, pageSize: 50, active: true });
  });

  it('openCreate opens dialog; on success reloads + toasts', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    dialogRef.afterClosed.mockReturnValue(of(SAMPLE));
    listStateRepAssignments.mockClear();
    await page.openCreate();
    expect(dialog.open).toHaveBeenCalled();
    expect(toast.success).toHaveBeenCalledWith('stateRep.create.toast');
    expect(listStateRepAssignments).toHaveBeenCalled();
  });

  it('openCreate ignores dialog cancel (afterClosed null)', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    dialogRef.afterClosed.mockReturnValue(of(null));
    listStateRepAssignments.mockClear();
    await page.openCreate();
    expect(toast.success).not.toHaveBeenCalled();
    expect(listStateRepAssignments).not.toHaveBeenCalled();
  });

  it('revoke confirms then deletes + reloads + toasts on success', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listStateRepAssignments.mockClear();
    await page.revoke(SAMPLE);
    expect(confirm.confirm).toHaveBeenCalledWith({
      titleKey: 'stateRep.revoke.title',
      messageKey: 'stateRep.revoke.message',
      confirmKey: 'stateRep.revoke.confirm',
      cancelKey: 'common.actions.cancel',
    });
    expect(revokeStateRepAssignment).toHaveBeenCalledWith(SAMPLE.id);
    expect(toast.success).toHaveBeenCalledWith('stateRep.revoke.toast');
    expect(listStateRepAssignments).toHaveBeenCalled();
  });

  it('revoke does nothing when confirm is cancelled', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    confirm.confirm.mockResolvedValueOnce(false);
    await page.revoke(SAMPLE);
    expect(revokeStateRepAssignment).not.toHaveBeenCalled();
    expect(toast.success).not.toHaveBeenCalled();
  });

  it('revoke surfaces api error via toast.error', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    revokeStateRepAssignment.mockResolvedValueOnce({ ok: false, error: { kind: 'forbidden' } });
    await page.revoke(SAMPLE);
    expect(toast.error).toHaveBeenCalledWith('errors.forbidden');
  });
});

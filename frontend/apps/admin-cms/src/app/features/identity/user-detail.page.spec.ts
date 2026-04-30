import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { MatDialog } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '@frontend/ui-kit';
import { IdentityApiService, type Result } from './identity-api.service';
import type { UserDetail } from './identity.types';
import { UserDetailPage } from './user-detail.page';

const SAMPLE: UserDetail = {
  id: 'u1',
  email: 'a@b.com',
  userName: 'alice',
  localePreference: 'en',
  knowledgeLevel: 'Beginner',
  interests: ['Sustainability'],
  countryId: null,
  avatarUrl: null,
  roles: ['SuperAdmin'],
  isActive: true,
};

describe('UserDetailPage', () => {
  let fixture: ComponentFixture<UserDetailPage>;
  let page: UserDetailPage;
  let getUser: jest.Mock;
  let api: { getUser: jest.Mock };
  let dialog: { open: jest.Mock };
  let toast: { success: jest.Mock; error: jest.Mock };
  let dialogRef: { afterClosed: jest.Mock };

  function ok(value: UserDetail): Result<UserDetail> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    getUser = jest.fn().mockResolvedValue(ok(SAMPLE));
    api = { getUser };
    dialogRef = { afterClosed: jest.fn().mockReturnValue(of(null)) };
    dialog = { open: jest.fn().mockReturnValue(dialogRef) };
    toast = { success: jest.fn(), error: jest.fn() };

    const authStub = {
      currentUser: () => null,
      isAuthenticated: () => false,
      hasPermission: () => true, // permission directive shows the button
    };

    await TestBed.configureTestingModule({
      imports: [UserDetailPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: IdentityApiService, useValue: api },
        { provide: MatDialog, useValue: dialog },
        { provide: ToastService, useValue: toast },
        { provide: AuthService, useValue: authStub },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'u1' } } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserDetailPage);
    page = fixture.componentInstance;
  });

  it('loads the user on init using the route param', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(getUser).toHaveBeenCalledWith('u1');
    expect(page.user()).toEqual(SAMPLE);
  });

  it('sets error.kind when api returns not-found', async () => {
    getUser.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.error()?.kind).toBe('not-found');
    expect(page.user()).toBeNull();
  });

  it('openRoleAssign opens MatDialog with userId + currentRoles', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.openRoleAssign();
    expect(dialog.open).toHaveBeenCalledTimes(1);
    const call = dialog.open.mock.calls[0];
    expect(call[1].data).toEqual({ userId: 'u1', currentRoles: ['SuperAdmin'] });
  });

  it('updates the user signal + toasts on dialog save', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const updated = { ...SAMPLE, roles: ['CommunityExpert'] };
    dialogRef.afterClosed.mockReturnValue(of(updated));
    await page.openRoleAssign();
    expect(page.user()).toEqual(updated);
    expect(toast.success).toHaveBeenCalledWith('roleAssign.saved');
  });

  it('does nothing on dialog cancel (afterClosed → null)', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    dialogRef.afterClosed.mockReturnValue(of(null));
    await page.openRoleAssign();
    expect(page.user()).toEqual(SAMPLE);
    expect(toast.success).not.toHaveBeenCalled();
  });
});

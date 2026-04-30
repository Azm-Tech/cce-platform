import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { IdentityApiService, type Result } from './identity-api.service';
import type { UserDetail } from './identity.types';
import { RoleAssignDialogComponent } from './role-assign.dialog';

const SAMPLE_USER: UserDetail = {
  id: 'u1',
  email: 'a@b.com',
  userName: 'a',
  localePreference: 'en',
  knowledgeLevel: 'Beginner',
  interests: [],
  countryId: null,
  avatarUrl: null,
  roles: ['SuperAdmin'],
  isActive: true,
};

describe('RoleAssignDialogComponent', () => {
  let dialog: RoleAssignDialogComponent;
  let assignRoles: jest.Mock;
  let dialogRef: { close: jest.Mock };
  let api: { assignRoles: jest.Mock };

  function configure(currentRoles: string[]) {
    dialogRef = { close: jest.fn() };
    assignRoles = jest.fn();
    api = { assignRoles };
    TestBed.configureTestingModule({
      imports: [RoleAssignDialogComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: IdentityApiService, useValue: api },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { userId: 'u1', currentRoles } },
      ],
    });
    const fixture = TestBed.createComponent(RoleAssignDialogComponent);
    dialog = fixture.componentInstance;
    fixture.detectChanges();
  }

  function ok(value: UserDetail): Result<UserDetail> {
    return { ok: true, value };
  }

  it('seeds selected from currentRoles', () => {
    configure(['SuperAdmin', 'ContentManager']);
    expect(dialog.selected()).toEqual(['SuperAdmin', 'ContentManager']);
  });

  it('save() calls api.assignRoles with userId + selected roles', async () => {
    configure(['SuperAdmin']);
    assignRoles.mockResolvedValueOnce(ok({ ...SAMPLE_USER, roles: ['SuperAdmin', 'ContentManager'] }));
    dialog.onSelectionChange(['SuperAdmin', 'ContentManager']);
    await dialog.save();
    expect(assignRoles).toHaveBeenCalledWith('u1', ['SuperAdmin', 'ContentManager']);
  });

  it('save() closes the dialog with the updated user on success', async () => {
    configure(['SuperAdmin']);
    const updated = { ...SAMPLE_USER, roles: ['CommunityExpert'] };
    assignRoles.mockResolvedValueOnce(ok(updated));
    await dialog.save();
    expect(dialogRef.close).toHaveBeenCalledWith(updated);
  });

  it('save() keeps the dialog open and surfaces errorKind on failure', async () => {
    configure(['SuperAdmin']);
    assignRoles.mockResolvedValueOnce({ ok: false, error: { kind: 'forbidden' } });
    await dialog.save();
    expect(dialog.errorKind()).toBe('forbidden');
    expect(dialogRef.close).not.toHaveBeenCalled();
    expect(dialog.saving()).toBe(false);
  });

  it('cancel() closes with null', () => {
    configure([]);
    dialog.cancel();
    expect(dialogRef.close).toHaveBeenCalledWith(null);
  });
});

import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { IdentityApiService, type Result } from './identity-api.service';
import type { StateRepAssignment } from './identity.types';
import { StateRepCreateDialogComponent } from './state-rep-create.dialog';

const VALID_USER = '11111111-1111-1111-1111-111111111111';
const VALID_COUNTRY = '22222222-2222-2222-2222-222222222222';

const SAMPLE: StateRepAssignment = {
  id: 'a-id',
  userId: VALID_USER,
  userName: 'alice',
  countryId: VALID_COUNTRY,
  assignedOn: '2026-04-29T00:00:00Z',
  assignedById: 'admin-id',
  revokedOn: null,
  revokedById: null,
  isActive: true,
};

describe('StateRepCreateDialogComponent', () => {
  let dialog: StateRepCreateDialogComponent;
  let createStateRepAssignment: jest.Mock;
  let dialogRef: { close: jest.Mock };
  let api: { createStateRepAssignment: jest.Mock };

  beforeEach(() => {
    createStateRepAssignment = jest.fn();
    api = { createStateRepAssignment };
    dialogRef = { close: jest.fn() };

    TestBed.configureTestingModule({
      imports: [StateRepCreateDialogComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: IdentityApiService, useValue: api },
        { provide: MatDialogRef, useValue: dialogRef },
      ],
    });
    const fixture = TestBed.createComponent(StateRepCreateDialogComponent);
    dialog = fixture.componentInstance;
    fixture.detectChanges();
  });

  function ok(value: StateRepAssignment): Result<StateRepAssignment> {
    return { ok: true, value };
  }

  it('marks the form invalid when GUIDs are missing or malformed', () => {
    expect(dialog.form.invalid).toBe(true);
    dialog.form.controls.userId.setValue('not-a-guid');
    dialog.form.controls.countryId.setValue('also-not');
    expect(dialog.form.invalid).toBe(true);
  });

  it('save() does not call api when form is invalid (and marks fields touched)', async () => {
    await dialog.save();
    expect(createStateRepAssignment).not.toHaveBeenCalled();
    expect(dialog.form.controls.userId.touched).toBe(true);
    expect(dialog.form.controls.countryId.touched).toBe(true);
  });

  it('save() calls api with form values and closes with result on success', async () => {
    dialog.form.controls.userId.setValue(VALID_USER);
    dialog.form.controls.countryId.setValue(VALID_COUNTRY);
    createStateRepAssignment.mockResolvedValueOnce(ok(SAMPLE));
    await dialog.save();
    expect(createStateRepAssignment).toHaveBeenCalledWith({
      userId: VALID_USER,
      countryId: VALID_COUNTRY,
    });
    expect(dialogRef.close).toHaveBeenCalledWith(SAMPLE);
  });

  it('save() keeps the dialog open and surfaces errorKind on duplicate', async () => {
    dialog.form.controls.userId.setValue(VALID_USER);
    dialog.form.controls.countryId.setValue(VALID_COUNTRY);
    createStateRepAssignment.mockResolvedValueOnce({ ok: false, error: { kind: 'duplicate' } });
    await dialog.save();
    expect(dialog.errorKind()).toBe('duplicate');
    expect(dialogRef.close).not.toHaveBeenCalled();
    expect(dialog.saving()).toBe(false);
  });

  it('cancel() closes with null', () => {
    dialog.cancel();
    expect(dialogRef.close).toHaveBeenCalledWith(null);
  });
});

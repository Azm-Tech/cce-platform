import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { ApproveExpertDialogComponent } from './approve-expert.dialog';
import { ExpertApiService } from './expert-api.service';
import type { ExpertRequest } from './expert.types';

const APPROVED: ExpertRequest = {
  id: 'r1',
  requestedById: 'u',
  requestedByUserName: 'a',
  requestedBioAr: '',
  requestedBioEn: '',
  requestedTags: [],
  submittedOn: '2026-04-29',
  status: 'Approved',
  processedById: 'admin',
  processedOn: '2026-04-29',
  rejectionReasonAr: null,
  rejectionReasonEn: null,
};

describe('ApproveExpertDialogComponent', () => {
  let dialog: ApproveExpertDialogComponent;
  let approve: jest.Mock;
  let dialogRef: { close: jest.Mock };

  beforeEach(() => {
    approve = jest.fn();
    dialogRef = { close: jest.fn() };
    TestBed.configureTestingModule({
      imports: [ApproveExpertDialogComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: ExpertApiService, useValue: { approve } },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { requestId: 'r1', requesterName: 'alice' } },
      ],
    });
    const fixture = TestBed.createComponent(ApproveExpertDialogComponent);
    dialog = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('form invalid when titles missing', () => {
    expect(dialog.form.invalid).toBe(true);
  });

  it('save() does nothing on invalid form', async () => {
    await dialog.save();
    expect(approve).not.toHaveBeenCalled();
  });

  it('save() POSTs and closes with the updated request', async () => {
    dialog.form.patchValue({ academicTitleAr: 'دكتور', academicTitleEn: 'Dr.' });
    approve.mockResolvedValueOnce({ ok: true, value: APPROVED });
    await dialog.save();
    expect(approve).toHaveBeenCalledWith('r1', { academicTitleAr: 'دكتور', academicTitleEn: 'Dr.' });
    expect(dialogRef.close).toHaveBeenCalledWith(APPROVED);
  });

  it('save() surfaces errorKind on failure', async () => {
    dialog.form.patchValue({ academicTitleAr: 'دكتور', academicTitleEn: 'Dr.' });
    approve.mockResolvedValueOnce({ ok: false, error: { kind: 'forbidden' } });
    await dialog.save();
    expect(dialog.errorKind()).toBe('forbidden');
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('cancel() closes with null', () => {
    dialog.cancel();
    expect(dialogRef.close).toHaveBeenCalledWith(null);
  });
});

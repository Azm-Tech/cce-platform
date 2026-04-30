import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { ExpertApiService } from './expert-api.service';
import type { ExpertRequest } from './expert.types';
import { RejectExpertDialogComponent } from './reject-expert.dialog';

const REJECTED: ExpertRequest = {
  id: 'r1',
  requestedById: 'u',
  requestedByUserName: 'a',
  requestedBioAr: '',
  requestedBioEn: '',
  requestedTags: [],
  submittedOn: '2026-04-29',
  status: 'Rejected',
  processedById: 'admin',
  processedOn: '2026-04-29',
  rejectionReasonAr: 'سبب',
  rejectionReasonEn: 'reason',
};

describe('RejectExpertDialogComponent', () => {
  let dialog: RejectExpertDialogComponent;
  let reject: jest.Mock;
  let dialogRef: { close: jest.Mock };

  beforeEach(() => {
    reject = jest.fn();
    dialogRef = { close: jest.fn() };
    TestBed.configureTestingModule({
      imports: [RejectExpertDialogComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: ExpertApiService, useValue: { reject } },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { requestId: 'r1', requesterName: 'alice' } },
      ],
    });
    const fixture = TestBed.createComponent(RejectExpertDialogComponent);
    dialog = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('form invalid when reasons missing', () => {
    expect(dialog.form.invalid).toBe(true);
  });

  it('save() POSTs and closes with the updated request', async () => {
    dialog.form.patchValue({ rejectionReasonAr: 'سبب', rejectionReasonEn: 'reason' });
    reject.mockResolvedValueOnce({ ok: true, value: REJECTED });
    await dialog.save();
    expect(reject).toHaveBeenCalledWith('r1', { rejectionReasonAr: 'سبب', rejectionReasonEn: 'reason' });
    expect(dialogRef.close).toHaveBeenCalledWith(REJECTED);
  });

  it('cancel() closes with null', () => {
    dialog.cancel();
    expect(dialogRef.close).toHaveBeenCalledWith(null);
  });
});

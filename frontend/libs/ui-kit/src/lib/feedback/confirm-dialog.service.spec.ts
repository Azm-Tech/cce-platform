import { TestBed } from '@angular/core/testing';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { of } from 'rxjs';
import { ConfirmDialogService } from './confirm-dialog.service';
import { ConfirmDialogComponent } from './confirm-dialog.component';

describe('ConfirmDialogService', () => {
  let sut: ConfirmDialogService;
  let dialog: { open: jest.Mock };

  beforeEach(() => {
    dialog = { open: jest.fn() };
    TestBed.configureTestingModule({
      providers: [
        ConfirmDialogService,
        { provide: MatDialog, useValue: dialog },
      ],
    });
    sut = TestBed.inject(ConfirmDialogService);
  });

  it('opens ConfirmDialogComponent with provided data', async () => {
    const fakeRef = { afterClosed: () => of(true) } as Partial<MatDialogRef<ConfirmDialogComponent>>;
    dialog.open.mockReturnValue(fakeRef);

    const result = await sut.confirm({ titleKey: 't', messageKey: 'm' });

    expect(dialog.open).toHaveBeenCalledWith(
      ConfirmDialogComponent,
      expect.objectContaining({ data: { titleKey: 't', messageKey: 'm' }, width: '480px' }),
    );
    expect(result).toBe(true);
  });

  it('returns false when dialog is dismissed without confirm', async () => {
    const fakeRef = { afterClosed: () => of(undefined) } as Partial<MatDialogRef<ConfirmDialogComponent>>;
    dialog.open.mockReturnValue(fakeRef);

    const result = await sut.confirm({ titleKey: 't', messageKey: 'm' });

    expect(result).toBe(false);
  });
});

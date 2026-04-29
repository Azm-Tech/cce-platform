import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let sut: ToastService;
  let snack: { open: jest.Mock };
  let translate: { instant: jest.Mock };

  beforeEach(() => {
    snack = { open: jest.fn() };
    translate = { instant: jest.fn().mockImplementation((key: string) => `T:${key}`) };
    TestBed.configureTestingModule({
      providers: [
        ToastService,
        { provide: MatSnackBar, useValue: snack },
        { provide: TranslateService, useValue: translate },
      ],
    });
    sut = TestBed.inject(ToastService);
  });

  it('opens a snackbar with translated message and success panel class on success', () => {
    sut.success('common.save');
    expect(translate.instant).toHaveBeenCalledWith('common.save', undefined);
    expect(snack.open).toHaveBeenCalledWith('T:common.save', undefined, expect.objectContaining({ panelClass: 'cce-toast-success', duration: 4000 }));
  });

  it('opens a snackbar with translated message and error panel class on error', () => {
    sut.error('errors.server');
    expect(snack.open).toHaveBeenCalledWith('T:errors.server', undefined, expect.objectContaining({ panelClass: 'cce-toast-error' }));
  });

  it('forwards interpolation params', () => {
    sut.success('common.savedNamed', { name: 'Alice' });
    expect(translate.instant).toHaveBeenCalledWith('common.savedNamed', { name: 'Alice' });
  });
});

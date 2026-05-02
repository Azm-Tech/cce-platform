import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { ConfirmDialogComponent, type ConfirmDialogData } from './confirm-dialog.component';

describe('ConfirmDialogComponent', () => {
  let fixture: ComponentFixture<ConfirmDialogComponent>;
  let dialogRef: { close: jest.Mock };

  function setUp(data: ConfirmDialogData): void {
    dialogRef = { close: jest.fn() };
    TestBed.configureTestingModule({
      imports: [ConfirmDialogComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    });
    fixture = TestBed.createComponent(ConfirmDialogComponent);
    fixture.detectChanges();
  }

  it('confirm closes with true', () => {
    setUp({
      titleKey: 't', bodyKey: 'b', confirmKey: 'OK', cancelKey: 'Cancel',
    });
    fixture.componentInstance.confirm();
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('cancel closes with false', () => {
    setUp({
      titleKey: 't', bodyKey: 'b', confirmKey: 'OK', cancelKey: 'Cancel',
    });
    fixture.componentInstance.cancel();
    expect(dialogRef.close).toHaveBeenCalledWith(false);
  });

  it('renders translated title + body keys', () => {
    setUp({
      titleKey: 'interactiveCity.saved.confirmDeleteTitle',
      bodyKey: 'interactiveCity.saved.confirmDeleteBody',
      confirmKey: 'interactiveCity.saved.confirmDelete',
      cancelKey: 'interactiveCity.saved.confirmCancel',
    });
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('interactiveCity.saved.confirmDeleteTitle');
    expect(html).toContain('interactiveCity.saved.confirmDeleteBody');
  });

  it('passes the dangerous flag through (component reads it)', () => {
    setUp({
      titleKey: 't', bodyKey: 'b', confirmKey: 'OK', cancelKey: 'Cancel',
      dangerous: true,
    });
    expect(fixture.componentInstance.data.dangerous).toBe(true);
  });
});

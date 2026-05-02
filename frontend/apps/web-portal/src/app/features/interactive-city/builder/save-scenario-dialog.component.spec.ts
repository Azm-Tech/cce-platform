import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import {
  SaveScenarioDialogComponent,
  type SaveScenarioDialogData,
  type SaveScenarioDialogResult,
} from './save-scenario-dialog.component';

describe('SaveScenarioDialogComponent', () => {
  let fixture: ComponentFixture<SaveScenarioDialogComponent>;
  let dialogRef: { close: jest.Mock };

  function setUp(initialName: string): void {
    dialogRef = { close: jest.fn() };
    const data: SaveScenarioDialogData = { initialName };
    TestBed.configureTestingModule({
      imports: [SaveScenarioDialogComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    });
    fixture = TestBed.createComponent(SaveScenarioDialogComponent);
    fixture.detectChanges();
  }

  it('pre-fills the name input from the passed-in data', () => {
    setUp('From Store');
    expect(fixture.componentInstance.nameControl.value).toBe('From Store');
  });

  it('submit closes the dialog with the trimmed name', () => {
    setUp('');
    fixture.componentInstance.nameControl.setValue('  My Scenario  ');
    fixture.componentInstance.submit();
    const result = dialogRef.close.mock.calls[0][0] as SaveScenarioDialogResult;
    expect(result).toEqual({ name: 'My Scenario' });
  });

  it('cancel closes with null name', () => {
    setUp('something');
    fixture.componentInstance.cancel();
    const result = dialogRef.close.mock.calls[0][0] as SaveScenarioDialogResult;
    expect(result).toEqual({ name: null });
  });

  it('submit is a no-op when the name is empty (after trim)', () => {
    setUp('');
    fixture.componentInstance.nameControl.setValue('   ');
    fixture.componentInstance.submit();
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('disables the submit button when the form is invalid', () => {
    setUp('');
    fixture.detectChanges();
    const submitButton = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((b) => b.textContent?.includes('interactiveCity.totals.save'));
    expect(submitButton?.disabled).toBe(true);
  });
});

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';

export interface SaveScenarioDialogData {
  initialName: string;
}

export interface SaveScenarioDialogResult {
  /** Trimmed name string. Null on cancel. */
  name: string | null;
}

/**
 * Single-input dialog to confirm/edit the scenario name before saving.
 * The TotalsBar opens it on Save (when authenticated). Returns the
 * trimmed name or null on cancel.
 */
@Component({
  selector: 'cce-save-scenario-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    TranslateModule,
  ],
  templateUrl: './save-scenario-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SaveScenarioDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<SaveScenarioDialogComponent, SaveScenarioDialogResult>);
  readonly data = inject<SaveScenarioDialogData>(MAT_DIALOG_DATA);

  readonly nameControl = new FormControl<string>(this.data.initialName ?? '', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(200)],
  });

  submit(): void {
    const trimmed = (this.nameControl.value ?? '').trim();
    if (trimmed === '') return;
    this.dialogRef.close({ name: trimmed });
  }

  cancel(): void {
    this.dialogRef.close({ name: null });
  }
}

import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { AccountApiService } from './account-api.service';
import type { EvaluationPayload } from './account.types';

@Component({
  selector: 'cce-evaluation-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    TranslocoModule,
  ],
  templateUrl: './evaluation.dialog.html',
  styleUrl: './evaluation.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EvaluationDialogComponent {
  private readonly api = inject(AccountApiService);
  private readonly ref = inject(MatDialogRef<EvaluationDialogComponent, boolean>);

  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly submitted = signal(false);

  readonly ratings = [5, 4, 3, 2, 1] as const;

  readonly form = new FormGroup({
    overallSatisfaction: new FormControl<number | null>(null, Validators.required),
    easeOfUse: new FormControl<number | null>(null, Validators.required),
    contentSuitability: new FormControl<number | null>(null, Validators.required),
    feedback: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] }),
  });

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const payload: EvaluationPayload = {
      overallSatisfaction: v.overallSatisfaction ?? undefined,
      easeOfUse: v.easeOfUse ?? undefined,
      contentSuitability: v.contentSuitability ?? undefined,
      feedback: v.feedback || null,
    };
    this.saving.set(true);
    this.errorKind.set(null);
    const res = await this.api.submitEvaluation(payload);
    this.saving.set(false);
    if (res.ok) {
      this.submitted.set(true);
      localStorage.setItem('cce_evaluated', '1');
      setTimeout(() => this.ref.close(true), 1800);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }
}

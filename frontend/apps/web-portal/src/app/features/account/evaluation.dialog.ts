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
  readonly fieldErrors = signal<Record<string, string>>({});
  readonly submitted = signal(false);

  /** 1 = Excellent (best) … 5 = Poor (worst) — matches BRD §6.3.6 */
  readonly ratingOptions = [
    { value: 1, labelKey: 'evaluation.scale1' },
    { value: 2, labelKey: 'evaluation.scale2' },
    { value: 3, labelKey: 'evaluation.scale3' },
    { value: 4, labelKey: 'evaluation.scale4' },
    { value: 5, labelKey: 'evaluation.scale5' },
  ] as const;

  readonly form = new FormGroup({
    overallSatisfaction:      new FormControl<number | null>(null, Validators.required),
    easeOfUse:                new FormControl<number | null>(null, Validators.required),
    contentSuitability:       new FormControl<number | null>(null, Validators.required),
    personalizedSuggestions:  new FormControl<number | null>(null, Validators.required),
    feedback: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] }),
  });

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const payload: EvaluationPayload = {
      overallSatisfaction:     v.overallSatisfaction     ?? undefined,
      easeOfUse:               v.easeOfUse               ?? undefined,
      contentSuitability:      v.contentSuitability      ?? undefined,
      personalizedSuggestions: v.personalizedSuggestions ?? undefined,
      feedback: v.feedback || null,
    };
    this.saving.set(true);
    this.errorKind.set(null);
    this.fieldErrors.set({});
    const res = await this.api.submitEvaluation(payload);
    this.saving.set(false);
    if (res.ok) {
      this.submitted.set(true);
      localStorage.setItem('cce_evaluated', '1');
      setTimeout(() => this.ref.close(true), 1800);
    } else if (res.error.kind === 'validation' && Object.keys(res.error.fieldErrors).length > 0) {
      const flat: Record<string, string> = {};
      for (const [field, msgs] of Object.entries(res.error.fieldErrors)) {
        flat[field] = msgs[0];
        const ctrl = this.form.get(field);
        if (ctrl) {
          ctrl.setErrors({ server: true });
          ctrl.markAsTouched();
        }
      }
      this.fieldErrors.set(flat);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }
}

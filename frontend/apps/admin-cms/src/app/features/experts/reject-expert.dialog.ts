import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { ExpertApiService } from './expert-api.service';
import type { ExpertRequest } from './expert.types';

export interface RejectExpertDialogData {
  requestId: string;
  requesterName: string | null;
}

interface RejectForm {
  rejectionReasonAr: FormControl<string>;
  rejectionReasonEn: FormControl<string>;
}

@Component({
  selector: 'cce-reject-expert-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    TranslateModule,
  ],
  templateUrl: './reject-expert.dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RejectExpertDialogComponent {
  private readonly api = inject(ExpertApiService);

  readonly form = new FormGroup<RejectForm>({
    rejectionReasonAr: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    rejectionReasonEn: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);

  constructor(
    private readonly ref: MatDialogRef<RejectExpertDialogComponent, ExpertRequest | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: RejectExpertDialogData,
  ) {}

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorKind.set(null);
    const res = await this.api.reject(this.data.requestId, this.form.getRawValue());
    this.saving.set(false);
    if (res.ok) this.ref.close(res.value);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void {
    this.ref.close(null);
  }
}

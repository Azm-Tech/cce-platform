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

export interface ApproveExpertDialogData {
  requestId: string;
  requesterName: string | null;
}

interface ApproveForm {
  academicTitleAr: FormControl<string>;
  academicTitleEn: FormControl<string>;
}

@Component({
  selector: 'cce-approve-expert-dialog',
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
  templateUrl: './approve-expert.dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApproveExpertDialogComponent {
  private readonly api = inject(ExpertApiService);

  readonly form = new FormGroup<ApproveForm>({
    academicTitleAr: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    academicTitleEn: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);

  constructor(
    private readonly ref: MatDialogRef<ApproveExpertDialogComponent, ExpertRequest | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: ApproveExpertDialogData,
  ) {}

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorKind.set(null);
    const res = await this.api.approve(this.data.requestId, this.form.getRawValue());
    this.saving.set(false);
    if (res.ok) this.ref.close(res.value);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void {
    this.ref.close(null);
  }
}

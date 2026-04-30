import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';
import { PublishingApiService } from './publishing-api.service';
import type { Event as CceEvent } from './publishing.types';

export interface RescheduleEventDialogData {
  event: CceEvent;
}

@Component({
  selector: 'cce-reschedule-event-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, TranslateModule,
  ],
  templateUrl: './reschedule-event.dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RescheduleEventDialogComponent {
  private readonly api = inject(PublishingApiService);
  readonly form: FormGroup<{ startsOn: FormControl<string>; endsOn: FormControl<string> }>;
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);

  constructor(
    private readonly ref: MatDialogRef<RescheduleEventDialogComponent, CceEvent | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: RescheduleEventDialogData,
  ) {
    this.form = new FormGroup({
      startsOn: new FormControl(data.event.startsOn, { nonNullable: true, validators: [Validators.required] }),
      endsOn: new FormControl(data.event.endsOn, { nonNullable: true, validators: [Validators.required] }),
    });
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorKind.set(null);
    const v = this.form.getRawValue();
    const res = await this.api.rescheduleEvent(this.data.event.id, {
      startsOn: v.startsOn,
      endsOn: v.endsOn,
      rowVersion: this.data.event.rowVersion,
    });
    this.saving.set(false);
    if (res.ok) this.ref.close(res.value);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void { this.ref.close(null); }
}

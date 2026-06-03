
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { PublishingApiService } from './publishing-api.service';
import type { Event as CceEvent } from './publishing.types';

export interface RescheduleEventDialogData {
  event: CceEvent;
}

/** Split an ISO datetime into a Date (date-only) plus HH / MM strings,
 *  snapping minutes to the nearest 15-minute bucket. Mirrors the helper
 *  used by event-form.dialog. */
function splitDateTime(iso: string | null | undefined): { date: Date | null; hour: string; minute: string } {
  if (!iso) return { date: null, hour: '', minute: '' };
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return { date: null, hour: '', minute: '' };
  const pad = (n: number) => String(n).padStart(2, '0');
  const snappedMinute = Math.round(d.getMinutes() / 15) * 15 % 60;
  return {
    date: new Date(d.getFullYear(), d.getMonth(), d.getDate()),
    hour: pad(d.getHours()),
    minute: pad(snappedMinute),
  };
}

function combineDateTime(date: Date | null, hour: string, minute: string): string {
  if (!date) return '';
  const local = new Date(date);
  local.setHours(Number(hour || 0), Number(minute || 0), 0, 0);
  if (Number.isNaN(local.getTime())) return '';
  return local.toISOString();
}

const HOUR_OPTIONS = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, '0'));
const MINUTE_OPTIONS = ['00', '15', '30', '45'];

interface RescheduleForm {
  startDate: FormControl<Date | null>;
  startHour: FormControl<string>;
  startMinute: FormControl<string>;
  endDate: FormControl<Date | null>;
  endHour: FormControl<string>;
  endMinute: FormControl<string>;
}

@Component({
  selector: 'cce-reschedule-event-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    TranslocoModule
],
  providers: [provideNativeDateAdapter()],
  templateUrl: './reschedule-event.dialog.html',
  styles: [`
    .cce-reschedule { display: flex; flex-direction: column; gap: 0.5rem; }
    .cce-reschedule__datetime-row {
      display: grid;
      grid-template-columns: 2fr 1fr 1fr;
      gap: 1rem;
    }
    @media (max-width: 540px) {
      .cce-reschedule__datetime-row { grid-template-columns: 1fr 1fr; }
      .cce-reschedule__date { grid-column: 1 / -1; }
    }
    .cce-reschedule__error {
      background: #fdecea; color: #b00020; padding: 0.6rem 0.85rem;
      border-radius: 6px; margin-top: 0.5rem; font-size: 0.85rem;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RescheduleEventDialogComponent {
  private readonly api = inject(PublishingApiService);
  readonly form: FormGroup<RescheduleForm>;
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly hourOptions = HOUR_OPTIONS;
  readonly minuteOptions = MINUTE_OPTIONS;

  constructor(
    private readonly ref: MatDialogRef<RescheduleEventDialogComponent, CceEvent | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: RescheduleEventDialogData,
  ) {
    const s = splitDateTime(data.event.startsOn);
    const e = splitDateTime(data.event.endsOn);
    this.form = new FormGroup<RescheduleForm>({
      startDate: new FormControl<Date | null>(s.date, { validators: [Validators.required] }),
      startHour: new FormControl(s.hour, { nonNullable: true, validators: [Validators.required] }),
      startMinute: new FormControl(s.minute, { nonNullable: true, validators: [Validators.required] }),
      endDate: new FormControl<Date | null>(e.date, { validators: [Validators.required] }),
      endHour: new FormControl(e.hour, { nonNullable: true, validators: [Validators.required] }),
      endMinute: new FormControl(e.minute, { nonNullable: true, validators: [Validators.required] }),
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
    const startsOn = combineDateTime(v.startDate, v.startHour, v.startMinute);
    const endsOn = combineDateTime(v.endDate, v.endHour, v.endMinute);
    const res = await this.api.rescheduleEvent(this.data.event.id, {
      startsOn,
      endsOn,
      rowVersion: this.data.event.rowVersion,
    });
    this.saving.set(false);
    if (res.ok) this.ref.close(res.value);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void { this.ref.close(null); }
}

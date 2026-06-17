
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { RichTextEditorComponent, ToastService } from '@frontend/ui-kit';
import { ContentApiService } from '../content/content-api.service';
import { PublishingApiService } from './publishing-api.service';
import type { Event, Topic } from './publishing.types';

const ALLOWED_IMAGE_MIME = ['image/png', 'image/jpeg', 'image/webp'];
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const TITLE_MAX = 255;
const DESC_MAX = 2000;
const LOCATION_MAX = 255;
const URL_PATTERN = /^https?:\/\/.+/i;

/** Split an ISO datetime string into a Date (date-only) and HH / MM
 *  parts for the form. Minutes snap to the nearest 15-minute bucket so
 *  they always match an available <mat-select> option. */
function splitDateTime(iso: string | null | undefined): { date: Date | null; hour: string; minute: string } {
  if (!iso) return { date: null, hour: '', minute: '' };
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return { date: null, hour: '', minute: '' };
  const pad = (n: number) => String(n).padStart(2, '0');
  const dateOnly = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  const snappedMinute = Math.round(d.getMinutes() / 15) * 15 % 60;
  return {
    date: dateOnly,
    hour: pad(d.getHours()),
    minute: pad(snappedMinute),
  };
}

/** Combine a Date (date-only) plus HH and MM strings into an ISO 8601
 *  UTC string. */
function combineDateTime(date: Date | null, hour: string, minute: string): string {
  if (!date) return '';
  const local = new Date(date);
  local.setHours(Number(hour || 0), Number(minute || 0), 0, 0);
  if (Number.isNaN(local.getTime())) return '';
  return local.toISOString();
}

const HOUR_OPTIONS = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, '0'));
const MINUTE_OPTIONS = ['00', '15', '30', '45'];

/** Cross-field validator: at least one of locationAr / locationEn /
 *  onlineMeetingUrl must be non-empty. */
function venueRequiredValidator(group: AbstractControl): ValidationErrors | null {
  const v = group.value as { locationAr?: string; locationEn?: string; onlineMeetingUrl?: string };
  const hasVenue =
    (v?.locationAr ?? '').trim().length > 0 ||
    (v?.locationEn ?? '').trim().length > 0 ||
    (v?.onlineMeetingUrl ?? '').trim().length > 0;
  return hasVenue ? null : { venueRequired: true };
}

export interface EventFormDialogData {
  event?: Event;
  /** When 'view', the form is rendered read-only and the Save action is hidden. */
  mode?: 'create' | 'edit' | 'view';
}

interface EventForm {
  titleAr: FormControl<string>;
  titleEn: FormControl<string>;
  descriptionAr: FormControl<string>;
  descriptionEn: FormControl<string>;
  startDate: FormControl<Date | null>;
  startHour: FormControl<string>;
  startMinute: FormControl<string>;
  endDate: FormControl<Date | null>;
  endHour: FormControl<string>;
  endMinute: FormControl<string>;
  locationAr: FormControl<string>;
  locationEn: FormControl<string>;
  onlineMeetingUrl: FormControl<string>;
  featuredImageUrl: FormControl<string>;
  topicId: FormControl<string>;
}

@Component({
  selector: 'cce-event-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    RichTextEditorComponent,
    TranslocoModule
],
  providers: [provideNativeDateAdapter()],
  templateUrl: './event-form.dialog.html',
  styles: [`
    .cce-event-form { display: flex; flex-direction: column; gap: 0.5rem; }
    .cce-event-form__row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .cce-event-form__field--full { width: 100%; }
    .cce-event-form__datetime-row {
      display: grid;
      grid-template-columns: 2fr 1fr 1fr;
      gap: 1rem;
    }
    @media (max-width: 540px) {
      .cce-event-form__datetime-row { grid-template-columns: 1fr 1fr; }
      .cce-event-form__date { grid-column: 1 / -1; }
    }
    .cce-event-form__image { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.5rem; }
    .cce-event-form__image-hint { font-size: 0.78rem; color: rgba(0,0,0,0.55); }
    .cce-event-form__preview { margin: 0 0 0.5rem; }
    .cce-event-form__preview img {
      max-width: 220px; max-height: 140px; border-radius: 8px;
      border: 1px solid rgba(0,0,0,0.08); object-fit: cover;
    }
    .cce-event-form__error {
      background: var(--danger--50); color: var(--danger--600); padding: 0.6rem 0.85rem;
      border-radius: 6px; margin-top: 0.5rem; font-size: 0.85rem;
    }
    @media (max-width: 600px) {
      .cce-event-form__row { grid-template-columns: 1fr; }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventFormDialogComponent {
  private readonly api = inject(PublishingApiService);
  private readonly assets = inject(ContentApiService);

  /** Inline-image uploader for the rich-text editor (asset media store). */
  readonly uploadImage = async (file: File): Promise<string | null> => {
    const res = await this.assets.uploadMedia(file);
    return res.ok ? res.value.url : null;
  };
  private readonly toast = inject(ToastService);
  private readonly localeService = inject(LocaleService);
  readonly form: FormGroup<EventForm>;
  readonly saving = signal(false);
  readonly uploadingImage = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly missingRequired = signal(false);
  readonly topics = signal<Topic[]>([]);
  readonly locale = this.localeService.locale;
  readonly isEdit: boolean;
  readonly isView: boolean;
  readonly titleMax = TITLE_MAX;
  readonly descMax = DESC_MAX;
  readonly locationMax = LOCATION_MAX;
  readonly hourOptions = HOUR_OPTIONS;
  readonly minuteOptions = MINUTE_OPTIONS;

  constructor(
    private readonly ref: MatDialogRef<EventFormDialogComponent, Event | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: EventFormDialogData,
  ) {
    this.isView = data.mode === 'view';
    this.isEdit = !this.isView && data.event !== undefined;
    const e = data.event;
    this.form = new FormGroup<EventForm>({
      titleAr: new FormControl(e?.titleAr ?? '', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(TITLE_MAX)],
      }),
      titleEn: new FormControl(e?.titleEn ?? '', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(TITLE_MAX)],
      }),
      descriptionAr: new FormControl(e?.descriptionAr ?? '', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(DESC_MAX)],
      }),
      descriptionEn: new FormControl(e?.descriptionEn ?? '', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(DESC_MAX)],
      }),
      startDate: new FormControl<Date | null>(splitDateTime(e?.startsOn).date, {
        validators: [Validators.required],
      }),
      startHour: new FormControl(splitDateTime(e?.startsOn).hour, {
        nonNullable: true,
        validators: [Validators.required],
      }),
      startMinute: new FormControl(splitDateTime(e?.startsOn).minute, {
        nonNullable: true,
        validators: [Validators.required],
      }),
      endDate: new FormControl<Date | null>(splitDateTime(e?.endsOn).date, {
        validators: [Validators.required],
      }),
      endHour: new FormControl(splitDateTime(e?.endsOn).hour, {
        nonNullable: true,
        validators: [Validators.required],
      }),
      endMinute: new FormControl(splitDateTime(e?.endsOn).minute, {
        nonNullable: true,
        validators: [Validators.required],
      }),
      locationAr: new FormControl(e?.locationAr ?? '', {
        nonNullable: true,
        validators: [Validators.maxLength(LOCATION_MAX)],
      }),
      locationEn: new FormControl(e?.locationEn ?? '', {
        nonNullable: true,
        validators: [Validators.maxLength(LOCATION_MAX)],
      }),
      onlineMeetingUrl: new FormControl(e?.onlineMeetingUrl ?? '', {
        nonNullable: true,
        validators: [Validators.maxLength(LOCATION_MAX), Validators.pattern(URL_PATTERN)],
      }),
      featuredImageUrl: new FormControl(e?.featuredImageUrl ?? '', { nonNullable: true }),
      topicId: new FormControl(e?.topicId ?? '', {
        nonNullable: true,
        validators: [Validators.required],
      }),
    }, { validators: venueRequiredValidator });
    if (this.isView) this.form.disable();
    void this.loadTopics();
  }

  private async loadTopics(): Promise<void> {
    const res = await this.api.listTopics({ onlyActive: true });
    if (res.ok) this.topics.set(res.value);
  }

  async onImagePicked(event: globalThis.Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    if (!ALLOWED_IMAGE_MIME.includes(file.type) || file.size > MAX_IMAGE_BYTES) {
      this.toast.error('news.field.imageInvalidType');
      return;
    }
    this.uploadingImage.set(true);
    const res = await this.assets.uploadMedia(file);
    this.uploadingImage.set(false);
    if (res.ok) {
      this.form.controls.featuredImageUrl.setValue(res.value.url);
    } else {
      this.toast.error('errors.ERR027');
    }
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.missingRequired.set(true);
      return;
    }
    this.missingRequired.set(false);
    this.saving.set(true);
    this.errorKind.set(null);
    const v = this.form.getRawValue();
    const nullify = (s: string) => s || null;
    const startsOn = combineDateTime(v.startDate, v.startHour, v.startMinute);
    const endsOn = combineDateTime(v.endDate, v.endHour, v.endMinute);

    if (this.isEdit && this.data.event) {
      const res = await this.api.updateEvent(this.data.event.id, {
        titleAr: v.titleAr,
        titleEn: v.titleEn,
        descriptionAr: v.descriptionAr,
        descriptionEn: v.descriptionEn,
        locationAr: nullify(v.locationAr),
        locationEn: nullify(v.locationEn),
        onlineMeetingUrl: nullify(v.onlineMeetingUrl),
        featuredImageUrl: nullify(v.featuredImageUrl),
        topicId: nullify(v.topicId),
        rowVersion: this.data.event.rowVersion,
      });
      this.saving.set(false);
      if (res.ok) this.ref.close(res.value);
      else { this.errorKind.set(res.error.kind); this.toast.error('errors.ERR027'); }
    } else {
      const res = await this.api.createEvent({
        titleAr: v.titleAr,
        titleEn: v.titleEn,
        descriptionAr: v.descriptionAr,
        descriptionEn: v.descriptionEn,
        startsOn,
        endsOn,
        locationAr: nullify(v.locationAr),
        locationEn: nullify(v.locationEn),
        onlineMeetingUrl: nullify(v.onlineMeetingUrl),
        featuredImageUrl: nullify(v.featuredImageUrl),
        topicId: nullify(v.topicId),
      });
      this.saving.set(false);
      if (res.ok) this.ref.close(res.value);
      else { this.errorKind.set(res.error.kind); this.toast.error('errors.ERR027'); }
    }
  }

  cancel(): void { this.ref.close(null); }
}


import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { ContentApiService } from '../content/content-api.service';
import { PublishingApiService } from './publishing-api.service';
import type { Event, Topic } from './publishing.types';

const ALLOWED_IMAGE_MIME = ['image/png', 'image/jpeg', 'image/webp'];
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const TITLE_MAX = 255;
const DESC_MAX = 2000;
const LOCATION_MAX = 255;
const URL_PATTERN = /^https?:\/\/.+/i;

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
  startsOn: FormControl<string>;
  endsOn: FormControl<string>;
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
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    TranslocoModule
],
  templateUrl: './event-form.dialog.html',
  styles: [`
    .cce-event-form { display: flex; flex-direction: column; gap: 0.5rem; }
    .cce-event-form__row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .cce-event-form__image { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.5rem; }
    .cce-event-form__image-hint { font-size: 0.78rem; color: rgba(0,0,0,0.55); }
    .cce-event-form__preview { margin: 0 0 0.5rem; }
    .cce-event-form__preview img {
      max-width: 220px; max-height: 140px; border-radius: 8px;
      border: 1px solid rgba(0,0,0,0.08); object-fit: cover;
    }
    .cce-event-form__error {
      background: #fdecea; color: #b00020; padding: 0.6rem 0.85rem;
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
      startsOn: new FormControl(e?.startsOn ?? '', { nonNullable: true, validators: [Validators.required] }),
      endsOn: new FormControl(e?.endsOn ?? '', { nonNullable: true, validators: [Validators.required] }),
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
    });
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
        startsOn: v.startsOn,
        endsOn: v.endsOn,
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


import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { RichTextEditorComponent, ToastService } from '@frontend/ui-kit';
import { CommunityApiService } from '../community/community-api.service';
import type { PublicTopic } from '../community/community.types';
import { MediaApiService } from '../../core/media/media-api.service';
import { CountriesApiService } from './countries-api.service';
import { ContentType, type CountryContentRequest } from './country.types';

const ALLOWED_IMAGE_MIME = ['image/png', 'image/jpeg', 'image/webp'];
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;

const HOUR_OPTIONS = Array.from({ length: 24 }, (_, i) => ({ value: String(i).padStart(2, '0'), label: String(i).padStart(2, '0') }));
const MINUTE_OPTIONS = ['00', '15', '30', '45'].map(m => ({ value: m, label: m }));

function combineDateTime(date: Date | null, hour: string, minute: string): string {
  if (!date) return '';
  const local = new Date(date);
  local.setHours(Number(hour || 0), Number(minute || 0), 0, 0);
  if (Number.isNaN(local.getTime())) return '';
  return local.toISOString();
}

function locationOrUrlRequired(group: AbstractControl): ValidationErrors | null {
  const { locationAr, locationEn, onlineMeetingUrl } = group.value;
  return locationAr || locationEn || onlineMeetingUrl ? null : { locationRequired: true };
}

interface EventRequestForm {
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
  topicId: FormControl<string>;
}

@Component({
  selector: 'cce-event-request-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    RichTextEditorComponent,
    TranslocoModule,
  ],
  providers: [provideNativeDateAdapter()],
  templateUrl: './event-request-form.dialog.html',
  styleUrl: './event-request-form.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventRequestFormDialogComponent {
  private readonly api = inject(CountriesApiService);
  private readonly communityApi = inject(CommunityApiService);
  private readonly media = inject(MediaApiService);
  private readonly toast = inject(ToastService);
  private readonly localeService = inject(LocaleService);
  private readonly ref =
    inject<MatDialogRef<EventRequestFormDialogComponent, CountryContentRequest | null>>(MatDialogRef);
  private readonly countryId = inject<string>(MAT_DIALOG_DATA);

  readonly locale = this.localeService.locale;
  readonly topics = signal<PublicTopic[]>([]);
  readonly uploadingImage = signal(false);
  readonly imagePreviewUrl = signal<string | null>(null);
  readonly saving = signal(false);
  readonly errorKey = signal<string | null>(null);
  readonly hourOptions = HOUR_OPTIONS;
  readonly minuteOptions = MINUTE_OPTIONS;
  private featuredImageUrl: string | null = null;

  readonly topicSearch = new FormControl('');
  private readonly topicSearchValue = toSignal(this.topicSearch.valueChanges, { initialValue: '' });
  readonly filteredTopics = computed(() => {
    const q = (this.topicSearchValue() ?? '').trim().toLowerCase();
    const all = this.topics();
    if (!q) return all;
    return all.filter(t =>
      (t.nameAr ?? '').toLowerCase().includes(q) || (t.nameEn ?? '').toLowerCase().includes(q)
    );
  });

  readonly startHourSearch = new FormControl('09');
  private readonly startHourSearchValue = toSignal(this.startHourSearch.valueChanges, { initialValue: '09' });
  readonly filteredStartHours = computed(() => {
    const q = (this.startHourSearchValue() ?? '').trim();
    if (!q) return this.hourOptions;
    return this.hourOptions.filter(o => o.label.startsWith(q));
  });

  readonly startMinuteSearch = new FormControl('00');
  private readonly startMinuteSearchValue = toSignal(this.startMinuteSearch.valueChanges, { initialValue: '00' });
  readonly filteredStartMinutes = computed(() => {
    const q = (this.startMinuteSearchValue() ?? '').trim();
    if (!q) return this.minuteOptions;
    return this.minuteOptions.filter(o => o.label.startsWith(q));
  });

  readonly endHourSearch = new FormControl('17');
  private readonly endHourSearchValue = toSignal(this.endHourSearch.valueChanges, { initialValue: '17' });
  readonly filteredEndHours = computed(() => {
    const q = (this.endHourSearchValue() ?? '').trim();
    if (!q) return this.hourOptions;
    return this.hourOptions.filter(o => o.label.startsWith(q));
  });

  readonly endMinuteSearch = new FormControl('00');
  private readonly endMinuteSearchValue = toSignal(this.endMinuteSearch.valueChanges, { initialValue: '00' });
  readonly filteredEndMinutes = computed(() => {
    const q = (this.endMinuteSearchValue() ?? '').trim();
    if (!q) return this.minuteOptions;
    return this.minuteOptions.filter(o => o.label.startsWith(q));
  });

  readonly form = new FormGroup<EventRequestForm>(
    {
      titleAr: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(255)] }),
      titleEn: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(255)] }),
      descriptionAr: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(2000)] }),
      descriptionEn: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(2000)] }),
      startDate: new FormControl<Date | null>(null, { validators: [Validators.required] }),
      startHour: new FormControl('09', { nonNullable: true, validators: [Validators.required] }),
      startMinute: new FormControl('00', { nonNullable: true, validators: [Validators.required] }),
      endDate: new FormControl<Date | null>(null, { validators: [Validators.required] }),
      endHour: new FormControl('17', { nonNullable: true, validators: [Validators.required] }),
      endMinute: new FormControl('00', { nonNullable: true, validators: [Validators.required] }),
      locationAr: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(255)] }),
      locationEn: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(255)] }),
      onlineMeetingUrl: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(255), Validators.pattern(/^https?:\/\/.+/i)] }),
      topicId: new FormControl('', { nonNullable: true }),
    },
    { validators: locationOrUrlRequired },
  );

  constructor() {
    void this.loadTopics();
  }

  private async loadTopics(): Promise<void> {
    const res = await this.communityApi.listTopics();
    if (res.ok) this.topics.set(res.value);
  }

  async onImagePicked(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    if (!ALLOWED_IMAGE_MIME.includes(file.type) || file.size > MAX_IMAGE_BYTES) {
      this.toast.error('newsEventRequest.form.imageInvalidType');
      return;
    }
    this.uploadingImage.set(true);
    const res = await this.media.uploadFile(file);
    this.uploadingImage.set(false);
    if (res.ok) {
      this.featuredImageUrl = res.value.url;
      this.imagePreviewUrl.set(res.value.url);
    } else {
      this.toast.error('errors.ERR027');
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorKey.set('errors.ERR013');
      return;
    }
    this.saving.set(true);
    this.errorKey.set(null);
    const v = this.form.getRawValue();
    const res = await this.api.submitRequest({
      countryId: this.countryId,
      content: {
        type: ContentType.Event,
        titleAr: v.titleAr,
        titleEn: v.titleEn,
        descriptionAr: v.descriptionAr,
        descriptionEn: v.descriptionEn,
        startsOn: combineDateTime(v.startDate, v.startHour, v.startMinute),
        endsOn: combineDateTime(v.endDate, v.endHour, v.endMinute),
        locationAr: v.locationAr || null,
        locationEn: v.locationEn || null,
        onlineMeetingUrl: v.onlineMeetingUrl || null,
        topicId: v.topicId || null,
        featuredImageUrl: this.featuredImageUrl,
      },
    });
    this.saving.set(false);
    if (res.ok) {
      this.toast.success('confirmations.CON024');
      this.ref.close(res.value);
    } else {
      this.errorKey.set('errors.ERR029');
    }
  }

  onTopicSelected(id: string, displayText: string): void {
    this.form.controls.topicId.setValue(id);
    this.topicSearch.setValue(id ? displayText : '', { emitEvent: false });
  }

  onStartHourSelected(value: string): void {
    this.form.controls.startHour.setValue(value);
    this.startHourSearch.setValue(value, { emitEvent: false });
  }

  onStartMinuteSelected(value: string): void {
    this.form.controls.startMinute.setValue(value);
    this.startMinuteSearch.setValue(value, { emitEvent: false });
  }

  onEndHourSelected(value: string): void {
    this.form.controls.endHour.setValue(value);
    this.endHourSearch.setValue(value, { emitEvent: false });
  }

  onEndMinuteSelected(value: string): void {
    this.form.controls.endMinute.setValue(value);
    this.endMinuteSearch.setValue(value, { emitEvent: false });
  }

  cancel(): void { this.ref.close(null); }
}

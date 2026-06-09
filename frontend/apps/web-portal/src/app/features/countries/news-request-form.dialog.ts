
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
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
import { RichTextEditorComponent, ToastService } from '@frontend/ui-kit';
import { CommunityApiService } from '../community/community-api.service';
import type { PublicTopic } from '../community/community.types';
import { MediaApiService } from '../../core/media/media-api.service';
import { CountriesApiService } from './countries-api.service';
import { ContentType, type CountryContentRequest } from './country.types';

const ALLOWED_IMAGE_MIME = ['image/png', 'image/jpeg', 'image/webp'];
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;

interface NewsRequestForm {
  titleAr: FormControl<string>;
  titleEn: FormControl<string>;
  contentAr: FormControl<string>;
  contentEn: FormControl<string>;
  topicId: FormControl<string>;
}

@Component({
  selector: 'cce-news-request-form-dialog',
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
    RichTextEditorComponent,
    TranslocoModule,
  ],
  templateUrl: './news-request-form.dialog.html',
  styleUrl: './news-request-form.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewsRequestFormDialogComponent {
  private readonly api = inject(CountriesApiService);
  private readonly communityApi = inject(CommunityApiService);
  private readonly media = inject(MediaApiService);
  private readonly toast = inject(ToastService);
  private readonly localeService = inject(LocaleService);
  private readonly ref =
    inject<MatDialogRef<NewsRequestFormDialogComponent, CountryContentRequest | null>>(MatDialogRef);
  private readonly countryId = inject<string>(MAT_DIALOG_DATA);

  readonly locale = this.localeService.locale;
  readonly topics = signal<PublicTopic[]>([]);
  readonly uploadingImage = signal(false);
  readonly imagePreviewUrl = signal<string | null>(null);
  readonly saving = signal(false);
  readonly errorKey = signal<string | null>(null);
  private featuredImageUrl: string | null = null;

  readonly form = new FormGroup<NewsRequestForm>({
    titleAr: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(255)] }),
    titleEn: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(255)] }),
    contentAr: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(2000)] }),
    contentEn: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(2000)] }),
    topicId: new FormControl('', { nonNullable: true }),
  });

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
        type: ContentType.News,
        titleAr: v.titleAr,
        titleEn: v.titleEn,
        contentAr: v.contentAr,
        contentEn: v.contentEn,
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

  cancel(): void { this.ref.close(null); }
}

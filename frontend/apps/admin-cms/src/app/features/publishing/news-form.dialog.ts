
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { ToastService } from '@frontend/ui-kit';
import { ContentApiService } from '../content/content-api.service';
import { PublishingApiService } from './publishing-api.service';
import type { News } from './publishing.types';

const ALLOWED_IMAGE_MIME = ['image/png', 'image/jpeg', 'image/webp'];
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const TITLE_MAX = 255;
const CONTENT_MAX = 2000;

export interface NewsFormDialogData {
  news?: News;
  /** When 'view', the form is rendered read-only and the Save action is hidden. */
  mode?: 'create' | 'edit' | 'view';
}

interface NewsForm {
  titleAr: FormControl<string>;
  titleEn: FormControl<string>;
  contentAr: FormControl<string>;
  contentEn: FormControl<string>;
  slug: FormControl<string>;
  featuredImageUrl: FormControl<string>;
}

@Component({
  selector: 'cce-news-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    TranslocoModule
],
  templateUrl: './news-form.dialog.html',
  styles: [`
    .cce-news-form { display: flex; flex-direction: column; gap: 0.5rem; }
    .cce-news-form__row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .cce-news-form__image { display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.5rem; }
    .cce-news-form__image-hint { font-size: 0.78rem; color: rgba(0,0,0,0.55); }
    .cce-news-form__preview { margin: 0 0 0.5rem; }
    .cce-news-form__preview img {
      max-width: 220px; max-height: 140px; border-radius: 8px;
      border: 1px solid rgba(0,0,0,0.08); object-fit: cover;
    }
    .cce-news-form__error {
      background: #fdecea; color: #b00020; padding: 0.6rem 0.85rem;
      border-radius: 6px; margin-top: 0.5rem; font-size: 0.85rem;
    }
    @media (max-width: 600px) {
      .cce-news-form__row { grid-template-columns: 1fr; }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewsFormDialogComponent {
  private readonly api = inject(PublishingApiService);
  private readonly assets = inject(ContentApiService);
  private readonly toast = inject(ToastService);
  readonly form: FormGroup<NewsForm>;
  readonly saving = signal(false);
  readonly uploadingImage = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly missingRequired = signal(false);
  readonly isEdit: boolean;
  readonly isView: boolean;
  readonly titleMax = TITLE_MAX;
  readonly contentMax = CONTENT_MAX;

  constructor(
    private readonly ref: MatDialogRef<NewsFormDialogComponent, News | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: NewsFormDialogData,
  ) {
    this.isView = data.mode === 'view';
    this.isEdit = !this.isView && data.news !== undefined;
    const n = data.news;
    this.form = new FormGroup<NewsForm>({
      titleAr: new FormControl(n?.titleAr ?? '', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(TITLE_MAX)],
      }),
      titleEn: new FormControl(n?.titleEn ?? '', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(TITLE_MAX)],
      }),
      contentAr: new FormControl(n?.contentAr ?? '', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(CONTENT_MAX)],
      }),
      contentEn: new FormControl(n?.contentEn ?? '', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(CONTENT_MAX)],
      }),
      slug: new FormControl(n?.slug ?? '', { nonNullable: true, validators: [Validators.required] }),
      featuredImageUrl: new FormControl(n?.featuredImageUrl ?? '', { nonNullable: true }),
    });
    if (this.isView) this.form.disable();
  }

  async onImagePicked(event: globalThis.Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    if (!ALLOWED_IMAGE_MIME.includes(file.type)) {
      this.toast.error('news.field.imageInvalidType');
      return;
    }
    if (file.size > MAX_IMAGE_BYTES) {
      this.toast.error('news.field.imageInvalidType');
      return;
    }
    this.uploadingImage.set(true);
    const res = await this.assets.uploadAsset(file);
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
    const body = { ...v, featuredImageUrl: v.featuredImageUrl || null };
    const res = this.isEdit && this.data.news
      ? await this.api.updateNews(this.data.news.id, { ...body, rowVersion: this.data.news.rowVersion })
      : await this.api.createNews(body);
    this.saving.set(false);
    if (res.ok) this.ref.close(res.value);
    else {
      this.errorKind.set(res.error.kind);
      this.toast.error('errors.ERR027');
    }
  }

  cancel(): void { this.ref.close(null); }
}

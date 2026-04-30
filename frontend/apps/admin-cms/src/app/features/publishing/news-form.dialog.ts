import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { PublishingApiService } from './publishing-api.service';
import type { News } from './publishing.types';

export interface NewsFormDialogData {
  news?: News;
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
    CommonModule, ReactiveFormsModule, MatButtonModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, TranslateModule,
  ],
  templateUrl: './news-form.dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewsFormDialogComponent {
  private readonly api = inject(PublishingApiService);
  readonly form: FormGroup<NewsForm>;
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isEdit: boolean;

  constructor(
    private readonly ref: MatDialogRef<NewsFormDialogComponent, News | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: NewsFormDialogData,
  ) {
    this.isEdit = data.news !== undefined;
    const n = data.news;
    this.form = new FormGroup<NewsForm>({
      titleAr: new FormControl(n?.titleAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      titleEn: new FormControl(n?.titleEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      contentAr: new FormControl(n?.contentAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      contentEn: new FormControl(n?.contentEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      slug: new FormControl(n?.slug ?? '', { nonNullable: true, validators: [Validators.required] }),
      featuredImageUrl: new FormControl(n?.featuredImageUrl ?? '', { nonNullable: true }),
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
    const body = { ...v, featuredImageUrl: v.featuredImageUrl || null };
    const res = this.isEdit && this.data.news
      ? await this.api.updateNews(this.data.news.id, { ...body, rowVersion: this.data.news.rowVersion })
      : await this.api.createNews(body);
    this.saving.set(false);
    if (res.ok) this.ref.close(res.value);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void { this.ref.close(null); }
}

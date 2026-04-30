import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslateModule } from '@ngx-translate/core';
import { PublishingApiService } from './publishing-api.service';
import { PAGE_TYPES, type Page, type PageType } from './publishing.types';

export interface PageFormDialogData {
  page?: Page;
}

interface PageForm {
  slug: FormControl<string>;
  pageType: FormControl<PageType>;
  titleAr: FormControl<string>;
  titleEn: FormControl<string>;
  contentAr: FormControl<string>;
  contentEn: FormControl<string>;
}

@Component({
  selector: 'cce-page-form-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule, TranslateModule,
  ],
  templateUrl: './page-form.dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageFormDialogComponent {
  private readonly api = inject(PublishingApiService);
  readonly pageTypes = PAGE_TYPES;
  readonly form: FormGroup<PageForm>;
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isEdit: boolean;

  constructor(
    private readonly ref: MatDialogRef<PageFormDialogComponent, Page | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: PageFormDialogData,
  ) {
    this.isEdit = data.page !== undefined;
    const p = data.page;
    this.form = new FormGroup<PageForm>({
      slug: new FormControl(p?.slug ?? '', { nonNullable: true, validators: [Validators.required] }),
      pageType: new FormControl<PageType>(p?.pageType ?? 'Custom', { nonNullable: true, validators: [Validators.required] }),
      titleAr: new FormControl(p?.titleAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      titleEn: new FormControl(p?.titleEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      contentAr: new FormControl(p?.contentAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      contentEn: new FormControl(p?.contentEn ?? '', { nonNullable: true, validators: [Validators.required] }),
    });
    if (this.isEdit) {
      this.form.controls.slug.disable();
      this.form.controls.pageType.disable();
    }
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorKind.set(null);
    const v = this.form.getRawValue();

    const res = this.isEdit && this.data.page
      ? await this.api.updatePage(this.data.page.id, {
          titleAr: v.titleAr,
          titleEn: v.titleEn,
          contentAr: v.contentAr,
          contentEn: v.contentEn,
          rowVersion: this.data.page.rowVersion,
        })
      : await this.api.createPage({
          slug: v.slug,
          pageType: v.pageType,
          titleAr: v.titleAr,
          titleEn: v.titleEn,
          contentAr: v.contentAr,
          contentEn: v.contentEn,
        });
    this.saving.set(false);
    if (res.ok) this.ref.close(res.value);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void { this.ref.close(null); }
}

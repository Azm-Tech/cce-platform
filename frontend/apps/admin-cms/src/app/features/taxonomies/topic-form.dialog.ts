import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';
import { TaxonomyApiService } from './taxonomy-api.service';
import type { Topic } from './taxonomy.types';

export interface TopicFormData {
  topic?: Topic;
}

interface TopicForm {
  nameAr: FormControl<string>;
  nameEn: FormControl<string>;
  descriptionAr: FormControl<string>;
  descriptionEn: FormControl<string>;
  slug: FormControl<string>;
  parentId: FormControl<string>;
  iconUrl: FormControl<string>;
  orderIndex: FormControl<number>;
  isActive: FormControl<boolean>;
}

@Component({
  selector: 'cce-topic-form-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatCheckboxModule,
    MatDialogModule, MatFormFieldModule, MatInputModule, TranslateModule,
  ],
  templateUrl: './topic-form.dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicFormDialogComponent {
  private readonly api = inject(TaxonomyApiService);
  readonly form: FormGroup<TopicForm>;
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isEdit: boolean;

  constructor(
    private readonly ref: MatDialogRef<TopicFormDialogComponent, Topic | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: TopicFormData,
  ) {
    this.isEdit = data.topic !== undefined;
    const t = data.topic;
    this.form = new FormGroup<TopicForm>({
      nameAr: new FormControl(t?.nameAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      nameEn: new FormControl(t?.nameEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      descriptionAr: new FormControl(t?.descriptionAr ?? '', { nonNullable: true }),
      descriptionEn: new FormControl(t?.descriptionEn ?? '', { nonNullable: true }),
      slug: new FormControl(t?.slug ?? '', { nonNullable: true, validators: [Validators.required] }),
      parentId: new FormControl(t?.parentId ?? '', { nonNullable: true }),
      iconUrl: new FormControl(t?.iconUrl ?? '', { nonNullable: true }),
      orderIndex: new FormControl(t?.orderIndex ?? 0, { nonNullable: true }),
      isActive: new FormControl(t?.isActive ?? true, { nonNullable: true }),
    });
    if (this.isEdit) {
      this.form.controls.slug.disable();
      this.form.controls.parentId.disable();
      this.form.controls.iconUrl.disable();
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

    const res = this.isEdit && this.data.topic
      ? await this.api.updateTopic(this.data.topic.id, {
          nameAr: v.nameAr,
          nameEn: v.nameEn,
          descriptionAr: v.descriptionAr,
          descriptionEn: v.descriptionEn,
          orderIndex: v.orderIndex,
          isActive: v.isActive,
        })
      : await this.api.createTopic({
          nameAr: v.nameAr,
          nameEn: v.nameEn,
          descriptionAr: v.descriptionAr,
          descriptionEn: v.descriptionEn,
          slug: v.slug,
          parentId: v.parentId || null,
          iconUrl: v.iconUrl || null,
          orderIndex: v.orderIndex,
        });
    this.saving.set(false);
    if (res.ok) this.ref.close(res.value);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void { this.ref.close(null); }
}

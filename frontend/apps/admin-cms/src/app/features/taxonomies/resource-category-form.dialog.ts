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
import type { ResourceCategory } from './taxonomy.types';

export interface ResourceCategoryFormData {
  category?: ResourceCategory;
}

interface CategoryForm {
  nameAr: FormControl<string>;
  nameEn: FormControl<string>;
  slug: FormControl<string>;
  parentId: FormControl<string>;
  orderIndex: FormControl<number>;
  isActive: FormControl<boolean>;
}

@Component({
  selector: 'cce-resource-category-form-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatCheckboxModule,
    MatDialogModule, MatFormFieldModule, MatInputModule, TranslateModule,
  ],
  templateUrl: './resource-category-form.dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceCategoryFormDialogComponent {
  private readonly api = inject(TaxonomyApiService);
  readonly form: FormGroup<CategoryForm>;
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isEdit: boolean;

  constructor(
    private readonly ref: MatDialogRef<ResourceCategoryFormDialogComponent, ResourceCategory | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: ResourceCategoryFormData,
  ) {
    this.isEdit = data.category !== undefined;
    const c = data.category;
    this.form = new FormGroup<CategoryForm>({
      nameAr: new FormControl(c?.nameAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      nameEn: new FormControl(c?.nameEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      slug: new FormControl(c?.slug ?? '', { nonNullable: true, validators: [Validators.required] }),
      parentId: new FormControl(c?.parentId ?? '', { nonNullable: true }),
      orderIndex: new FormControl(c?.orderIndex ?? 0, { nonNullable: true }),
      isActive: new FormControl(c?.isActive ?? true, { nonNullable: true }),
    });
    if (this.isEdit) {
      this.form.controls.slug.disable();
      this.form.controls.parentId.disable();
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

    const res = this.isEdit && this.data.category
      ? await this.api.updateCategory(this.data.category.id, {
          nameAr: v.nameAr,
          nameEn: v.nameEn,
          orderIndex: v.orderIndex,
          isActive: v.isActive,
        })
      : await this.api.createCategory({
          nameAr: v.nameAr,
          nameEn: v.nameEn,
          slug: v.slug,
          parentId: v.parentId || null,
          orderIndex: v.orderIndex,
        });
    this.saving.set(false);
    if (res.ok) this.ref.close(res.value);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void { this.ref.close(null); }
}

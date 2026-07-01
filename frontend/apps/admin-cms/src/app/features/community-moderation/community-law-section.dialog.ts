import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';
import { TranslateFieldComponent } from '@frontend/ui-kit';
import { CommunityModerationApiService } from './community-moderation-api.service';
import type { CommunityLawSectionDto } from './admin-post.types';

export interface CommunityLawSectionFormData {
  section?: CommunityLawSectionDto;
}

interface LawForm {
  titleAr: FormControl<string>;
  titleEn: FormControl<string>;
  contentAr: FormControl<string>;
  contentEn: FormControl<string>;
}

@Component({
  selector: 'cce-community-law-section-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    TranslateFieldComponent,
    TranslocoModule,
  ],
  templateUrl: './community-law-section.dialog.html',
  styleUrl: './community-law-section.dialog.scss',
  // Default CD — dialog overlay + Transloco require it.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class CommunityLawSectionDialogComponent {
  private readonly api = inject(CommunityModerationApiService);
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isEdit: boolean;
  readonly form: FormGroup<LawForm>;

  constructor(
    private readonly ref: MatDialogRef<CommunityLawSectionDialogComponent, true | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: CommunityLawSectionFormData,
  ) {
    this.isEdit = data.section !== undefined;
    const s = data.section;
    this.form = new FormGroup<LawForm>({
      titleAr: new FormControl(s?.titleAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      titleEn: new FormControl(s?.titleEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      contentAr: new FormControl(s?.contentAr ?? '', { nonNullable: true }),
      contentEn: new FormControl(s?.contentEn ?? '', { nonNullable: true }),
    });
  }

  async save(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.errorKind.set(null);
    const v = this.form.getRawValue();
    const body = {
      titleAr: v.titleAr || null,
      titleEn: v.titleEn || null,
      contentAr: v.contentAr || null,
      contentEn: v.contentEn || null,
    };
    const res = this.isEdit && this.data.section
      ? await this.api.updateSection(this.data.section.id, body)
      : await this.api.createSection(body);
    this.saving.set(false);
    if (res.ok) this.ref.close(true);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void { this.ref.close(null); }
}

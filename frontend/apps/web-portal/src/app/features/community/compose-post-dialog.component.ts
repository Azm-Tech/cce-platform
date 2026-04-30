import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators,
} from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { CommunityApiService } from './community-api.service';
import type { CreatePostPayload } from './community.types';

export interface ComposePostDialogData {
  topicId: string;
}

export interface ComposePostDialogResult {
  submitted: boolean;
  postId?: string;
}

interface ComposePostFormShape {
  content: FormControl<string>;
  locale: FormControl<'ar' | 'en'>;
  isAnswerable: FormControl<boolean>;
}

@Component({
  selector: 'cce-compose-post-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatDialogModule, MatButtonModule, MatCheckboxModule,
    MatFormFieldModule, MatInputModule, MatRadioModule,
    TranslateModule,
  ],
  templateUrl: './compose-post-dialog.component.html',
  styleUrl: './compose-post-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComposePostDialogComponent {
  private readonly api = inject(CommunityApiService);
  private readonly toast = inject(ToastService);
  private readonly localeService = inject(LocaleService);
  private readonly dialogRef =
    inject<MatDialogRef<ComposePostDialogComponent, ComposePostDialogResult>>(MatDialogRef);
  readonly data = inject<ComposePostDialogData>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly submitting = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly form: FormGroup<ComposePostFormShape> = this.fb.nonNullable.group({
    content: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.minLength(10),
      Validators.maxLength(5000),
    ]),
    locale: this.fb.nonNullable.control<'ar' | 'en'>(this.localeService.locale(), Validators.required),
    isAnswerable: this.fb.nonNullable.control(true),
  });

  async submit(): Promise<void> {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: CreatePostPayload = {
      topicId: this.data.topicId,
      content: v.content,
      locale: v.locale,
      isAnswerable: v.isAnswerable,
    };
    this.submitting.set(true);
    this.errorKind.set(null);
    const res = await this.api.createPost(payload);
    this.submitting.set(false);
    if (res.ok) {
      this.toast.success('community.compose.toast');
      this.dialogRef.close({ submitted: true, postId: res.value.id });
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  cancel(): void {
    this.dialogRef.close({ submitted: false });
  }
}

import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { CommunityApiService } from './community-api.service';
import { CommunityStateService } from './community-state.service';
import type { CreatePostPayload, PostType, PublicTopic } from './community.types';

export interface ComposePostDialogData {
  topics: PublicTopic[];
  preselectedTopicId?: string | null;
}

export interface ComposePostDialogResult {
  submitted: boolean;
  postId?: string;
}

interface ComposePostFormShape {
  title: FormControl<string>;
  topicId: FormControl<string>;
  type: FormControl<PostType>;
  content: FormControl<string>;
  locale: FormControl<'ar' | 'en'>;
  isAnswerable: FormControl<boolean>;
}

@Component({
  selector: 'cce-compose-post-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    TranslocoModule,
  ],
  templateUrl: './compose-post-dialog.component.html',
  styleUrl: './compose-post-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.Default,
})
export class ComposePostDialogComponent {
  private readonly api = inject(CommunityApiService);
  private readonly communityState = inject(CommunityStateService);
  private readonly toast = inject(ToastService);
  readonly locale = inject(LocaleService).locale;
  private readonly dialogRef =
    inject<MatDialogRef<ComposePostDialogComponent, ComposePostDialogResult>>(MatDialogRef);
  readonly data = inject<ComposePostDialogData>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly submitting = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly typeOptions: { value: PostType; labelKey: string; icon: string }[] = [
    { value: 0, labelKey: 'community.postType.informational', icon: 'info' },
    { value: 1, labelKey: 'community.postType.question', icon: 'message-circle-question-mark' },
    { value: 2, labelKey: 'community.postType.poll', icon: 'messages-square' },
  ];

  readonly form: FormGroup<ComposePostFormShape> = this.fb.nonNullable.group({
    title: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(150),
    ]),
    topicId: this.fb.nonNullable.control(
      this.data.preselectedTopicId ?? '',
      [Validators.required],
    ),
    type: this.fb.nonNullable.control<PostType>(0),
    content: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.minLength(10),
      Validators.maxLength(5000),
    ]),
    locale: this.fb.nonNullable.control<'ar' | 'en'>(
      (this.locale() as 'ar' | 'en') ?? 'ar',
    ),
    isAnswerable: this.fb.nonNullable.control(false),
  });

  topicLabel(topic: PublicTopic): string {
    return (this.locale() === 'ar' ? topic.nameAr : topic.nameEn) ??
      topic.nameEn ?? topic.nameAr ?? '';
  }

  setType(t: PostType): void {
    this.form.controls.type.setValue(t);
  }

  setLocale(l: 'ar' | 'en'): void {
    this.form.controls.locale.setValue(l);
  }

  get contentLength(): number {
    return this.form.controls.content.value.length;
  }

  async submit(): Promise<void> {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: CreatePostPayload = {
      communityId: this.communityState.communityId() ?? '',
      topicId: v.topicId,
      type: v.type,
      title: v.title.trim() || null,
      content: v.content,
      locale: v.locale,
      isAnswerable: v.isAnswerable,
      saveAsDraft: false,
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

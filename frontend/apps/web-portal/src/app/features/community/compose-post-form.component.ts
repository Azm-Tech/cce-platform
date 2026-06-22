import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
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

interface ComposePostFormShape {
  title: FormControl<string>;
  topicId: FormControl<string>;
  type: FormControl<PostType>;
  content: FormControl<string>;
  isAnswerable: FormControl<boolean>;
}

export interface ComposePostSubmittedEvent {
  postId: string;
}

@Component({
  selector: 'cce-compose-post-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    TranslocoModule,
  ],
  templateUrl: './compose-post-form.component.html',
  styleUrl: './compose-post-form.component.scss',
  // Default required — rendered inside dialog overlay, OnPush breaks Transloco
  changeDetection: ChangeDetectionStrategy.Default,
})
export class ComposePostFormComponent implements OnInit {
  private readonly api = inject(CommunityApiService);
  private readonly communityState = inject(CommunityStateService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  readonly locale = inject(LocaleService).locale;

  // ── Inputs ────────────────────────────────────────────────────────────────
  readonly topics = input<PublicTopic[]>([]);
  readonly preselectedTopicId = input<string | null>(null);

  // ── Outputs ───────────────────────────────────────────────────────────────
  readonly submitted = output<ComposePostSubmittedEvent>();

  // ── State ─────────────────────────────────────────────────────────────────
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
    topicId: this.fb.nonNullable.control('', [Validators.required]),
    type: this.fb.nonNullable.control<PostType>(0),
    content: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.minLength(10),
      Validators.maxLength(5000),
    ]),
    isAnswerable: this.fb.nonNullable.control(false),
  });

  /** Post language is derived from the user's selected UI language — not an input. */
  readonly postLocale = computed<'ar' | 'en'>(() => (this.locale() === 'en' ? 'en' : 'ar'));

  ngOnInit(): void {
    const id = this.preselectedTopicId();
    if (id) this.form.controls.topicId.setValue(id);
  }

  get contentLength(): number {
    return this.form.controls.content.value.length;
  }

  setType(t: PostType): void {
    this.form.controls.type.setValue(t);
  }

  topicLabel(topic: PublicTopic): string {
    return (this.locale() === 'ar' ? topic.nameAr : topic.nameEn) ??
      topic.nameEn ?? topic.nameAr ?? '';
  }

  /** Called by the host dialog's submit button. */
  async triggerSubmit(): Promise<void> {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: CreatePostPayload = {
      communityId: this.communityState.communityId() ?? '',
      topicId: v.topicId,
      type: v.type,
      title: v.title.trim() || null,
      content: v.content,
      locale: this.postLocale(),
      isAnswerable: v.isAnswerable,
      saveAsDraft: false,
    };
    this.submitting.set(true);
    this.errorKind.set(null);
    const res = await this.api.createPost(payload);
    this.submitting.set(false);
    if (res.ok) {
      this.toast.success('community.compose.toast');
      this.submitted.emit({ postId: res.value.id });
    } else {
      this.errorKind.set(res.error.kind);
    }
  }
}

import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
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
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { Subscription } from 'rxjs';
import { MediaApiService } from '../../core/media/media-api.service';
import { CommunityApiService } from './community-api.service';
import { CommunityStateService } from './community-state.service';
import type { AttachmentKind, CreatePostPayload, PostType, PublicTopic } from './community.types';
import {
  ATTACHMENT_RULES,
  kindForCategory,
  validateAddition,
  type MediaCategory,
} from './lib/attachment-rules';
 
interface ComposePostFormShape {
  title: FormControl<string>;
  topicId: FormControl<string>;
  type: FormControl<PostType>;
  content: FormControl<string>;
  isAnswerable: FormControl<boolean>;
}

interface StagedAttachment {
  id: string; // client-side tracking ID
  file: File;
  fileName: string;
  sizeBytes: number;
  mimeType: string;
  category: MediaCategory;
  kind: AttachmentKind;
  previewUrl?: string;

  // Upload status signals
  status: 'uploading' | 'success' | 'error';
  progress: number;
  assetFileId?: string; // set once success
  error?: string; // error translation key
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
    MatProgressBarModule,
    MatProgressSpinnerModule,
    TranslocoModule,
  ],
  templateUrl: './compose-post-form.component.html',
  styleUrl: './compose-post-form.component.scss',
  // Default required — rendered inside dialog overlay, OnPush breaks Transloco
  changeDetection: ChangeDetectionStrategy.Default,
})
export class ComposePostFormComponent implements OnInit, OnDestroy {
  private readonly api = inject(CommunityApiService);
  private readonly communityState = inject(CommunityStateService);
  private readonly toast = inject(ToastService);
  private readonly media = inject(MediaApiService);
  private readonly fb = inject(FormBuilder);
  readonly locale = inject(LocaleService).locale;
  private readonly uploadSubscriptions = new Map<string, Subscription>();

  // ── Inputs ────────────────────────────────────────────────────────────────
  readonly topics = input<PublicTopic[]>([]);
  readonly preselectedTopicId = input<string | null>(null);

  // ── Outputs ───────────────────────────────────────────────────────────────
  readonly submitted = output<ComposePostSubmittedEvent>();

  // ── State ─────────────────────────────────────────────────────────────────
  readonly submitting = signal(false);
  readonly errorKind = signal<string | null>(null);

  // Attachments (optional, any post type) — uploaded to /api/media on pick.
  readonly attachments = signal<StagedAttachment[]>([]);
  readonly uploading = signal(false);
  readonly attachmentError = signal<string | null>(null);

  // Per-category limits (exposed to the template for hints + disabled state).
  readonly rules = ATTACHMENT_RULES;
  readonly images = computed(() => this.attachments().filter((a) => a.category === 'image'));
  readonly videos = computed(() => this.attachments().filter((a) => a.category === 'video'));
  readonly files = computed(() => this.attachments().filter((a) => a.category === 'file'));
  readonly imagesFull = computed(() => this.images().length >= ATTACHMENT_RULES.image.maxCount);
  readonly videoFull = computed(() => this.videos().length >= ATTACHMENT_RULES.video.maxCount);
  readonly filesFull = computed(() => this.files().length >= ATTACHMENT_RULES.file.maxCount);

  // Poll (type === Poll) — managed outside the reactive form, validated manually.
  readonly pollOptions = signal<string[]>(['', '']);
  readonly pollDeadline = signal<string>('');
  readonly pollAllowMultiple = signal(false);
  readonly pollAnonymous = signal(false);
  readonly pollShowResults = signal(false);

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

  get isPoll(): boolean {
    return this.form.controls.type.value === 2;
  }

  get isQuestion(): boolean {
    return this.form.controls.type.value === 1;
  }

  topicLabel(topic: PublicTopic): string {
    return (this.locale() === 'ar' ? topic.nameAr : topic.nameEn) ??
      topic.nameEn ?? topic.nameAr ?? '';
  }

  // ── Poll options ────────────────────────────────────────────────────────────
  addPollOption(): void {
    this.pollOptions.update((o) => [...o, '']);
  }

  removePollOption(index: number): void {
    this.pollOptions.update((o) => (o.length <= 2 ? o : o.filter((_, i) => i !== index)));
  }

  setPollOption(index: number, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.pollOptions.update((o) => o.map((x, i) => (i === index ? value : x)));
  }

  setPollDeadline(event: Event): void {
    this.pollDeadline.set((event.target as HTMLInputElement).value);
  }

  togglePollFlag(flag: 'allowMultiple' | 'anonymous' | 'showResults', event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    if (flag === 'allowMultiple') this.pollAllowMultiple.set(checked);
    else if (flag === 'anonymous') this.pollAnonymous.set(checked);
    else this.pollShowResults.set(checked);
  }

  private isPollValid(): boolean {
    if (!this.isPoll) return true;
    const opts = this.pollOptions().map((o) => o.trim()).filter(Boolean);
    return opts.length >= 2 && !!this.pollDeadline();
  }

  /** Used by the host dialog to gate its submit button. */
  canSubmit(): boolean {
    const hasUploading = this.attachments().some((a) => a.status === 'uploading');
    return !this.form.invalid && this.isPollValid() && !hasUploading;
  }

  // ── Attachments ─────────────────────────────────────────────────────────────
  onFilesPicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    void this.handleFiles(input.files);
    input.value = '';
  }

  private async handleFiles(list: FileList | null): Promise<void> {
    if (!list || list.length === 0) return;
    this.attachmentError.set(null);

    // Create a working staged array first so we validate files against
    // the running total as we process them.
    let stagedSetForValidation = [...this.attachments()];

    for (const file of Array.from(list)) {
      const check = validateAddition(stagedSetForValidation, file);
      if (!check.ok) {
        this.attachmentError.set(check.errorKey);
        continue;
      }

      const category = check.category;
      const kind = kindForCategory(category);
      const previewUrl = category === 'image' ? URL.createObjectURL(file) : undefined;
      const attachmentId =
        typeof crypto !== 'undefined' && crypto.randomUUID
          ? crypto.randomUUID()
          : Math.random().toString(36).substring(2);

      const newStaged: StagedAttachment = {
        id: attachmentId,
        file,
        fileName: file.name,
        sizeBytes: file.size,
        mimeType: file.type,
        category,
        kind,
        previewUrl,
        status: 'uploading',
        progress: 0,
      };

      // Add to staged set so next iterations in the batch validate correctly
      stagedSetForValidation.push(newStaged);

      // Append immediately to the UI attachments list
      this.attachments.update((a) => [...a, newStaged]);

      // Trigger upload
      const sub = this.media.uploadFileWithProgress(file).subscribe({
        next: (update) => {
          if (update.error) {
            this.updateAttachmentError(attachmentId, 'errors.' + update.error.kind);
          } else if (update.asset) {
            this.updateAttachmentSuccess(attachmentId, update.asset.id);
          } else {
            this.updateAttachmentProgress(attachmentId, update.progress);
          }
        },
        error: () => {
          this.updateAttachmentError(attachmentId, 'errors.server');
        },
      });

      this.uploadSubscriptions.set(attachmentId, sub);
    }
  }

  updateAttachmentProgress(id: string, progress: number): void {
    this.attachments.update((a) =>
      a.map((x) => (x.id === id ? { ...x, progress } : x)),
    );
  }

  updateAttachmentSuccess(id: string, assetFileId: string): void {
    this.attachments.update((a) =>
      a.map((x) =>
        x.id === id ? { ...x, status: 'success', progress: 100, assetFileId } : x,
      ),
    );
  }

  updateAttachmentError(id: string, errorKey: string): void {
    this.attachments.update((a) =>
      a.map((x) => (x.id === id ? { ...x, status: 'error', error: errorKey } : x)),
    );
  }

  cancelUpload(id: string): void {
    const sub = this.uploadSubscriptions.get(id);
    if (sub) {
      sub.unsubscribe();
      this.uploadSubscriptions.delete(id);
    }
    this.removeAttachment(id);
  }

  removeAttachment(id: string): void {
    const sub = this.uploadSubscriptions.get(id);
    if (sub) {
      sub.unsubscribe();
      this.uploadSubscriptions.delete(id);
    }
    this.attachments.update((a) => {
      const target = a.find((x) => x.id === id);
      if (target?.previewUrl) URL.revokeObjectURL(target.previewUrl);
      return a.filter((x) => x.id !== id);
    });
  }

  ngOnDestroy(): void {
    for (const sub of this.uploadSubscriptions.values()) {
      sub.unsubscribe();
    }
    this.uploadSubscriptions.clear();
    for (const a of this.attachments()) {
      if (a.previewUrl) URL.revokeObjectURL(a.previewUrl);
    }
  }

  /** mat-icon name for a category chip. */
  categoryIcon(category: MediaCategory): string {
    if (category === 'image') return 'image';
    if (category === 'video') return 'video';
    return 'file-text';
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  // ── Submit ────────────────────────────────────────────────────────────────
  /** Called by the host dialog's submit button. */
  async triggerSubmit(): Promise<void> {
    if (!this.canSubmit() || this.submitting()) return;
    const v = this.form.getRawValue();
    // Deterministic order: images, then the video, then files.
    const order: Record<MediaCategory, number> = { image: 0, video: 1, file: 2 };
    const atts = this.attachments()
      .filter((a) => a.status === 'success' && a.assetFileId)
      .sort((a, b) => order[a.category] - order[b.category]);
    const payload: CreatePostPayload = {
      communityId: this.communityState.communityId() ?? '',
      topicId: v.topicId,
      type: v.type,
      title: v.title.trim() || null,
      content: v.content,
      locale: this.postLocale(),
      isAnswerable: v.isAnswerable,
      attachments: atts.length
        ? atts.map((a, i) => ({
            assetFileId: a.assetFileId!,
            kind: a.kind,
            sortOrder: i,
            mimeType: a.mimeType,
            sizeBytes: a.sizeBytes,
          }))
        : null,
      poll: this.isPoll
        ? CommunityApiService.buildPollPayload({
            deadline: new Date(this.pollDeadline()).toISOString(),
            optionLabels: this.pollOptions().map((o) => o.trim()).filter(Boolean),
            allowMultiple: this.pollAllowMultiple(),
            isAnonymous: this.pollAnonymous(),
            showResultsBeforeClose: this.pollShowResults(),
          })
        : null,
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

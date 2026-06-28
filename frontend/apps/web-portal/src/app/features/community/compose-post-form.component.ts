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
import { MediaApiService } from '../../core/media/media-api.service';
import { CommunityApiService } from './community-api.service';
import { CommunityStateService } from './community-state.service';
import type { AttachmentKind, CreatePostPayload, PostType, PublicTopic } from './community.types';

interface ComposePostFormShape {
  title: FormControl<string>;
  topicId: FormControl<string>;
  type: FormControl<PostType>;
  content: FormControl<string>;
  isAnswerable: FormControl<boolean>;
}

/** A file already uploaded to the assets store, ready to attach on submit. */
interface UploadedAttachment {
  assetFileId: string;
  fileName: string;
  sizeBytes: number;
  kind: AttachmentKind;
}

export interface ComposePostSubmittedEvent {
  postId: string;
}

const MAX_ATTACHMENT_BYTES = 3 * 1024 * 1024; // 3 MB
const ALLOWED_EXTENSIONS = ['jpg', 'jpeg', 'png', 'pdf', 'docx', 'doc'];

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
  private readonly media = inject(MediaApiService);
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

  // Attachments (optional, any post type) — uploaded to /api/assets on pick.
  readonly attachments = signal<UploadedAttachment[]>([]);
  readonly uploading = signal(false);
  readonly attachmentError = signal<string | null>(null);
  readonly dragOver = signal(false);

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
    return !this.form.invalid && this.isPollValid() && !this.uploading();
  }

  // ── Attachments ─────────────────────────────────────────────────────────────
  onFilesPicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    void this.handleFiles(input.files);
    input.value = '';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(true);
  }

  onDragLeave(): void {
    this.dragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
    void this.handleFiles(event.dataTransfer?.files ?? null);
  }

  private async handleFiles(list: FileList | null): Promise<void> {
    if (!list || list.length === 0) return;
    this.attachmentError.set(null);
    for (const file of Array.from(list)) {
      const ext = file.name.split('.').pop()?.toLowerCase() ?? '';
      if (!ALLOWED_EXTENSIONS.includes(ext)) {
        this.attachmentError.set('community.compose.attachmentBadType');
        continue;
      }
      if (file.size > MAX_ATTACHMENT_BYTES) {
        this.attachmentError.set('community.compose.attachmentTooLarge');
        continue;
      }
      this.uploading.set(true);
      const res = await this.media.uploadAsset(file);
      this.uploading.set(false);
      if (res.ok) {
        // kind: 0 = Media (inline image/video), 1 = Document (downloadable).
        const isMedia = file.type.startsWith('image/') || file.type.startsWith('video/');
        const kind: AttachmentKind = isMedia ? 0 : 1;
        this.attachments.update((a) => [
          ...a,
          { assetFileId: res.value.id, fileName: file.name, sizeBytes: file.size, kind },
        ]);
      } else {
        this.attachmentError.set('errors.' + res.error.kind);
      }
    }
  }

  removeAttachment(assetFileId: string): void {
    this.attachments.update((a) => a.filter((x) => x.assetFileId !== assetFileId));
  }

  attachmentIcon(kind: AttachmentKind): string {
    return kind === 0 ? 'image' : 'file-text';
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
    const atts = this.attachments();
    const payload: CreatePostPayload = {
      communityId: this.communityState.communityId() ?? '',
      topicId: v.topicId,
      type: v.type,
      title: v.title.trim() || null,
      content: v.content,
      locale: this.postLocale(),
      isAnswerable: v.isAnswerable,
      attachments: atts.length
        ? atts.map((a, i) => ({ assetFileId: a.assetFileId, kind: a.kind, sortOrder: i }))
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

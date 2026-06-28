
import {
  ChangeDetectionStrategy, Component, ElementRef, HostListener,
  OnDestroy, OnInit, ViewChild, inject, input, output, signal,
} from '@angular/core';
import {
  FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators,
} from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { RealtimeHubService } from '@frontend/real-time';
import { AuthService } from '../../core/auth/auth.service';
import { CommunityApiService } from './community-api.service';
import { CommunityAuthPromptService } from './community-auth-prompt.service';
import { CommunityStateService } from './community-state.service';
import type { CreateReplyPayload, MentionableUser } from './community.types';

interface ComposeReplyFormShape {
  content: FormControl<string>;
  locale: FormControl<'ar' | 'en'>;
}

@Component({
  selector: 'cce-compose-reply-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatIconModule,
    TranslocoModule,
  ],
  templateUrl: './compose-reply-form.component.html',
  styleUrl: './compose-reply-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComposeReplyFormComponent implements OnInit, OnDestroy {
  private readonly api = inject(CommunityApiService);
  private readonly auth = inject(AuthService);
  private readonly authPrompt = inject(CommunityAuthPromptService);
  private readonly toast = inject(ToastService);
  private readonly localeService = inject(LocaleService);
  private readonly fb = inject(FormBuilder);
  private readonly hub = inject(RealtimeHubService);
  private readonly elementRef = inject(ElementRef);
  private readonly communityState = inject(CommunityStateService);

  readonly postId = input.required<string>();
  readonly replyCreated = output<string>();
  /** Set when the user clicks "Reply" on a specific reply — shows the replying-to chip. */
  readonly parentReply = input<{ id: string; authorName: string | null } | null>(null);
  /** Emitted when the user dismisses the replying-to chip. */
  readonly cancelParent = output<void>();

  // ── Mention picker ─────────────────────────────────────────────────────────
  readonly mentionOpen = signal(false);
  readonly mentionLoading = signal(false);
  readonly autocompleteResults = signal<MentionableUser[]>([]);
  private _mentionAtPos = -1;
  private _debounceTimer: ReturnType<typeof setTimeout> | null = null;
  // Maps display name → userId for mentions inserted this session.
  // Used to rebuild the @[uuid:name] tag format at submit time without
  // showing the raw tag to the user while they type.
  private _mentionMap: Array<{ displayName: string; userId: string }> = [];

  @ViewChild('textareaEl') textareaEl?: ElementRef<HTMLTextAreaElement>;

  // ── Typing indicator broadcast ──────────────────────────────────────────
  private typingActive = false;
  private lastTypingSent = 0;
  private idleTimer: ReturnType<typeof setTimeout> | null = null;
  private static readonly TYPING_REEMIT_MS = 2000;
  private static readonly TYPING_IDLE_MS = 3000;

  readonly submitting = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly form: FormGroup<ComposeReplyFormShape> = this.fb.nonNullable.group({
    content: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.minLength(1),
      Validators.maxLength(500),
    ]),
    locale: this.fb.nonNullable.control<'ar' | 'en'>('en', Validators.required),
  });

  ngOnInit(): void {
    this.form.controls.locale.setValue(this.localeService.locale());
  }

  ngOnDestroy(): void {
    this.endTyping();
    this.closeMention();
  }

  /** Anonymous users get the auth dialog the moment they focus the field. */
  onFieldFocus(event: FocusEvent): void {
    if (this.auth.isAuthenticated()) return;
    (event.target as HTMLElement | null)?.blur();
    this.authPrompt.requireAuth('community.authDialog.messageReply');
  }

  /** Broadcast typing (throttled re-emit); also drives @mention detection. */
  onType(event: Event): void {
    this.detectMention(event.target as HTMLTextAreaElement);

    if (!this.auth.isAuthenticated()) return;
    const now = Date.now();
    if (now - this.lastTypingSent > ComposeReplyFormComponent.TYPING_REEMIT_MS) {
      this.lastTypingSent = now;
      this.typingActive = true;
      this.hub.startTyping(this.postId());
    }
    if (this.idleTimer) clearTimeout(this.idleTimer);
    this.idleTimer = setTimeout(() => this.endTyping(), ComposeReplyFormComponent.TYPING_IDLE_MS);
  }

  onKeydown(event: KeyboardEvent): void {
    if (this.mentionOpen() && event.key === 'Escape') {
      event.preventDefault();
      this.closeMention();
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.mentionOpen()) return;
    if (this.elementRef.nativeElement.contains(event.target as Node)) return;
    this.closeMention();
  }

  /** Stop broadcasting typing (idle, blur, submit, or destroy). */
  endTyping(): void {
    if (this.idleTimer) {
      clearTimeout(this.idleTimer);
      this.idleTimer = null;
    }
    if (this.typingActive) {
      this.typingActive = false;
      this.lastTypingSent = 0;
      this.hub.stopTyping(this.postId());
    }
  }

  private detectMention(ta: HTMLTextAreaElement): void {
    const caret = ta.selectionStart ?? 0;
    const before = ta.value.slice(0, caret);
    // Match @ at start or after whitespace/punctuation, followed by word chars or Arabic (min 2 chars)
    const match = /(^|[\s,])@([\w؀-ۿ]{2,})$/.exec(before);
    if (match) {
      const query = match[2];
      this._mentionAtPos = before.lastIndexOf('@');
      this.mentionOpen.set(true);
      if (this._debounceTimer) clearTimeout(this._debounceTimer);
      this._debounceTimer = setTimeout(() => void this.fetchMentionableUsers(query), 250);
    } else {
      this.closeMention();
    }
  }

  private async fetchMentionableUsers(query: string): Promise<void> {
    await this.communityState.ensureLoaded();
    const communityId = this.communityState.communityId();
    if (!communityId) return;
    this.mentionLoading.set(true);
    const res = await this.api.getMentionableUsers(communityId, query);
    this.mentionLoading.set(false);
    if (res.ok) this.autocompleteResults.set(res.value);
  }

  /** Insert @displayName into the textarea (clean display); record the userId mapping for submit. */
  selectMention(user: MentionableUser): void {
    const ta = this.textareaEl?.nativeElement;
    if (!ta || this._mentionAtPos < 0) return;
    const caret = ta.selectionStart ?? 0;
    const val = ta.value;
    // Show plain @displayName — the tag is reconstructed at submit time.
    const replacement = `@${user.displayName} `;
    const newVal = val.slice(0, this._mentionAtPos) + replacement + val.slice(caret);
    const newCaret = this._mentionAtPos + replacement.length;

    this.form.controls.content.setValue(newVal);
    // Store mapping so submit() can encode @displayName → @[userId:displayName]
    if (!this._mentionMap.some((m) => m.userId === user.userId)) {
      this._mentionMap.push({ displayName: user.displayName, userId: user.userId });
    }
    this.closeMention();

    setTimeout(() => {
      ta.setSelectionRange(newCaret, newCaret);
      ta.focus();
    });
  }

  /** Replace @displayName tokens with @[userId:displayName] tags for the server. */
  private encodementions(content: string): string {
    if (this._mentionMap.length === 0) return content;
    let result = content;
    // Sort longest display name first to avoid partial replacements (e.g. "Ali" inside "Ali Hassan")
    const sorted = [...this._mentionMap].sort((a, b) => b.displayName.length - a.displayName.length);
    for (const { displayName, userId } of sorted) {
      result = result.split(`@${displayName}`).join(`@[${userId}:${displayName}]`);
    }
    return result;
  }

  private closeMention(): void {
    if (this._debounceTimer) { clearTimeout(this._debounceTimer); this._debounceTimer = null; }
    if (!this.mentionOpen()) return;
    this.mentionOpen.set(false);
    this.autocompleteResults.set([]);
    this._mentionAtPos = -1;
  }

  async submit(): Promise<void> {
    if (!this.authPrompt.requireAuth('community.authDialog.messageReply')) return;
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: CreateReplyPayload = {
      content: this.encodementions(v.content.trim()),
      locale: v.locale,
      parentReplyId: this.parentReply()?.id ?? null,
    };
    this.submitting.set(true);
    this.errorKind.set(null);
    const res = await this.api.createReply(this.postId(), payload);
    this.submitting.set(false);
    if (res.ok) {
      this.endTyping();
      this.toast.success('community.reply.toast');
      this.form.reset({ content: '', locale: this.localeService.locale() });
      this._mentionMap = [];
      this.replyCreated.emit(res.value.id);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }
}

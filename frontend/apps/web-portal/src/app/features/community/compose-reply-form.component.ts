
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, input, output, signal } from '@angular/core';
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
import type { CreateReplyPayload } from './community.types';

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

  readonly postId = input.required<string>();
  readonly replyCreated = output<string>();

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
  }

  /** Anonymous users get the auth dialog the moment they focus the field. */
  onFieldFocus(event: FocusEvent): void {
    if (this.auth.isAuthenticated()) return;
    (event.target as HTMLElement | null)?.blur();
    this.authPrompt.requireAuth('community.authDialog.messageReply');
  }

  /** Broadcast typing (throttled re-emit) while the user types; auto-stop on idle. */
  onType(): void {
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

  async submit(): Promise<void> {
    if (!this.authPrompt.requireAuth('community.authDialog.messageReply')) return;
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: CreateReplyPayload = {
      content: v.content.trim(),
      locale: v.locale,
      parentReplyId: null,
    };
    this.submitting.set(true);
    this.errorKind.set(null);
    const res = await this.api.createReply(this.postId(), payload);
    this.submitting.set(false);
    if (res.ok) {
      this.endTyping();
      this.toast.success('community.reply.toast');
      this.form.reset({ content: '', locale: this.localeService.locale() });
      this.replyCreated.emit(res.value.id);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }
}

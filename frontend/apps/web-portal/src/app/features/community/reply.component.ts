import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { ComposeReplyFormComponent } from './compose-reply-form.component';
import { authorHandle, authorInitial, timeAgo } from './lib/social-helpers';
import { LikeDislikeControlComponent } from './like-dislike-control.component';
import { MarkAnswerButtonComponent } from './mark-answer-button.component';
import type { PublicPostReply } from './community.types';

/**
 * Reply card rendered in the social-feed style: gradient avatar tile,
 * speech-bubble body, byline with locale-aware time-ago, expert + lang
 * chips, and an action row with Like / Dislike thumbs + a Reply button
 * that opens an inline thread composer in the parent page. Optional
 * "Mark as answer" footer is visible only to the post author for a
 * not-yet-accepted reply.
 *
 * The component accepts a list of nested children so reply-to-reply
 * conversations can render as an indented thread.
 */
@Component({
  selector: 'cce-reply',
  standalone: true,
  imports: [
    CommonModule, DatePipe,
    MatIconModule,
    TranslateModule,
    ComposeReplyFormComponent,
    LikeDislikeControlComponent,
    MarkAnswerButtonComponent,
  ],
  templateUrl: './reply.component.html',
  styleUrl: './reply.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReplyComponent {
  private readonly localeService = inject(LocaleService);

  readonly reply = input.required<PublicPostReply>();
  /** True when this reply is the post's accepted answer. Renders a green ribbon. */
  readonly isAccepted = input<boolean>(false);
  /** When provided, shows the "mark as accepted answer" button (author-only host gates this). */
  readonly markableForPostId = input<string | null>(null);
  /** Direct children of this reply in the thread tree. Rendered indented. */
  readonly children = input<readonly PublicPostReply[]>([]);
  /** The id of the reply currently being replied to. Drives the
   *  highlight ring on the targeted bubble. */
  readonly replyingToId = input<string | null>(null);
  /** When this reply is the post's accepted answer (parent-only flag). */
  readonly acceptedReplyId = input<string | null>(null);
  /** Active user id used to gate the Mark-as-answer button on children. */
  readonly canMarkAnswer = input<boolean>(false);
  /** Post id used by the Mark-as-answer button on child replies. */
  readonly postId = input<string | null>(null);

  /** Whether the active user is authenticated. Drives whether the
   *  inline reply composer or a sign-in CTA shows up when this reply
   *  is the active replyingTo target. */
  readonly isAuthenticated = input<boolean>(false);
  /** Re-emitted by the inner MarkAnswerButton so parent pages can refresh. */
  readonly answerMarked = output<void>();
  /** User clicked the "Reply" button — parent page opens the inline
   *  thread composer with this reply as the parent. */
  readonly replyClicked = output<PublicPostReply>();
  /** Emitted when the inline composer creates a reply — propagates
   *  up to the page so it can refresh the list. */
  readonly replyCreated = output<void>();
  /** Emitted when the user closes the inline composer — propagates
   *  up so the page clears `replyingToReplyId`. */
  readonly cancelReply = output<void>();

  readonly locale = this.localeService.locale;
  readonly showLanguageBadge = computed(
    () => this.reply().locale !== this.localeService.locale(),
  );
  readonly isHighlighted = computed(
    () => this.replyingToId() === this.reply().id,
  );

  // ─── Social helpers (locale-aware) ───────────────────────
  timeAgo(iso: string): string { return timeAgo(iso, this.locale()); }
  authorHandle(id: string): string { return authorHandle(id); }
  authorInitial(id: string): string { return authorInitial(id); }

  onReplyClick(): void {
    this.replyClicked.emit(this.reply());
  }
}

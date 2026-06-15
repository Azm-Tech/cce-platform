import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { ComposeReplyFormComponent } from './compose-reply-form.component';
import { timeAgo } from './lib/social-helpers';
import { MarkAnswerButtonComponent } from './mark-answer-button.component';
import { CommunityApiService } from './community-api.service';
import type { PublicPostReply, VoteDirection } from './community.types';

@Component({
  selector: 'cce-reply',
  standalone: true,
  imports: [
    DatePipe,
    MatIconModule,
    TranslocoModule,
    ComposeReplyFormComponent,
    MarkAnswerButtonComponent,
  ],
  templateUrl: './reply.component.html',
  styleUrl: './reply.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReplyComponent {
  private readonly localeService = inject(LocaleService);
  private readonly api = inject(CommunityApiService);
  private readonly toast = inject(ToastService);

  readonly reply = input.required<PublicPostReply>();
  readonly isAccepted = input<boolean>(false);
  readonly markableForPostId = input<string | null>(null);
  readonly children = input<readonly PublicPostReply[]>([]);
  readonly replyingToId = input<string | null>(null);
  readonly acceptedReplyId = input<string | null>(null);
  readonly canMarkAnswer = input<boolean>(false);
  readonly postId = input<string | null>(null);
  readonly isAuthenticated = input<boolean>(false);

  readonly answerMarked = output<void>();
  readonly replyClicked = output<PublicPostReply>();
  readonly replyCreated = output<void>();
  readonly cancelReply = output<void>();

  readonly locale = this.localeService.locale;

  readonly isHighlighted = computed(() => this.replyingToId() === this.reply().id);

  readonly displayName = computed(() => this.reply().authorName?.trim() || 'عضو');
  readonly avatarInitial = computed(() => {
    const name = this.reply().authorName?.trim();
    return name ? name[0].toUpperCase() : '?';
  });

  readonly myVote = signal<VoteDirection>(0);
  readonly voting = signal(false);
  readonly voteScore = computed(
    () => this.reply().upvoteCount + (this.myVote() === 1 ? 1 : 0),
  );

  timeAgo(iso: string): string { return timeAgo(iso, this.locale()); }

  onReplyClick(): void {
    this.replyClicked.emit(this.reply());
  }

  async onVote(): Promise<void> {
    if (!this.isAuthenticated()) {
      this.toast.error('community.signInToRate');
      return;
    }
    if (this.voting()) return;

    const prev = this.myVote();
    const next: VoteDirection = prev === 1 ? 0 : 1;

    this.myVote.set(next);
    this.voting.set(true);

    const res = await this.api.voteReply(this.reply().id, next);

    this.voting.set(false);

    if (!res.ok) {
      this.myVote.set(prev);
      this.toast.error('errors.' + res.error.kind);
    }
  }
}

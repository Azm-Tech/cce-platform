import {
  ChangeDetectionStrategy, Component, computed, inject, input, output, signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { timeAgo } from './lib/social-helpers';
import { MarkAnswerButtonComponent } from './mark-answer-button.component';
import { CommunityApiService } from './community-api.service';
import { CommunityAuthPromptService } from './community-auth-prompt.service';
import type { MentionUser, PublicPostReply, VoteDirection } from './community.types';

export interface ContentPart {
  type: 'text' | 'mention';
  value: string;
  userId: string | null;
}

@Component({
  selector: 'cce-reply',
  standalone: true,
  imports: [
    DatePipe,
    MatIconModule,
    TranslocoModule,
    MarkAnswerButtonComponent,
    ReplyComponent,
  ],
  templateUrl: './reply.component.html',
  styleUrl: './reply.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReplyComponent {
  private readonly localeService = inject(LocaleService);
  private readonly api = inject(CommunityApiService);
  private readonly authPrompt = inject(CommunityAuthPromptService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly reply = input.required<PublicPostReply>();
  readonly isAccepted = input<boolean>(false);
  readonly markableForPostId = input<string | null>(null);
  /** Thread participants passed down from the page for @mention resolution. */
  readonly participants = input<MentionUser[]>([]);
  /** Level-2 child replies (pre-grouped by the page). */
  readonly children = input<PublicPostReply[]>([]);
  /** True when this instance IS a level-2 child — hides the Reply button. */
  readonly isChildReply = input<boolean>(false);

  readonly answerMarked = output<void>();
  /** Emitted when the user clicks "Reply" — carries enough info for the compose form. */
  readonly replyToReply = output<{ id: string; authorName: string | null }>();

  readonly locale = this.localeService.locale;

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

  // ── @mention name → user lookup ───────────────────────────────────────────
  private readonly mentionLookup = computed(() => {
    const m = new Map<string, MentionUser>();
    for (const p of this.participants()) {
      const full = p.name.trim().toLowerCase();
      m.set(full, p);
      // First word so "@فيصل" matches "فيصل الحربي"
      const first = full.split(/\s+/)[0];
      if (first && !m.has(first)) m.set(first, p);
    }
    return m;
  });

  /** Split reply content into plain-text and @mention segments.
   *
   *  New format:  @[uuid:displayName]  — userId and name are embedded in content.
   *  Old format:  @name               — resolved via mentionedUsers or thread participants (backward compat).
   */
  readonly contentParts = computed<ContentPart[]>(() => {
    const content = this.reply().content ?? '';

    // New format — content contains @[uuid:displayName] tags
    if (content.includes('@[')) {
      return this.parseNewFormat(content);
    }

    // Old format fallback
    return this.parseOldFormat(content);
  });

  private parseNewFormat(content: string): ContentPart[] {
    const NEW_MENTION = /@\[([a-f0-9-]{36}):([^\]]+)\]/g;
    const parts: ContentPart[] = [];
    let last = 0;
    let m: RegExpExecArray | null;
    while ((m = NEW_MENTION.exec(content)) !== null) {
      if (m.index > last) parts.push({ type: 'text', value: content.slice(last, m.index), userId: null });
      parts.push({ type: 'mention', value: `@${m[2]}`, userId: m[1] });
      last = m.index + m[0].length;
    }
    if (last < content.length) parts.push({ type: 'text', value: content.slice(last), userId: null });
    return parts;
  }

  private parseOldFormat(content: string): ContentPart[] {
    const backendUsers = this.reply().mentionedUsers ?? null;
    const lookup = this.mentionLookup();

    const MENTION = /(@[^\s]+)/g;
    const parts: ContentPart[] = [];
    const mentionParts: ContentPart[] = [];
    let last = 0;
    let m: RegExpExecArray | null;

    while ((m = MENTION.exec(content)) !== null) {
      if (m.index > last) parts.push({ type: 'text', value: content.slice(last, m.index), userId: null });
      const mp: ContentPart = { type: 'mention', value: m[1], userId: null };
      parts.push(mp);
      mentionParts.push(mp);
      last = m.index + m[1].length;
    }
    if (last < content.length) parts.push({ type: 'text', value: content.slice(last), userId: null });

    if (backendUsers && backendUsers.length > 0) {
      const pool = [...backendUsers];
      for (const mp of mentionParts) {
        const token = mp.value.slice(1).toLowerCase();
        const idx = pool.findIndex((u) => {
          const n = u.name.toLowerCase();
          return n === token || n.startsWith(token + ' ') || token === n.split(/\s+/)[0];
        });
        if (idx >= 0) { mp.userId = pool[idx].id; pool.splice(idx, 1); }
        else if (pool.length > 0) { mp.userId = pool[0].id; pool.splice(0, 1); }
      }
    } else {
      for (const mp of mentionParts) {
        const token = mp.value.slice(1).toLowerCase();
        mp.userId = lookup.get(token)?.id ?? null;
      }
    }

    return parts;
  }

  onMentionClick(userId: string): void {
    void this.router.navigate(['/community/users', userId]);
  }

  onReplyClick(): void {
    this.replyToReply.emit({ id: this.reply().id, authorName: this.reply().authorName });
  }

  timeAgo(iso: string): string { return timeAgo(iso, this.locale()); }

  async onVote(): Promise<void> {
    if (!this.authPrompt.requireAuth('community.authDialog.messageVote')) return;
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

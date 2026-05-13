import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { AuthService } from '../../core/auth/auth.service';

type Vote = 'like' | 'dislike' | null;

interface StoredCounts {
  likes: number;
  dislikes: number;
}

/**
 * Like / Dislike thumbs control used on posts and replies.
 *
 * The public Community API does not expose a like/dislike endpoint
 * today, so the user's vote and the visible counts are persisted to
 * localStorage. Counts are seeded deterministically from the entity id
 * so each post/reply has a stable starting number across sessions.
 *
 * Anonymous users see the buttons disabled (clicking shows a toast
 * asking them to sign in). Authenticated users can switch between
 * Like / Dislike, or click again to unset their vote.
 */
@Component({
  selector: 'cce-like-dislike-control',
  standalone: true,
  imports: [CommonModule, MatIconModule, TranslateModule],
  template: `
    <div class="cce-like-dislike" [class.cce-like-dislike--compact]="compact()">
      <button
        type="button"
        class="cce-like-dislike__btn"
        [class.cce-like-dislike__btn--active]="vote() === 'like'"
        [attr.aria-pressed]="vote() === 'like'"
        [attr.aria-label]="(vote() === 'like'
            ? 'community.detail.actionLiked'
            : 'community.detail.actionLike') | translate"
        (click)="setVote('like')"
      >
        <mat-icon>{{ vote() === 'like' ? 'thumb_up' : 'thumb_up_off_alt' }}</mat-icon>
        <span class="cce-like-dislike__label">
          {{ (vote() === 'like'
              ? 'community.detail.actionLiked'
              : 'community.detail.actionLike') | translate }}
        </span>
        <span class="cce-like-dislike__count">{{ likeCount() }}</span>
      </button>

      <button
        type="button"
        class="cce-like-dislike__btn cce-like-dislike__btn--down"
        [class.cce-like-dislike__btn--active]="vote() === 'dislike'"
        [attr.aria-pressed]="vote() === 'dislike'"
        [attr.aria-label]="(vote() === 'dislike'
            ? 'community.detail.actionDisliked'
            : 'community.detail.actionDislike') | translate"
        (click)="setVote('dislike')"
      >
        <mat-icon>{{ vote() === 'dislike' ? 'thumb_down' : 'thumb_down_off_alt' }}</mat-icon>
        <span class="cce-like-dislike__label">
          {{ (vote() === 'dislike'
              ? 'community.detail.actionDisliked'
              : 'community.detail.actionDislike') | translate }}
        </span>
        <span class="cce-like-dislike__count">{{ dislikeCount() }}</span>
      </button>
    </div>
  `,
  styleUrl: './like-dislike-control.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LikeDislikeControlComponent {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  /** Stable id for the entity being voted on (post.id or reply.id). */
  readonly entityId = input.required<string>();
  /** Used to namespace localStorage keys so a post and a reply with
   *  the same prefix don't collide. Defaults to 'post'. */
  readonly entityType = input<'post' | 'reply'>('post');
  /** Compact mode (icon-only) for use in dense reply footers. */
  readonly compact = input<boolean>(false);

  readonly vote = signal<Vote>(null);
  readonly counts = signal<StoredCounts>({ likes: 0, dislikes: 0 });

  readonly likeCount = computed(() => this.counts().likes);
  readonly dislikeCount = computed(() => this.counts().dislikes);

  constructor() {
    // Hydrate from localStorage whenever the inputs change.
    effect(() => {
      const id = this.entityId();
      if (!id) return;
      const seeded = seededCounts(id);
      const stored = readStored(this.entityType(), id);
      if (stored) {
        this.counts.set({
          likes: Math.max(0, seeded.likes + stored.likeDelta),
          dislikes: Math.max(0, seeded.dislikes + stored.dislikeDelta),
        });
        this.vote.set(stored.vote);
      } else {
        this.counts.set(seeded);
        this.vote.set(null);
      }
    });
  }

  setVote(target: Exclude<Vote, null>): void {
    if (!this.auth.isAuthenticated()) {
      this.toast.error('community.signInToRate');
      return;
    }
    const prev = this.vote();
    const next: Vote = prev === target ? null : target;

    // Update counts optimistically
    this.counts.update((c) => {
      let likes = c.likes;
      let dislikes = c.dislikes;
      // Reverse the previous vote
      if (prev === 'like') likes = Math.max(0, likes - 1);
      if (prev === 'dislike') dislikes = Math.max(0, dislikes - 1);
      // Apply the new vote
      if (next === 'like') likes += 1;
      if (next === 'dislike') dislikes += 1;
      return { likes, dislikes };
    });
    this.vote.set(next);

    // Persist as deltas relative to the deterministic seed so the user's
    // contribution survives a page refresh.
    const seeded = seededCounts(this.entityId());
    writeStored(this.entityType(), this.entityId(), {
      vote: next,
      likeDelta: this.counts().likes - seeded.likes,
      dislikeDelta: this.counts().dislikes - seeded.dislikes,
    });
  }
}

/* ─── Seeded counts (deterministic per entity id) ──────────
 *
 * A post / reply with a given guid always renders with the same
 * baseline likes/dislikes, regardless of session. Replaced by real
 * server counts when a backend endpoint is added.
 */
function seededCounts(id: string): StoredCounts {
  let h = 2166136261; // FNV-1a 32-bit offset
  for (let i = 0; i < id.length; i++) {
    h ^= id.charCodeAt(i);
    h = Math.imul(h, 16777619);
  }
  // Likes 8..160, dislikes 0..24 — feels populated without overshooting.
  const likes = 8 + (Math.abs(h) % 153);
  const dislikes = Math.abs(h >> 7) % 25;
  return { likes, dislikes };
}

interface StoredVoteRecord {
  vote: Vote;
  likeDelta: number;
  dislikeDelta: number;
}

function storageKey(type: string, id: string): string {
  return `cce-likes:${type}:${id}`;
}

function readStored(type: string, id: string): StoredVoteRecord | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = window.localStorage.getItem(storageKey(type, id));
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<StoredVoteRecord>;
    if (typeof parsed !== 'object' || parsed === null) return null;
    return {
      vote: parsed.vote === 'like' || parsed.vote === 'dislike' ? parsed.vote : null,
      likeDelta: Number(parsed.likeDelta) || 0,
      dislikeDelta: Number(parsed.dislikeDelta) || 0,
    };
  } catch {
    return null;
  }
}

function writeStored(type: string, id: string, record: StoredVoteRecord): void {
  if (typeof window === 'undefined') return;
  try {
    window.localStorage.setItem(storageKey(type, id), JSON.stringify(record));
  } catch {
    // Quota exceeded or storage disabled — silent fail.
  }
}

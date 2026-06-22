import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { CommunityModerationApiService } from './community-moderation-api.service';
import type { AdminPostDetail, AdminPostReply, AdminPostRow } from './admin-post.types';

export interface PostDetailDialogData {
  row: AdminPostRow;
}

export interface PostDetailDialogResult {
  postDeleted?: boolean;
}

@Component({
  selector: 'cce-post-detail-dialog',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule, MatDialogModule, MatIconModule,
    MatProgressBarModule, MatProgressSpinnerModule, MatTooltipModule,
    TranslocoModule,
  ],
  templateUrl: './post-detail-dialog.component.html',
  styleUrl: './post-detail-dialog.component.scss',
  // Default required: OnPush breaks Transloco across the MatDialog overlay boundary.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class PostDetailDialogComponent implements OnInit {
  private readonly api = inject(CommunityModerationApiService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly localeService = inject(LocaleService);
  private readonly ref = inject(MatDialogRef<PostDetailDialogComponent, PostDetailDialogResult>);

  readonly data = inject<PostDetailDialogData>(MAT_DIALOG_DATA);
  readonly row = this.data.row;
  readonly locale = this.localeService.locale;

  readonly loading = signal(true);
  readonly errorKind = signal<string | null>(null);
  readonly post = signal<AdminPostDetail | null>(null);
  readonly replies = signal<AdminPostReply[]>([]);
  readonly deleting = signal(false);
  readonly deletingReplyId = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    const [postRes, repliesRes] = await Promise.all([
      this.api.getPostDetail(this.row.id),
      this.api.listPostReplies(this.row.id),
    ]);
    this.loading.set(false);
    if (postRes.ok) {
      this.post.set(postRes.value);
    } else {
      this.errorKind.set(postRes.error.kind);
    }
    if (repliesRes.ok) {
      this.replies.set(repliesRes.value);
    }
  }

  topicName(): string {
    const p = this.post();
    const isAr = this.locale() === 'ar';
    if (p) return isAr ? (p.topicNameAr ?? '') : (p.topicNameEn ?? '');
    return isAr ? this.row.topicNameAr : this.row.topicNameEn;
  }

  async deletePost(): Promise<void> {
    if (this.row.isDeleted || this.deleting()) return;
    const confirmed = await this.confirm.confirm({
      titleKey: 'communityModeration.post.title',
      messageKey: 'communityModeration.post.message',
      confirmKey: 'communityModeration.post.confirm',
      cancelKey: 'common.actions.cancel',
    });
    if (!confirmed) return;
    this.deleting.set(true);
    const res = await this.api.softDeletePost(this.row.id);
    this.deleting.set(false);
    if (res.ok) {
      this.toast.success('communityModeration.post.toast');
      this.ref.close({ postDeleted: true });
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }

  async deleteReply(reply: AdminPostReply): Promise<void> {
    if (this.deletingReplyId()) return;
    const confirmed = await this.confirm.confirm({
      titleKey: 'communityModeration.reply.title',
      messageKey: 'communityModeration.reply.message',
      confirmKey: 'communityModeration.reply.confirm',
      cancelKey: 'common.actions.cancel',
    });
    if (!confirmed) return;
    this.deletingReplyId.set(reply.id);
    const res = await this.api.softDeleteReply(reply.id);
    this.deletingReplyId.set(null);
    if (res.ok) {
      this.toast.success('communityModeration.reply.toast');
      this.replies.update((rs) => rs.filter((r) => r.id !== reply.id));
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }

  close(): void {
    this.ref.close();
  }
}

import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { CommunityApiService } from './community-api.service';

/**
 * Author-only "mark this reply as the accepted answer" trigger.
 *
 * The host page enforces visibility (only render when
 * currentUser?.id === post.authorId AND post.isAnswerable AND
 * post.answeredReplyId !== reply.id). Server is the real guard;
 * the UI conditional is convenience.
 */
@Component({
  selector: 'cce-mark-answer-button',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, TranslateModule],
  template: `
    <button
      type="button"
      mat-stroked-button
      color="accent"
      class="cce-mark-answer"
      [disabled]="disabled() || submitting()"
      (click)="onClick()"
    >
      <mat-icon>check_circle</mat-icon>
      {{ 'community.markAnswer.button' | translate }}
    </button>
  `,
  styles: [
    `:host { display: inline-block; }
     .cce-mark-answer { gap: 0.25rem; }`,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MarkAnswerButtonComponent {
  private readonly api = inject(CommunityApiService);
  private readonly toast = inject(ToastService);

  readonly postId = input.required<string>();
  readonly replyId = input.required<string>();
  readonly disabled = input<boolean>(false);

  readonly marked = output<void>();

  readonly submitting = signal(false);

  async onClick(): Promise<void> {
    if (this.disabled() || this.submitting()) return;
    this.submitting.set(true);
    const res = await this.api.markAnswer(this.postId(), this.replyId());
    this.submitting.set(false);
    if (res.ok) {
      this.toast.success('community.markAnswer.toast');
      this.marked.emit();
    } else {
      this.toast.error('errors.' + res.error.kind);
    }
  }
}

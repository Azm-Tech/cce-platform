import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { MarkAnswerButtonComponent } from './mark-answer-button.component';
import type { PublicPostReply } from './community.types';

@Component({
  selector: 'cce-reply',
  standalone: true,
  imports: [CommonModule, DatePipe, MatCardModule, MatIconModule, TranslateModule, MarkAnswerButtonComponent],
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
  /** Re-emitted by the inner MarkAnswerButton so parent pages can refresh. */
  readonly answerMarked = output<void>();

  readonly showLanguageBadge = computed(
    () => this.reply().locale !== this.localeService.locale(),
  );
}

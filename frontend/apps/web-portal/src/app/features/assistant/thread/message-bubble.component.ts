import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import type { ThreadMessage } from '../assistant.types';
import { CitationChipComponent } from './citation-chip.component';

/**
 * Single role-styled message bubble. Renders the content with a
 * blinking streaming cursor while status === 'streaming'. Citation
 * chips render in the footer. Hover-revealed actions: copy, retry
 * (on error), regenerate (on the last successful assistant message).
 */
@Component({
  selector: 'cce-message-bubble',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    TranslateModule,
    CitationChipComponent,
  ],
  templateUrl: './message-bubble.component.html',
  styleUrl: './message-bubble.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MessageBubbleComponent {
  readonly message = input.required<ThreadMessage>();
  /** True when this is the most recent assistant message — controls
   *  whether retry / regenerate buttons are eligible. */
  readonly isLast = input<boolean>(false);

  readonly retry = output<void>();
  readonly regenerate = output<void>();
  readonly copyContent = output<string>();

  readonly isUser = computed(() => this.message().role === 'user');
  readonly isAssistant = computed(() => this.message().role === 'assistant');
  readonly isStreaming = computed(() => this.message().status === 'streaming');
  readonly isError = computed(() => this.message().status === 'error');
  readonly isComplete = computed(() => this.message().status === 'complete');

  /** Retry button shown only on the most recent failed assistant message. */
  readonly showRetry = computed(() => this.isAssistant() && this.isError() && this.isLast());
  /** Regenerate shown on the last successful assistant message. */
  readonly showRegenerate = computed(
    () => this.isAssistant() && this.isComplete() && this.isLast(),
  );

  copyToClipboard(): void {
    this.copyContent.emit(this.message().content);
  }
}

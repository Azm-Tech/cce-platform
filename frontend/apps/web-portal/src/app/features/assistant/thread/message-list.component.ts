import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  ViewChild,
  computed,
  effect,
  inject,
} from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { AssistantStore } from './assistant-store.service';
import { MessageBubbleComponent } from './message-bubble.component';
import { TypingIndicatorComponent } from './typing-indicator.component';

/**
 * Scroll-y message list. Auto-scrolls to the bottom when a new message
 * is added (length increase) — does NOT auto-scroll on every text-chunk
 * update so the bubble grows in place at the bottom of the view.
 *
 * `aria-live="polite"` lets screen readers hear streaming tokens.
 * Empty state pushes the user to the compose box. While the latest
 * message status is 'pending', a TypingIndicator renders below.
 */
@Component({
  selector: 'cce-message-list',
  standalone: true,
  imports: [TranslateModule, MessageBubbleComponent, TypingIndicatorComponent],
  templateUrl: './message-list.component.html',
  styleUrl: './message-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MessageListComponent {
  private readonly store = inject(AssistantStore);

  readonly messages = this.store.messages;
  readonly hasMessages = computed(() => this.messages().length > 0);
  readonly lastMessage = computed(() => {
    const ms = this.messages();
    return ms.length === 0 ? null : ms[ms.length - 1];
  });
  readonly showTyping = computed(() => this.lastMessage()?.status === 'pending');

  @ViewChild('scrollContainer', { static: false })
  private readonly scrollContainer?: ElementRef<HTMLElement>;

  constructor() {
    let lastLength = 0;
    effect(() => {
      const len = this.messages().length;
      if (len > lastLength) {
        // New message added — scroll to bottom on next paint.
        queueMicrotask(() => this.scrollToBottom());
      }
      lastLength = len;
    });
  }

  isLast(index: number): boolean {
    return index === this.messages().length - 1;
  }

  retry(): void {
    void this.store.retry();
  }

  regenerate(): void {
    void this.store.regenerate();
  }

  copy(content: string): void {
    if (typeof navigator !== 'undefined' && navigator.clipboard) {
      void navigator.clipboard.writeText(content);
    }
  }

  private scrollToBottom(): void {
    const el = this.scrollContainer?.nativeElement;
    if (!el) return;
    el.scrollTop = el.scrollHeight;
  }
}

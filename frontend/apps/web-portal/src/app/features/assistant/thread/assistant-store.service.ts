import { Injectable, computed, inject, signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { AssistantApiService } from '../assistant-api.service';
import {
  newMessage,
  type Citation,
  type Role,
  type SseEvent,
  type ThreadMessage,
} from '../assistant.types';

/**
 * Signals-first state container for the assistant thread. Owns the
 * in-memory message list, the streaming flag, and the AbortController
 * for the in-flight stream. Sub-components consume signals and emit
 * actions via this service.
 */
@Injectable()
export class AssistantStore {
  private readonly api = inject(AssistantApiService);
  private readonly localeService = inject(LocaleService);

  readonly messages = signal<ThreadMessage[]>([]);
  readonly streaming = signal<boolean>(false);
  readonly canSend = computed(() => !this.streaming());

  private abortController: AbortController | null = null;

  /**
   * Append a user message + an assistant placeholder, open the SSE
   * stream, route events to the placeholder. Resolves once the stream
   * ends (done | error | cancel).
   */
  async sendMessage(content: string): Promise<void> {
    const trimmed = content.trim();
    if (trimmed === '' || this.streaming()) return;

    const userMsg = newMessage('user', trimmed);
    const assistantMsg = newMessage('assistant', '');
    this.messages.update((prev) => [...prev, userMsg, assistantMsg]);
    this.streaming.set(true);

    const controller = new AbortController();
    this.abortController = controller;

    const wireMessages = this.messages()
      .filter((m) => m.status !== 'pending')
      .map((m): { role: Role; content: string } => ({ role: m.role, content: m.content }));

    try {
      const stream = this.api.query(
        { messages: wireMessages, locale: this.localeService.locale() },
        controller.signal,
      );
      let terminal = false;
      for await (const event of stream) {
        this.applyEvent(assistantMsg.id, event);
        if (event.type === 'done' || event.type === 'error') terminal = true;
      }
      // If the server closed the stream without a terminal event, treat
      // the partial content as complete (lenient default).
      if (!terminal) this.markComplete(assistantMsg.id);
    } catch (err) {
      if (controller.signal.aborted) {
        // User cancelled — keep partial content, mark complete (not error).
        this.markComplete(assistantMsg.id);
      } else {
        this.markError(assistantMsg.id, errorKindOf(err));
      }
    } finally {
      this.streaming.set(false);
      if (this.abortController === controller) this.abortController = null;
    }
  }

  /** Abort the in-flight stream. Partial content is preserved; status
   *  flips to 'complete' (not 'error' — the user chose to stop). */
  cancel(): void {
    this.abortController?.abort();
  }

  /** Re-send the last user message after a failed assistant turn.
   *  Removes the failed assistant message and starts a new turn. */
  async retry(): Promise<void> {
    const msgs = this.messages();
    const last = msgs[msgs.length - 1];
    if (!last || last.role !== 'assistant' || last.status !== 'error') return;
    const lastUser = [...msgs].reverse().find((m) => m.role === 'user');
    if (!lastUser) return;

    // Drop the failed assistant placeholder; sendMessage will re-add user.
    this.messages.update((prev) => prev.slice(0, -2));
    await this.sendMessage(lastUser.content);
  }

  /** Re-stream a fresh reply for the most-recent user message. */
  async regenerate(): Promise<void> {
    const msgs = this.messages();
    const last = msgs[msgs.length - 1];
    if (!last || last.role !== 'assistant' || last.status !== 'complete') return;
    const lastUser = [...msgs].reverse().find((m) => m.role === 'user');
    if (!lastUser) return;
    this.messages.update((prev) => prev.slice(0, -2));
    await this.sendMessage(lastUser.content);
  }

  /** Wipe the thread. */
  clear(): void {
    this.messages.set([]);
  }

  // ─── Internal helpers ───

  private applyEvent(assistantId: string, event: SseEvent): void {
    switch (event.type) {
      case 'text':
        this.appendText(assistantId, event.content);
        break;
      case 'citation':
        this.appendCitation(assistantId, event.citation);
        break;
      case 'done':
        this.markComplete(assistantId);
        break;
      case 'error':
        this.markError(assistantId, event.error.kind);
        break;
    }
  }

  private appendText(id: string, chunk: string): void {
    this.messages.update((prev) =>
      prev.map((m) =>
        m.id === id
          ? { ...m, content: m.content + chunk, status: 'streaming' }
          : m,
      ),
    );
  }

  private appendCitation(id: string, citation: Citation): void {
    this.messages.update((prev) =>
      prev.map((m) =>
        m.id === id ? { ...m, citations: [...m.citations, citation] } : m,
      ),
    );
  }

  private markComplete(id: string): void {
    this.messages.update((prev) =>
      prev.map((m) => (m.id === id ? { ...m, status: 'complete' } : m)),
    );
  }

  private markError(id: string, kind: string): void {
    this.messages.update((prev) =>
      prev.map((m) =>
        m.id === id ? { ...m, status: 'error', errorKind: kind } : m,
      ),
    );
  }
}

function errorKindOf(err: unknown): string {
  if (err instanceof Error) {
    if (err.message.includes('500') || err.message.includes('SSE open failed')) return 'server';
    if (err.name === 'AbortError') return 'aborted';
    return 'unknown';
  }
  return 'unknown';
}

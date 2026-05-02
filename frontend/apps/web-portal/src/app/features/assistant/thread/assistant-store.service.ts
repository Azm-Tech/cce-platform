import { Injectable, computed, signal } from '@angular/core';
import type { ThreadMessage } from '../assistant.types';

/**
 * Signals-first state container for the assistant thread. Phase 02 fills
 * in actions (sendMessage / cancel / retry / regenerate / clear). This
 * stub exists so Phase 00 stubs can import the type without circular refs.
 */
@Injectable()
export class AssistantStore {
  readonly messages = signal<ThreadMessage[]>([]);
  readonly streaming = signal<boolean>(false);
  readonly canSend = computed(() => !this.streaming());
}

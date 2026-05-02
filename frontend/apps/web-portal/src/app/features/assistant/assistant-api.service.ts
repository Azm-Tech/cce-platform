import { Injectable } from '@angular/core';
import type { AssistantQueryRequest, SseEvent } from './assistant.types';

@Injectable({ providedIn: 'root' })
export class AssistantApiService {
  /**
   * Phase 01 wires this to /api/assistant/query via openSseStream.
   * Returns a typed async iterator of SSE events.
   */
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  query(_req: AssistantQueryRequest, _signal: AbortSignal): AsyncIterable<SseEvent> {
    throw new Error('AssistantApiService.query: implemented in Sub-9 Phase 01.');
  }
}

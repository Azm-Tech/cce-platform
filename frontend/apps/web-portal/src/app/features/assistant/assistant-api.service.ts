import { Injectable } from '@angular/core';
import type { AssistantQueryRequest, SseEvent } from './assistant.types';
import { openSseStream } from './lib/sse-client';

@Injectable({ providedIn: 'root' })
export class AssistantApiService {
  query(req: AssistantQueryRequest, signal: AbortSignal): AsyncIterable<SseEvent> {
    return openSseStream('/api/assistant/query', req, signal);
  }
}

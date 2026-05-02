/**
 * Mirrors backend DTOs from CCE.Application.Assistant.* and the SSE
 * wire format from /api/assistant/query (Sub-9 reshape). Threads are
 * client-owned in-memory state — no persistence layer in v0.1.0.
 */

export type Role = 'user' | 'assistant';

export interface Citation {
  id: string;
  kind: 'resource' | 'map-node';
  title: string;
  href: string;
  sourceText?: string;
}

export interface ThreadMessage {
  id: string;
  role: Role;
  content: string;
  citations: Citation[];
  status: 'pending' | 'streaming' | 'complete' | 'error';
  errorKind?: string;
  /** ISO 8601 timestamp set client-side at message creation. */
  createdAt: string;
}

/** Wire-format request body for POST /api/assistant/query. */
export interface AssistantQueryRequest {
  messages: { role: Role; content: string }[];
  locale: 'ar' | 'en';
}

/** SSE event discriminated union. The wire format is `data: <json>\n\n`
 *  per event; the parser maps each event into one of these. */
export type SseEvent =
  | { type: 'text'; content: string }
  | { type: 'citation'; citation: Citation }
  | { type: 'done' }
  | { type: 'error'; error: { kind: string } };

/** Generate a stable-ish unique id. Prefers `crypto.randomUUID()` when
 *  available (browsers + Node 19+); falls back to a Math.random-based
 *  builder so jest-environment-jsdom (which lacks the WebCrypto API by
 *  default) can still run the unit tests. Collision probability is fine
 *  for short-lived in-memory thread state. */
function randomId(): string {
  const c: { randomUUID?: () => string } | undefined =
    typeof globalThis !== 'undefined' ? (globalThis as { crypto?: { randomUUID?: () => string } }).crypto : undefined;
  if (c && typeof c.randomUUID === 'function') {
    return c.randomUUID();
  }
  // RFC 4122 v4-shaped fallback. Not cryptographically secure — that's fine here.
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (ch) => {
    const r = (Math.random() * 16) | 0;
    const v = ch === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

/** Helper to build a ThreadMessage with sensible defaults. Used by the
 *  store so tests don't have to repeat the same boilerplate. */
export function newMessage(role: Role, content: string): ThreadMessage {
  return {
    id: randomId(),
    role,
    content,
    citations: [],
    status: role === 'user' ? 'complete' : 'pending',
    createdAt: new Date().toISOString(),
  };
}

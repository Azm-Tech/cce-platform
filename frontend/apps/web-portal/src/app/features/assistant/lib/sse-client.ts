import type { SseEvent } from '../assistant.types';

const EVENT_DELIMITER = '\n\n';
const DATA_PREFIX = 'data:';

/**
 * Open a server-sent-events stream against `url` with a JSON POST body
 * and yield typed events as they arrive. Honours `signal` for abort.
 *
 * The transport layer assumes:
 *  - Server responds with Content-Type: text/event-stream.
 *  - Each event is exactly `data: <json>\n\n` (no event-id / event-name fields).
 *  - JSON parses to one of the SseEvent shapes.
 *
 * Malformed events are skipped (not thrown) so a single corrupt frame
 * doesn't kill the stream. The store turns the absence of events into
 * its own error state.
 */
export async function* openSseStream(
  url: string,
  body: unknown,
  signal: AbortSignal,
): AsyncGenerator<SseEvent, void, void> {
  const res = await fetch(url, {
    method: 'POST',
    credentials: 'same-origin',
    headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
    body: JSON.stringify(body),
    signal,
  });

  if (!res.ok || !res.body) {
    throw new Error(`SSE open failed: ${res.status}`);
  }

  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  try {
    while (true) {
      const { value, done } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });

      // Drain complete events (separated by \n\n).
      let delimiterIdx = buffer.indexOf(EVENT_DELIMITER);
      while (delimiterIdx !== -1) {
        const rawEvent = buffer.slice(0, delimiterIdx);
        buffer = buffer.slice(delimiterIdx + EVENT_DELIMITER.length);

        const parsed = parseEvent(rawEvent);
        if (parsed) yield parsed;

        delimiterIdx = buffer.indexOf(EVENT_DELIMITER);
      }
    }
  } finally {
    try {
      reader.releaseLock();
    } catch {
      // ignore
    }
  }
}

function parseEvent(raw: string): SseEvent | null {
  // An event may have multiple lines; we only honour `data:` lines.
  const dataLines: string[] = [];
  for (const line of raw.split('\n')) {
    if (line.startsWith(DATA_PREFIX)) {
      dataLines.push(line.slice(DATA_PREFIX.length).trimStart());
    }
  }
  if (dataLines.length === 0) return null;
  const json = dataLines.join('\n');
  try {
    const parsed: unknown = JSON.parse(json);
    if (isValidEvent(parsed)) return parsed;
  } catch {
    return null;
  }
  return null;
}

function isValidEvent(x: unknown): x is SseEvent {
  if (!x || typeof x !== 'object') return false;
  const t = (x as { type?: unknown }).type;
  return t === 'text' || t === 'citation' || t === 'done' || t === 'error';
}

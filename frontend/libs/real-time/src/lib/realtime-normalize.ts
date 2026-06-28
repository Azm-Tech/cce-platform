/**
 * The hub serializes some payloads as PascalCase (named records) and others as
 * camelCase (anonymous objects), and the final casing depends on the SignalR
 * serializer config. To read every payload uniformly we lower-case the first
 * character of each top-level key, mapping `PostId` → `postId` and leaving
 * `postId` unchanged.
 *
 * Payloads are flat objects, so a shallow transform is sufficient.
 */
export function normalizePayload<T>(raw: unknown): T {
  if (raw === null || typeof raw !== 'object' || Array.isArray(raw)) {
    return raw as T;
  }
  const out: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(raw as Record<string, unknown>)) {
    const camel = key.length > 0 ? key.charAt(0).toLowerCase() + key.slice(1) : key;
    // Prefer an existing camelCase key if the payload somehow carries both casings.
    if (!(camel in out)) {
      out[camel] = value;
    }
  }
  return out as T;
}

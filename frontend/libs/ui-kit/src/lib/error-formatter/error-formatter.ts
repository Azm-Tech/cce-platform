import { HttpErrorResponse } from '@angular/common/http';

/** Map FluentValidation 400 ProblemDetails to a per-field error map. */
export function toFieldErrors(err: HttpErrorResponse): Record<string, string[]> {
  if (err.status !== 400) return {};
  const body = err.error;
  if (!body || typeof body !== 'object') return {};
  const errors = (body as { errors?: Record<string, string[]> }).errors;
  if (!errors) return {};
  const normalized: Record<string, string[]> = {};
  for (const [key, msgs] of Object.entries(errors)) {
    const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
    normalized[camelKey] = Array.isArray(msgs) ? msgs : [String(msgs)];
  }
  return normalized;
}

/** Shape of a feature-domain error after wrapper mapping. */
export type FeatureError =
  | { kind: 'concurrency'; message?: string }
  | { kind: 'duplicate'; message?: string }
  | { kind: 'validation'; fieldErrors: Record<string, string[]> }
  | { kind: 'not-found' }
  | { kind: 'forbidden' }
  | { kind: 'server' }
  | { kind: 'network' }
  | { kind: 'unknown'; status: number };

/** Map an HttpErrorResponse to a FeatureError. */
export function toFeatureError(err: HttpErrorResponse): FeatureError {
  if (err.status === 0) return { kind: 'network' };
  if (err.status === 400) return { kind: 'validation', fieldErrors: toFieldErrors(err) };
  if (err.status === 404) return { kind: 'not-found' };
  if (err.status === 403) return { kind: 'forbidden' };
  if (err.status === 409) {
    const type = (err.error as { type?: string })?.type ?? '';
    return type.includes('/duplicate') ? { kind: 'duplicate' } : { kind: 'concurrency' };
  }
  if (err.status >= 500) return { kind: 'server' };
  return { kind: 'unknown', status: err.status };
}

import { HttpErrorResponse } from '@angular/common/http';

/**
 * Maps the CCE API array-based 400 body to a per-field message map.
 * Handles both PascalCase and camelCase field names from the server.
 * Keeps the first error message per field.
 *
 * Expected shape: { errors: [{ field, code, message }] }
 */
export function toApiFieldErrors(err: HttpErrorResponse): Record<string, string> {
  if (err.status !== 400) return {};
  const raw = (err.error as { errors?: { field: string; message: string }[] })?.errors;
  if (!Array.isArray(raw)) return {};
  const result: Record<string, string> = {};
  for (const e of raw) {
    const key = e.field.charAt(0).toLowerCase() + e.field.slice(1);
    result[key] ??= e.message;
  }
  return result;
}

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
  if (err.status === 400) {
    const raw = (err.error as { errors?: unknown })?.errors;
    if (Array.isArray(raw)) {
      const fieldErrors: Record<string, string[]> = {};
      for (const e of raw as { field: string; message: string }[]) {
        const key = e.field.charAt(0).toLowerCase() + e.field.slice(1);
        (fieldErrors[key] ??= []).push(e.message);
      }
      return { kind: 'validation', fieldErrors };
    }
    return { kind: 'validation', fieldErrors: toFieldErrors(err) };
  }
  if (err.status === 404) return { kind: 'not-found' };
  if (err.status === 403) return { kind: 'forbidden' };
  if (err.status === 409) {
    const type = (err.error as { type?: string })?.type ?? '';
    return type.includes('/duplicate') ? { kind: 'duplicate' } : { kind: 'concurrency' };
  }
  if (err.status >= 500) return { kind: 'server' };
  return { kind: 'unknown', status: err.status };
}

import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { map } from 'rxjs';

/**
 * Unwraps the standard CCE API envelope `{ success, data, errors, ... }`
 * so every downstream service receives the raw `data` value directly.
 * Responses that don't match the envelope shape (blobs, plain arrays, etc.)
 * are passed through unchanged.
 */
export const apiEnvelopeInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.includes('/api/admin/')) return next(req);
  return next(req).pipe(
    map(event => {
      if (!(event instanceof HttpResponse)) return event;
      const body = event.body;
      if (
        body !== null &&
        typeof body === 'object' &&
        !Array.isArray(body) &&
        'success' in body &&
        'data' in body
      ) {
        return event.clone({ body: (body as { data: unknown }).data });
      }
      return event;
    }),
  );
};

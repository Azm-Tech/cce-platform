import { HttpInterceptorFn } from '@angular/common/http';
import { isInternalUrl } from './is-internal-url';

const correlationIdHeader = 'X-Correlation-Id';

function newCorrelationId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return `cid-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

export const correlationIdInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.headers.has(correlationIdHeader) || !isInternalUrl(req.url)) {
    return next(req);
  }
  return next(req.clone({ headers: req.headers.set(correlationIdHeader, newCorrelationId()) }));
};

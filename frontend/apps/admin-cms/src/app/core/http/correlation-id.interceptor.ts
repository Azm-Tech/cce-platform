import { HttpInterceptorFn } from '@angular/common/http';

const correlationIdHeader = 'X-Correlation-Id';

function newCorrelationId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return `cid-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

export const correlationIdInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.headers.has(correlationIdHeader)) {
    return next(req);
  }
  const cloned = req.clone({ headers: req.headers.set(correlationIdHeader, newCorrelationId()) });
  return next(cloned);
};

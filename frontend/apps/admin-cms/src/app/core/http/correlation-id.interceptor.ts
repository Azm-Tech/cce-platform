import { HttpInterceptorFn } from '@angular/common/http';

const correlationIdHeader = 'X-Correlation-Id';

function newCorrelationId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return `cid-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

/**
 * Returns true when the request is targeting the CCE backend (same-origin or the
 * configured /api/* path). Cross-origin requests (e.g. Keycloak OIDC discovery,
 * Sentry, KAPSARC) MUST NOT receive the X-Correlation-Id header — those
 * services do not declare it in `Access-Control-Allow-Headers`, so the browser
 * preflight (OPTIONS) is rejected and the GET never fires.
 */
function isInternalUrl(url: string): boolean {
  // Relative URLs (`/api/...`, `/assets/...`) are always same-origin.
  if (url.startsWith('/')) return true;
  // Absolute URLs: only stamp when same origin as the SPA.
  try {
    return new URL(url).origin === window.location.origin;
  } catch {
    return false;
  }
}

export const correlationIdInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.headers.has(correlationIdHeader)) {
    return next(req);
  }
  if (!isInternalUrl(req.url)) {
    return next(req);
  }
  const cloned = req.clone({ headers: req.headers.set(correlationIdHeader, newCorrelationId()) });
  return next(cloned);
};

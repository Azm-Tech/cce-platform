import { HttpInterceptorFn } from '@angular/common/http';
import { isInternalUrl } from './is-internal-url';

/**
 * Sets `withCredentials: true` on same-origin requests so the browser sends
 * the BFF session cookie. Cross-origin requests pass through untouched
 * (would force a credentialed CORS preflight which third-party APIs
 * generally do not allow).
 */
export const bffCredentialsInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isInternalUrl(req.url)) return next(req);
  return next(req.clone({ withCredentials: true }));
};

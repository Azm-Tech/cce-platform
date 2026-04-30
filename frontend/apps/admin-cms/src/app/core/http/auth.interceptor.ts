import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

/**
 * Same-origin or relative-path requests target the CCE backend; everything
 * else (Keycloak discovery, Sentry, KAPSARC) is third-party and must not be
 * tagged with `withCredentials` — that would force a credentialed CORS
 * preflight which Keycloak's discovery endpoint does not allow.
 */
function isInternalUrl(url: string): boolean {
  if (url.startsWith('/')) return true;
  try {
    return new URL(url).origin === window.location.origin;
  } catch {
    return false;
  }
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const cloned = isInternalUrl(req.url)
    ? req.clone({ withCredentials: true })
    : req;
  return next(cloned).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && isInternalUrl(req.url) && !req.url.includes('/api/me')) {
        const returnUrl = encodeURIComponent(router.url);
        window.location.assign(`/auth/login?returnUrl=${returnUrl}`);
      }
      return throwError(() => err);
    }),
  );
};

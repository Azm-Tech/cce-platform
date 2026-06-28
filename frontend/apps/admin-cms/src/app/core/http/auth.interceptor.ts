import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { EMPTY, catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { isInternalUrl } from './is-internal-url';

/**
 * Paths that may legitimately return 401 without requiring a redirect.
 * /api/me   — used on cold-start to probe whether the session is still valid.
 * /api/auth — token-lifecycle endpoints (login, refresh, logout).
 */
const SILENT_401_PATHS = ['/api/me', '/api/auth/'];

/**
 * Dual-purpose auth interceptor:
 *   1. Adds `withCredentials: true` to every same-origin request so the
 *      browser sends BFF session cookies alongside the Bearer token.
 *   2. On 401 from a protected endpoint, attempts a silent token refresh and
 *      retries the original request once. Redirects to login only if the
 *      refresh produces no new access token.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const isInternal = isInternalUrl(req.url);
  const outReq = isInternal ? req.clone({ withCredentials: true }) : req;

  return next(outReq).pipe(
    catchError((err) => {
      if (
        isInternal &&
        err instanceof HttpErrorResponse &&
        err.status === 401 &&
        !SILENT_401_PATHS.some((p) => req.url.includes(p))
      ) {
        const authService = inject(AuthService);
        const router = inject(Router);
        return from(authService.refresh()).pipe(
          switchMap(() => {
            const token = authService.accessToken();
            if (!token) {
              authService.signIn(router.url);
              return EMPTY;
            }
            return next(outReq.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
          }),
        );
      }
      return throwError(() => err);
    }),
  );
};

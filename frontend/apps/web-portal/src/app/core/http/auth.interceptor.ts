import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { EMPTY, catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { isInternalUrl } from './is-internal-url';

/**
 * Paths that may legitimately return 401 without requiring a redirect.
 * /api/me   — used on cold-start to probe whether the session is still valid.
 * /api/auth — token-lifecycle endpoints (login, refresh, logout, register).
 */
const SILENT_401_PATHS = ['/api/me', '/api/auth/'];

/**
 * On 401 from a protected same-origin endpoint, attempts a silent token
 * refresh and retries the original request once with the new token.
 * Redirects to login only if the refresh produces no new access token.
 *
 * withCredentials is handled separately by bffCredentialsInterceptor.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((err) => {
      if (
        isInternalUrl(req.url) &&
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
            return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
          }),
        );
      }
      return throwError(() => err);
    }),
  );
};

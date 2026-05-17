import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { isInternalUrl } from './is-internal-url';

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

import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { EnvironmentInjector, inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '@frontend/ui-kit';

/**
 * Lazy-resolve ToastService only when an error fires. Eager `inject(ToastService)`
 * at the top of the interceptor pulls in TranslateService via ToastService's
 * dependency chain, which transitively needs HttpClient (TranslateLoader factory).
 * If the very first HTTP request is the env.json bootstrap fetch, that creates
 * a circular DI on `ToastService` mid-request. Resolving the toast lazily inside
 * `catchError` defers the lookup until the request completes (and there's an
 * error to surface), breaking the cycle. NG0200 fix.
 */
export const serverErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const injector = inject(EnvironmentInjector);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status >= 500) {
        injector.get(ToastService).error('errors.server');
      } else if (err.status === 403) {
        injector.get(ToastService).error('errors.forbidden');
      }
      return throwError(() => err);
    }),
  );
};

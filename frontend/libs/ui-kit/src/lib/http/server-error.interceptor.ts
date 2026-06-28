import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { EnvironmentInjector, inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../feedback/toast.service';

/** HTTP status codes to suppress from the global error toast for a specific request. */
export const SUPPRESS_ERROR_TOAST = new HttpContextToken<number[]>(() => []);

/**
 * Lazy-resolves ToastService inside catchError to avoid NG0200 cycles when
 * the first request in-flight is the env.json bootstrap fetch (ToastService
 * depends on TranslocoService which depends on HttpClient).
 */
export const serverErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const injector = inject(EnvironmentInjector);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const suppress = req.context.get(SUPPRESS_ERROR_TOAST);
      const toast = injector.get(ToastService);
      if (err.status === 0) {
        toast.error('errors.network');
      } else if (err.status === 403) {
        toast.error('errors.forbidden');
      } else if (err.status === 404 && !suppress.includes(404)) {
        toast.error('errors.not-found');
      } else if (err.status === 429) {
        toast.error('errors.rateLimit');
      } else if (err.status >= 500) {
        toast.error('errors.server');
      }
      return throwError(() => err);
    }),
  );
};

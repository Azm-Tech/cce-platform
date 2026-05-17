import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { EnvironmentInjector, inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../feedback/toast.service';

/**
 * Lazy-resolves ToastService inside catchError to avoid NG0200 cycles when
 * the first request in-flight is the env.json bootstrap fetch (ToastService
 * depends on TranslocoService which depends on HttpClient).
 */
export const serverErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const injector = inject(EnvironmentInjector);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const toast = injector.get(ToastService);
      if (err.status === 0) {
        toast.error('errors.network');
      } else if (err.status === 429) {
        toast.error('errors.rateLimit');
      } else if (err.status >= 500) {
        toast.error('errors.server');
      } else if (err.status === 403) {
        toast.error('errors.forbidden');
      }
      return throwError(() => err);
    }),
  );
};

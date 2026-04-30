import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { EnvironmentInjector, inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '@frontend/ui-kit';

/**
 * Lazy-resolves ToastService inside catchError to avoid NG0200 cycles
 * (Sub-5 admin-cms hit this when env.json bootstrap was the first request).
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

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CceAdminRole } from '@frontend/contracts';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login'], {
      queryParams: state.url && state.url !== '/' ? { returnUrl: state.url } : undefined,
    });
  }

  const isAdmin = auth.hasAnyRole(...(Object.values(CceAdminRole) as CceAdminRole[]));
  if (!isAdmin) {
    return router.createUrlTree(['/login']);
  }

  return true;
};

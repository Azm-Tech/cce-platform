import { CanMatchFn, Route } from '@angular/router';
import { inject } from '@angular/core';
import { CcePermission } from '@frontend/contracts';
import { AuthService } from './auth.service';

export const permissionGuard: CanMatchFn = (route: Route) => {
  const auth = inject(AuthService);
  const required = route.data?.['permission'] as CcePermission | undefined;
  if (!required) return true;
  return auth.hasPermission(required);
};

import { CanMatchFn, Route } from '@angular/router';
import { inject } from '@angular/core';
import { CcePortalRole } from '@frontend/contracts';
import { AuthService } from './auth.service';

/**
 * Blocks lazy routes that require a specific portal role.
 * Usage: canMatch: [roleGuard], data: { role: CcePortalRole.Expert }
 * Unauthenticated users are handled upstream by authGuard.
 */
export const roleGuard: CanMatchFn = (route: Route) => {
  const required = route.data?.['role'] as CcePortalRole | undefined;
  if (!required) return true;
  return inject(AuthService).hasRole(required);
};

import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Boolean-only gate. Public routes do NOT use this — they're left unguarded.
 * On miss, redirects through BFF /auth/login with returnUrl=<originally requested>.
 */
export const authGuard: CanMatchFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  auth.signIn(router.url);
  return false;
};

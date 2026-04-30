import { inject } from '@angular/core';
import {
  ActivatedRouteSnapshot, CanActivateFn, RouterStateSnapshot,
} from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Module-level latch ensuring the cold-start refresh fires at most once
 * per app boot, even when the guard is invoked for several routes
 * before the first call resolves.
 */
let refreshAttempted = false;

/**
 * Resets the cold-start latch. Test-only; production code never resets.
 */
export function _resetAuthGuardForTest(): void {
  refreshAttempted = false;
}

/**
 * Production guard for routes that require an authenticated user.
 *
 * Behavior:
 * - Authenticated -> true.
 * - Not authenticated AND we have not yet attempted a refresh -> awaits
 *   `auth.refresh()` once, then re-checks `isAuthenticated()`. This
 *   covers the cold-start case where the BFF cookie is valid but
 *   `/api/me` has not yet been called this session.
 * - Not authenticated AND refresh has already been attempted -> calls
 *   `auth.signIn(state.url)` so the BFF round-trips through Keycloak
 *   and brings the user back to the originally requested URL, then
 *   returns false so the route doesn't render.
 */
export const authGuard: CanActivateFn = async (
  _route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot,
) => {
  const auth = inject(AuthService);
  if (auth.isAuthenticated()) return true;

  if (!refreshAttempted) {
    refreshAttempted = true;
    await auth.refresh();
    if (auth.isAuthenticated()) return true;
  }

  auth.signIn(state.url);
  return false;
};

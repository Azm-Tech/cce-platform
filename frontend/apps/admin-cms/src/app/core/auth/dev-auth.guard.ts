import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { DevAuthService } from './dev-auth.service';

/**
 * Demo-mode auth guard for admin-cms.
 *
 * Replaces `autoLoginPartialRoutesGuard` from `angular-auth-oidc-client`
 * which required a real OIDC provider (Keycloak / Entra ID) running.
 * In dev mode we use the BFF's `/dev/sign-in?role=...` cookie shim:
 *   • If a `cce-dev-role` cookie is present, allow the route.
 *   • Otherwise redirect to `/dev/sign-in?role=cce-admin&returnUrl=...`
 *     which sets the cookie and bounces back to the requested URL.
 *
 * The cookie itself is HttpOnly (issued by the backend) so we can't read
 * it directly from JS — we use `document.cookie` only to detect the
 * non-HttpOnly companion role cookie. If the cookie isn't there, we
 * trigger a sign-in redirect.
 */
export const devAuthGuard: CanActivateFn = (_route, state) => {
  const devAuth = inject(DevAuthService);
  const router = inject(Router);

  if (devAuth.hasSession()) return true;

  // Kick the user through the BFF dev sign-in flow with the platform-admin
  // role; the backend sets the cookie and redirects to `returnUrl`.
  const returnUrl = state.url || '/profile';
  window.location.assign(
    `/dev/sign-in?role=cce-admin&returnUrl=${encodeURIComponent(returnUrl)}`,
  );
  // Block this navigation; the redirect above will take over.
  return router.parseUrl('/');
};

import { Injectable, signal, computed } from '@angular/core';

/**
 * Cookie-based dev-mode auth state for admin-cms.
 *
 * Reads the `cce-dev-role` cookie set by the BFF's `/dev/sign-in` shim.
 * Exposes a synchronous `hasSession()` for guards and a reactive
 * signal `currentRole` for the toolbar.
 *
 * Replaces the OIDC-token-based session handling that used to live in
 * `OidcSecurityService` (which required a Keycloak / Entra ID provider).
 */
@Injectable({ providedIn: 'root' })
export class DevAuthService {
  private readonly _role = signal<string | null>(this.readRoleCookie());

  readonly currentRole = this._role.asReadonly();
  readonly isAuthenticated = computed(() => this._role() !== null);
  readonly displayLabel = computed(() => {
    const role = this._role();
    if (!role) return '';
    switch (role) {
      case 'cce-admin':    return 'Platform Admin';
      case 'cce-editor':   return 'CMS Editor';
      case 'cce-reviewer': return 'Reviewer';
      case 'cce-expert':   return 'Verified Expert';
      case 'cce-user':     return 'End User';
      default:             return role;
    }
  });

  hasSession(): boolean {
    return this.readRoleCookie() !== null;
  }

  signIn(role: string = 'cce-admin', returnUrl: string = '/'): void {
    window.location.assign(
      `/dev/sign-in?role=${encodeURIComponent(role)}&returnUrl=${encodeURIComponent(returnUrl)}`,
    );
  }

  signOut(): void {
    // Expire the cookie client-side and bounce to /
    document.cookie = 'cce-dev-role=; Path=/; Max-Age=0';
    this._role.set(null);
    window.location.assign('/');
  }

  /** Refresh internal state from the cookie (e.g. after navigation). */
  refresh(): void {
    this._role.set(this.readRoleCookie());
  }

  private readRoleCookie(): string | null {
    if (typeof document === 'undefined') return null;
    const m = document.cookie.match(/(?:^|;\s*)cce-dev-role=([^;]+)/);
    return m ? decodeURIComponent(m[1]) : null;
  }
}

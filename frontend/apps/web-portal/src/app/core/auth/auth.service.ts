import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthApiService, AuthUser, TokenPair } from './auth-api.service';
import { ToastService } from '@frontend/ui-kit';
import { CcePortalRole } from '@frontend/contracts';

export type CurrentUser = AuthUser;

const REFRESH_TOKEN_KEY = 'cce_rt';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authApi = inject(AuthApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  private readonly _currentUser = signal<CurrentUser | null>(null);
  private readonly _accessToken = signal<string | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly accessToken = this._accessToken.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);
  readonly roles = computed(() => this._currentUser()?.roles ?? []);

  hasRole(role: CcePortalRole): boolean {
    return this._currentUser()?.roles.includes(role) ?? false;
  }

  hasAnyRole(...roles: CcePortalRole[]): boolean {
    return roles.some((r) => this.hasRole(r));
  }

  setSession(tokens: TokenPair): void {
    this._accessToken.set(tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
    this._currentUser.set(tokens.user);
  }

  /** Bootstraps session from stored refresh token. Called from APP_INITIALIZER. */
  async refresh(): Promise<void> {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    if (!refreshToken) {
      this._currentUser.set(null);
      return;
    }
    try {
      const tokens = await firstValueFrom(this.authApi.refresh(refreshToken));
      this.setSession(tokens);
    } catch {
      this._accessToken.set(null);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      this._currentUser.set(null);
    }
  }

  /** Navigates to the login page, preserving returnUrl for post-login redirect. */
  signIn(returnUrl: string = '/'): void {
    void this.router.navigate(['/login'], {
      queryParams: returnUrl && returnUrl !== '/login' ? { returnUrl } : undefined,
    });
  }

  async signOut(): Promise<void> {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    try {
      if (refreshToken) {
        await firstValueFrom(this.authApi.logout(refreshToken));
      }
      this.toast.success('account.logout.successMessage');
    } catch {
      this.toast.error('account.logout.errorMessage');
    } finally {
      this._accessToken.set(null);
      this._currentUser.set(null);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      void this.router.navigate(['/']);
    }
  }

  /** Public test helper. Not for application code. */
  _setUserForTest(user: CurrentUser | null): void {
    this._currentUser.set(user);
  }
}

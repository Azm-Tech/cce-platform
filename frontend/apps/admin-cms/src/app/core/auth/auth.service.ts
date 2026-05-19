import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthApiService, AuthUser, TokenPair } from './auth-api.service';
import { ToastService } from '@frontend/ui-kit';

export interface CurrentUser extends AuthUser {
  permissions: readonly string[];
}

const PERMISSIONS_BY_ROLE: Record<string, readonly string[]> = {
  'cce-admin': [
    'User.Read', 'Role.Assign',
    'Community.Expert.ApproveRequest', 'Community.Post.Moderate',
    'Resource.Center.Upload', 'Resource.Country.Approve',
    'News.Update', 'Event.Manage',
    'Page.Edit',
    'Country.Profile.Update',
    'Notification.TemplateManage',
    'Report.UserRegistrations',
    'Audit.Read',
    'Translation.Manage', 'Settings.Manage',
  ],
  'cce-editor': [
    'Resource.Center.Upload', 'News.Update', 'Event.Manage',
    'Page.Edit', 'User.Read', 'Translation.Manage',
  ],
  'cce-reviewer': [
    'Resource.Country.Approve', 'Community.Post.Moderate',
    'Community.Expert.ApproveRequest', 'User.Read',
  ],
  'cce-expert': ['User.Read'],
  'cce-user': [],
};

const REFRESH_TOKEN_KEY = 'cce_admin_rt';

function derivePermissions(roles: string[]): readonly string[] {
  const perms = new Set<string>();
  for (const role of roles) {
    for (const perm of PERMISSIONS_BY_ROLE[role] ?? []) {
      perms.add(perm);
    }
  }
  return [...perms];
}

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

  setSession(tokens: TokenPair): void {
    this._accessToken.set(tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
    this._currentUser.set({
      ...tokens.user,
      permissions: derivePermissions(tokens.user.roles),
    });
  }

  hasPermission(permission: string): boolean {
    return this._currentUser()?.permissions.includes(permission) ?? false;
  }

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
      void this.router.navigate(['/login']);
    }
  }

  /** Public test helper. Not for application code. */
  _setUserForTest(user: CurrentUser | null): void {
    this._currentUser.set(user);
  }
}

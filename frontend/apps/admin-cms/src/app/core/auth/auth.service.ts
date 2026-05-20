import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthApiService, AuthUser, TokenPair } from './auth-api.service';
import { ToastService } from '@frontend/ui-kit';
import { CceAdminRole, CcePermission, CcePortalRole } from '@frontend/contracts';

export interface CurrentUser extends AuthUser {
  permissions: readonly CcePermission[];
}

const ALL_PERMISSIONS: readonly CcePermission[] = [
  CcePermission.UserRead,
  CcePermission.RoleAssign,
  CcePermission.CommunityExpertApprove,
  CcePermission.CommunityPostModerate,
  CcePermission.ResourceCenterUpload,
  CcePermission.ResourceCountryApprove,
  CcePermission.NewsUpdate,
  CcePermission.EventManage,
  CcePermission.PageEdit,
  CcePermission.CountryProfileUpdate,
  CcePermission.NotificationTemplateManage,
  CcePermission.ReportUserRegistrations,
  CcePermission.AuditRead,
  CcePermission.TranslationManage,
  CcePermission.SettingsManage,
];

const PERMISSIONS_BY_ROLE: Record<CceAdminRole, readonly CcePermission[]> = {
  [CceAdminRole.SuperAdmin]: ALL_PERMISSIONS,
  [CceAdminRole.Admin]:      ALL_PERMISSIONS,
  [CceAdminRole.ContentManager]: [
    CcePermission.ResourceCenterUpload,
    CcePermission.NewsUpdate,
    CcePermission.EventManage,
    CcePermission.PageEdit,
    CcePermission.CountryProfileUpdate,
    CcePermission.TranslationManage,
  ],
  [CceAdminRole.StateRepresentative]: [
    CcePermission.ResourceCountryApprove,
    CcePermission.CountryProfileUpdate,
  ],
};

const REFRESH_TOKEN_KEY = 'cce_admin_rt';

function derivePermissions(roles: (CceAdminRole | CcePortalRole)[]): readonly CcePermission[] {
  const perms = new Set<CcePermission>();
  for (const role of roles) {
    for (const perm of PERMISSIONS_BY_ROLE[role as CceAdminRole] ?? []) {
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
  readonly roles = computed(() => this._currentUser()?.roles ?? []);

  hasRole(role: CceAdminRole): boolean {
    return this._currentUser()?.roles.includes(role) ?? false;
  }

  hasAnyRole(...roles: CceAdminRole[]): boolean {
    return roles.some((r) => this.hasRole(r));
  }

  setSession(tokens: TokenPair): void {
    this._accessToken.set(tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
    this._currentUser.set({
      ...tokens.user,
      permissions: derivePermissions(tokens.user.roles),
    });
  }

  hasPermission(permission: CcePermission): boolean {
    return this._currentUser()?.permissions.includes(permission) ?? false;
  }

  hasAnyPermission(...permissions: CcePermission[]): boolean {
    return permissions.some((p) => this.hasPermission(p));
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

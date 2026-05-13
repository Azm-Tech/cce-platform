import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface CurrentUser {
  id: string;
  email: string;
  userName: string;
  permissions: readonly string[];
}

/**
 * Dev-mode role → permission map. Mirrors what the production backend
 * would compute server-side (per ADR / permissions.yaml). Used by
 * `AuthService.refresh()` when the admin backend doesn't expose a
 * `/api/me` endpoint, so we can derive permissions client-side from
 * the `cce-dev-role` cookie set by `/dev/sign-in?role=...`.
 *
 * Platform Admin gets every permission referenced by the nav-config;
 * other roles get scoped subsets so each persona sees a meaningful
 * but limited menu.
 */
const ALL_NAV_PERMISSIONS = [
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
] as const;

const DEV_PERMISSIONS_BY_ROLE: Record<string, readonly string[]> = {
  // Platform admin gets everything, including Translations + Settings.
  'cce-admin': ALL_NAV_PERMISSIONS,
  // CMS editor: content + page operations + translations (so they can
  // localize new content without admin approval).
  'cce-editor': [
    'Resource.Center.Upload', 'News.Update', 'Event.Manage',
    'Page.Edit', 'User.Read', 'Translation.Manage',
  ],
  // Reviewer: moderation tasks only — no settings/translations.
  'cce-reviewer': [
    'Resource.Country.Approve', 'Community.Post.Moderate',
    'Community.Expert.ApproveRequest', 'User.Read',
  ],
  'cce-expert': ['User.Read'],
  'cce-user': [],
};

const DEV_USER_INFO_BY_ROLE: Record<string, { email: string; userName: string; id: string }> = {
  'cce-admin':    { id: 'aaaaaaaa-aaaa-aaaa-aaaa-000000000001', email: 'cce-admin@cce.local',    userName: 'cce-admin' },
  'cce-editor':   { id: 'aaaaaaaa-aaaa-aaaa-aaaa-000000000002', email: 'cce-editor@cce.local',   userName: 'cce-editor' },
  'cce-reviewer': { id: 'aaaaaaaa-aaaa-aaaa-aaaa-000000000003', email: 'cce-reviewer@cce.local', userName: 'cce-reviewer' },
  'cce-expert':   { id: 'aaaaaaaa-aaaa-aaaa-aaaa-000000000004', email: 'cce-expert@cce.local',   userName: 'cce-expert' },
  'cce-user':     { id: 'aaaaaaaa-aaaa-aaaa-aaaa-000000000005', email: 'cce-user@cce.local',     userName: 'cce-user' },
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly _currentUser = signal<CurrentUser | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);

  /**
   * Bootstraps the user. Tries `/api/me` first; falls back to deriving
   * the user + permissions from the `cce-dev-role` cookie set by the
   * BFF dev-sign-in shim (the admin backend doesn't ship /api/me yet).
   * Call from APP_INITIALIZER.
   */
  async refresh(): Promise<void> {
    // Try the real endpoint first (future-proofs against the day the
    // admin backend ships /api/me).
    try {
      const me = await firstValueFrom(this.http.get<CurrentUser>('/api/me'));
      this._currentUser.set(me);
      return;
    } catch {
      // fall through to cookie-derived user
    }
    const role = this.readDevRoleCookie();
    if (!role) {
      this._currentUser.set(null);
      return;
    }
    const info = DEV_USER_INFO_BY_ROLE[role];
    const permissions = DEV_PERMISSIONS_BY_ROLE[role] ?? [];
    if (!info) {
      this._currentUser.set(null);
      return;
    }
    this._currentUser.set({
      id: info.id,
      email: info.email,
      userName: info.userName,
      permissions,
    });
  }

  hasPermission(permission: string): boolean {
    const u = this._currentUser();
    return u?.permissions.includes(permission) ?? false;
  }

  /** Public test helper — explicitly set the user. Not for application code; AuthService.refresh() is the prod path. */
  _setUserForTest(user: CurrentUser | null): void {
    this._currentUser.set(user);
  }

  signOut(): void {
    this._currentUser.set(null);
    // Expire the dev cookie client-side, then bounce home.
    document.cookie = 'cce-dev-role=; Path=/; Max-Age=0';
    window.location.assign('/');
  }

  private readDevRoleCookie(): string | null {
    if (typeof document === 'undefined') return null;
    const m = document.cookie.match(/(?:^|;\s*)cce-dev-role=([^;]+)/);
    return m ? decodeURIComponent(m[1]) : null;
  }
}

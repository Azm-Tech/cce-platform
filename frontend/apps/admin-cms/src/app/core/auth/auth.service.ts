import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface CurrentUser {
  id: string;
  email: string;
  userName: string;
  permissions: readonly string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly _currentUser = signal<CurrentUser | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);

  /** Bootstraps the user from /api/me. Call from APP_INITIALIZER. */
  async refresh(): Promise<void> {
    try {
      const me = await firstValueFrom(this.http.get<CurrentUser>('/api/me'));
      this._currentUser.set(me);
    } catch {
      this._currentUser.set(null);
    }
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
    window.location.assign('/auth/logout');
  }
}

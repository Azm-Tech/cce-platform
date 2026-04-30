import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface CurrentUser {
  id: string;
  email: string | null;
  userName: string | null;
  displayNameAr: string | null;
  displayNameEn: string | null;
  avatarUrl: string | null;
  countryId: string | null;
  isExpert: boolean;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly _currentUser = signal<CurrentUser | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);

  /** Bootstraps from /api/me. Tolerates 401 (anonymous) silently. Call from APP_INITIALIZER. */
  async refresh(): Promise<void> {
    try {
      const me = await firstValueFrom(this.http.get<CurrentUser>('/api/me'));
      this._currentUser.set(me);
    } catch {
      this._currentUser.set(null);
    }
  }

  /** Public test helper. Not for application code. */
  _setUserForTest(user: CurrentUser | null): void {
    this._currentUser.set(user);
  }

  /** Full-page navigation to BFF login. SPA does NOT exchange tokens itself. */
  signIn(returnUrl: string = window.location.pathname + window.location.search): void {
    window.location.assign(`/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`);
  }

  /** Full-page POST is overkill; use a tiny form to satisfy POST + browser redirect. */
  async signOut(): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/auth/logout', {}));
    } finally {
      this._currentUser.set(null);
      window.location.assign('/');
    }
  }
}

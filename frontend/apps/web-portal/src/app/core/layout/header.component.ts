import { ChangeDetectionStrategy, Component, OnDestroy, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../auth/auth.service';
import { LocaleSwitcherComponent } from '../../locale-switcher/locale-switcher.component';
import { NotificationsApiService } from '../../features/notifications/notifications-api.service';
import { NotificationsDrawerComponent } from '../../features/notifications/notifications-drawer.component';
import { SearchBoxComponent } from './search-box.component';
import { PRIMARY_NAV } from './nav-config';

const UNREAD_POLL_MS = 60_000;

@Component({
  selector: 'cce-header',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive,
    MatBadgeModule, MatButtonModule, MatIconModule, MatMenuModule,
    TranslateModule, LocaleSwitcherComponent, SearchBoxComponent,
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderComponent implements OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly notificationsApi = inject(NotificationsApiService);

  readonly nav = PRIMARY_NAV;
  readonly mobileMenuOpen = signal(false);
  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly userLabel = computed(() => {
    const u = this.auth.currentUser();
    return u?.displayNameEn ?? u?.userName ?? u?.email ?? '';
  });
  readonly unreadCount = signal(0);
  readonly badgeHidden = computed(() => this.unreadCount() === 0);

  private pollTimer: ReturnType<typeof setInterval> | null = null;

  constructor() {
    // Start / stop the unread-count poll based on auth state.
    effect(() => {
      if (this.isAuthenticated()) {
        void this.refreshUnreadCount();
        this.startPoll();
      } else {
        this.stopPoll();
        this.unreadCount.set(0);
      }
    });
  }

  ngOnDestroy(): void {
    this.stopPoll();
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update((v) => !v);
  }

  signIn(): void { this.auth.signIn(); }
  async signOut(): Promise<void> { await this.auth.signOut(); }

  openNotifications(): void {
    const ref = this.dialog.open<NotificationsDrawerComponent, void, void>(
      NotificationsDrawerComponent,
      {
        position: { right: '0', top: '0' },
        width: '420px',
        height: '100vh',
        panelClass: 'cce-notifications-dialog',
        autoFocus: 'first-tabbable',
      },
    );
    ref.componentInstance.unreadCountChange.subscribe((n) => this.unreadCount.set(n));
    void ref.componentInstance.refresh();
  }

  private async refreshUnreadCount(): Promise<void> {
    const res = await this.notificationsApi.getUnreadCount();
    if (res.ok) this.unreadCount.set(res.value);
  }

  private startPoll(): void {
    if (this.pollTimer) return;
    this.pollTimer = setInterval(() => void this.refreshUnreadCount(), UNREAD_POLL_MS);
  }

  private stopPoll(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }
}

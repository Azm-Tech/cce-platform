import { ChangeDetectionStrategy, Component, HostListener, OnDestroy, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { TranslateModule } from '@ngx-translate/core';
import { filter } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { LocaleSwitcherComponent } from '../../locale-switcher/locale-switcher.component';
import { NotificationsApiService } from '../../features/notifications/notifications-api.service';
import { NotificationsDrawerComponent } from '../../features/notifications/notifications-drawer.component';
import { SearchBoxComponent } from './search-box.component';
import { PRIMARY_NAV, type NavGroup, type NavLink, type PrimaryNavItem } from './nav-config';

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
  private readonly router = inject(Router);

  readonly nav = PRIMARY_NAV;
  readonly mobileMenuOpen = signal(false);

  /** Currently-open mega-menu group id, or null when no panel is open.
   *  Null means no panel is showing. */
  readonly openGroupId = signal<string | null>(null);
  /** Route of the currently-hovered child link inside the open mega-
   *  menu — drives which preview image renders in the right pane. */
  readonly hoveredChildRoute = signal<string | null>(null);
  private hoverCloseTimer: ReturnType<typeof setTimeout> | null = null;

  /** Resolve the active mega-menu's preview image based on which child
   *  the user is hovering. Falls back to the group's first child so
   *  the panel always has a picture even before the user hovers. */
  previewImageFor(group: NavGroup): string | null {
    const route = this.hoveredChildRoute();
    const target =
      group.children.find((c) => c.route === route) ??
      group.children[0];
    return target?.previewImage ?? null;
  }
  /** Resolve the title/description of the currently-previewed child. */
  previewLinkFor(group: NavGroup): NavLink | null {
    const route = this.hoveredChildRoute();
    return group.children.find((c) => c.route === route) ?? group.children[0] ?? null;
  }

  /** Type-guard for *ngIf in templates. */
  isGroup(item: PrimaryNavItem): item is NavGroup {
    return item.kind === 'group';
  }
  isLink(item: PrimaryNavItem): item is NavLink {
    return item.kind === 'link';
  }

  /** Open the mega-menu panel for a group (click or hover). */
  openGroup(group: NavGroup): void {
    if (this.hoverCloseTimer) {
      clearTimeout(this.hoverCloseTimer);
      this.hoverCloseTimer = null;
    }
    this.openGroupId.set(group.id);
  }

  /** Close the mega-menu panel. Optional small delay used by hover-out
   *  so the user can move the mouse from the trigger to the panel
   *  without the panel disappearing mid-traversal. */
  closeGroup(immediate = false): void {
    if (this.hoverCloseTimer) {
      clearTimeout(this.hoverCloseTimer);
      this.hoverCloseTimer = null;
    }
    if (immediate) {
      this.openGroupId.set(null);
      return;
    }
    this.hoverCloseTimer = setTimeout(() => {
      this.openGroupId.set(null);
      this.hoverCloseTimer = null;
    }, 140);
  }

  /** Click on the trigger always pins the panel open. Hover-out can't
   *  close it once it has been clicked — only outside-click, Esc, or
   *  a navigation will close it. (Without this guard, mouseenter would
   *  open the panel a fraction of a second before click fires, and the
   *  old toggle behavior would then close it again — making the click
   *  feel like a no-op on desktop.) */
  toggleGroup(group: NavGroup): void {
    this.openGroup(group);
  }

  /** True if the route currently matches one of the group's children
   *  — used to apply the brand-active style to the trigger button. */
  isGroupActive(group: NavGroup): boolean {
    const url = this.router.url.split('?')[0];
    return group.children.some((c) =>
      c.route === '/' ? url === '/' : url === c.route || url.startsWith(c.route + '/'),
    );
  }
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

    // Auto-close mobile menu AND any open mega-menu after every
    // successful navigation. Covers nav-link clicks, programmatic
    // navigation, browser back/forward, and any other source.
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => {
        this.mobileMenuOpen.set(false);
        this.openGroupId.set(null);
        this.hoveredChildRoute.set(null);
      });
  }

  ngOnDestroy(): void {
    this.stopPoll();
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update((v) => !v);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }

  /** Esc closes any open menu — mega-menu has priority since it's
   *  more transient than the mobile menu. */
  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.openGroupId() !== null) {
      this.closeGroup(true);
      return;
    }
    if (this.mobileMenuOpen()) this.closeMobileMenu();
  }

  /** Click outside the header (on the document body) closes any
   *  open menus. Anchored to mousedown so the menu closes BEFORE
   *  the click activates whatever's underneath. */
  @HostListener('document:mousedown', ['$event'])
  onDocumentMousedown(event: MouseEvent): void {
    if (!this.mobileMenuOpen() && this.openGroupId() === null) return;
    const target = event.target as Element | null;
    if (target && !target.closest('cce-header')) {
      this.closeMobileMenu();
      this.closeGroup(true);
    }
  }

  /**
   * Header "Sign in" button → SPA login screen at /login. The login
   * page captures `returnUrl` so after successful sign-in the user
   * lands back on whatever page they came from.
   */
  signIn(): void {
    const returnUrl = window.location.pathname + window.location.search;
    void this.router.navigate(['/login'], {
      queryParams: returnUrl && returnUrl !== '/login' ? { returnUrl } : undefined,
    });
  }
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

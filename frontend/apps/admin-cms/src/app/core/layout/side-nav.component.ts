import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../auth/auth.service';
import { PermissionDirective } from '../auth/permission.directive';
import { NAV_GROUPS, NavGroup } from './nav-config';

/**
 * Admin sidebar — categorized nav. Sections are derived from
 * `NAV_GROUPS` in nav-config; each section is hidden entirely when
 * the current user has no permission for any of its items (avoids
 * orphan section headings for limited-permission users).
 */
@Component({
  selector: 'cce-side-nav',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    MatListModule,
    MatIconModule,
    TranslateModule,
    PermissionDirective,
  ],
  templateUrl: './side-nav.component.html',
  styleUrl: './side-nav.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SideNavComponent {
  private readonly auth = inject(AuthService);

  /** All groups. Per-item permission gates are handled by the
   *  `*ccePermission` structural directive on each row; this
   *  `visibleGroups` signal additionally hides the *section
   *  heading* when no row inside it is visible. */
  readonly visibleGroups = computed<readonly NavGroup[]>(() => {
    // Subscribe to the auth user signal so this recomputes after sign-in.
    this.auth.currentUser();
    return NAV_GROUPS.filter((group) =>
      group.items.some((item) => this.auth.hasPermission(item.permission)),
    );
  });
}

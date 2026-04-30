import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { ToastService } from '../../core/ui/toast.service';
import type { FeatureError } from '../../core/ui/error-formatter';
import { IdentityApiService } from './identity-api.service';
import { RoleAssignDialogComponent, type RoleAssignDialogData } from './role-assign.dialog';
import type { UserDetail } from './identity.types';

/**
 * Admin → User detail page. Read-only profile card + role-assign action.
 * Profile editing remains the user's own concern via /api/me; the admin
 * page exposes role management only (gated by `Role.Assign`).
 */
@Component({
  selector: 'cce-user-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    TranslateModule,
    PermissionDirective,
  ],
  templateUrl: './user-detail.page.html',
  styleUrl: './user-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserDetailPage implements OnInit {
  private readonly api = inject(IdentityApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  readonly user = signal<UserDetail | null>(null);
  readonly loading = signal(false);
  readonly error = signal<FeatureError | null>(null);

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set({ kind: 'not-found' });
      return;
    }
    await this.load(id);
  }

  async load(id: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    const res = await this.api.getUser(id);
    this.loading.set(false);
    if (res.ok) this.user.set(res.value);
    else this.error.set(res.error);
  }

  async openRoleAssign(): Promise<void> {
    const u = this.user();
    if (!u) return;
    const data: RoleAssignDialogData = { userId: u.id, currentRoles: [...u.roles] };
    const ref = this.dialog.open(RoleAssignDialogComponent, { data, width: '480px' });
    const updated = await firstValueFrom(ref.afterClosed());
    if (updated) {
      this.user.set(updated);
      this.toast.success('roleAssign.saved');
    }
  }
}

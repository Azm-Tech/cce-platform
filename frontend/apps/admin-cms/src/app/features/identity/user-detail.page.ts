
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import type { FeatureError } from '@frontend/ui-kit';
import { RoleLabelPipe } from './role-label.pipe';
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
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    TranslocoModule,
    PermissionDirective,
    RoleLabelPipe,
  ],
  templateUrl: './user-detail.page.html',
  styleUrl: './user-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserDetailPage implements OnInit {
  private readonly api = inject(IdentityApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmDialogService);

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

  async deleteUser(): Promise<void> {
    const u = this.user();
    if (!u) return;
    const confirmed = await this.confirm.confirm({
      titleKey: 'users.delete.confirmTitle',
      messageKey: 'users.delete.confirmMessage',
      confirmKey: 'users.delete.confirmButton',
      cancelKey: 'common.actions.cancel',
    });
    if (!confirmed) return;
    const res = await this.api.deleteUser(u.id);
    if (res.ok) {
      this.toast.success('users.delete.successToast');
      void this.router.navigate(['/users']);
    } else {
      this.toast.error('users.delete.errorGeneric');
    }
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

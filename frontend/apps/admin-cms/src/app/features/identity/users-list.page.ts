
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import {
  PagedTableColumn,
  PagedTableComponent,
  PagedTablePageChange,
  ToastService,
} from '@frontend/ui-kit';
import { UserCreateDialogComponent } from './user-create.dialog';
import { IdentityApiService } from './identity-api.service';
import { KNOWN_ROLE_OPTIONS, type UserListItem } from './identity.types';
import { RoleLabelPipe } from './role-label.pipe';

/**
 * Admin → Users list page. Paged Material table with search + role filter.
 * Each row links to {@link UserDetailPage}. Permission gate is applied at
 * route-level via permissionGuard ('User.Read').
 */
@Component({
  selector: 'cce-users-list',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    TranslocoModule,
    PagedTableComponent,
  ],
  templateUrl: './users-list.page.html',
  styleUrl: './users-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersListPage implements OnInit {
  private readonly api = inject(IdentityApiService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslocoService);
  private readonly rolePipe = new RoleLabelPipe();

  readonly searchInput = signal('');
  readonly roleFilter = signal<string>('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<UserListItem[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly knownRoleOptions = KNOWN_ROLE_OPTIONS;

  readonly columns: PagedTableColumn<UserListItem>[] = [
    { key: 'userName', labelKey: 'users.col.userName', cell: (r) => r.userName ?? '—' },
    { key: 'email', labelKey: 'users.col.email', cell: (r) => r.email ?? '—' },
    { key: 'roles', labelKey: 'users.col.roles', cell: (r) => r.roles.length
        ? r.roles.map(v => this.translate.translate(this.rolePipe.transform(v))).join(', ')
        : '—' },
    { key: 'isActive', labelKey: 'users.col.isActive', cell: (r) => (r.isActive ? '✓' : '✗') },
  ];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listUsers({
      page: this.page(),
      pageSize: this.pageSize(),
      search: this.searchInput() || undefined,
      role: this.roleFilter() || undefined,
    });
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value.items);
      this.total.set(Number(res.value.total));
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  onPage(e: PagedTablePageChange): void {
    this.page.set(e.page);
    this.pageSize.set(e.pageSize);
    void this.load();
  }

  onSearch(): void {
    this.page.set(1);
    void this.load();
  }

  onRoleFilter(value: string): void {
    this.roleFilter.set(value);
    this.page.set(1);
    void this.load();
  }

  onRowClick(user: UserListItem): void {
    void this.router.navigate(['/users', user.id]);
  }

  openCreate(): void {
    const ref = this.dialog.open(UserCreateDialogComponent, { width: '560px', disableClose: true });
    ref.afterClosed().subscribe((created: UserListItem | null) => {
      if (created) {
        this.toast.success('users.create.successToast');
        this.page.set(1);
        void this.load();
      }
    });
  }
}

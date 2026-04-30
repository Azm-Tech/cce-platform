import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/permission.guard';

/**
 * Identity admin feature routes — lazy-loaded under `/users`.
 * `/users` → UsersListPage (Task 1.1)
 * `/users/:id` → UserDetailPage (added in Task 1.2)
 */
export const IDENTITY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./users-list.page').then((m) => m.UsersListPage),
    data: { permission: 'User.Read' },
    canMatch: [permissionGuard],
  },
];

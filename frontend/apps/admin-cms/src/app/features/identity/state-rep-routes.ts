import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/permission.guard';

/**
 * State-rep assignment routes — lazy-loaded under `/state-rep-assignments`.
 * Single-screen feature for v0.1.0 (list page handles create + revoke via dialogs).
 */
export const STATE_REP_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./state-rep-list.page').then((m) => m.StateRepListPage),
    data: { permission: 'Role.Assign' },
    canMatch: [permissionGuard],
  },
];

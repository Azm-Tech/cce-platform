import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/permission.guard';

export const REPORTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./reports.page').then((m) => m.ReportsPage),
    // Landing-page-level gate uses the broadest report permission;
    // each card is also gated individually via *ccePermission.
    data: { permission: 'Report.UserRegistrations' },
    canMatch: [permissionGuard],
  },
];

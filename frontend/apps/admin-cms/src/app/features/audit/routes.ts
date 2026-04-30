import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/permission.guard';

export const AUDIT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./audit.page').then((m) => m.AuditPage),
    data: { permission: 'Audit.Read' },
    canMatch: [permissionGuard],
  },
];

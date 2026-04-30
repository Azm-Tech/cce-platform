import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/permission.guard';

export const NOTIFICATIONS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./notifications-list.page').then((m) => m.NotificationsListPage),
    data: { permission: 'Notification.TemplateManage' },
    canMatch: [permissionGuard],
  },
];

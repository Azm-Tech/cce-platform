import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/permission.guard';

/**
 * Expert workflow routes — lazy-loaded under `/experts`.
 * `/experts` → ExpertRequestsListPage (Pending/Approved/Rejected requests).
 * `/experts/profiles` → ExpertProfilesListPage (approved expert profiles).
 */
export const EXPERTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./expert-requests-list.page').then((m) => m.ExpertRequestsListPage),
    data: { permission: 'Community.Expert.ApproveRequest' },
    canMatch: [permissionGuard],
  },
  {
    path: 'profiles',
    loadComponent: () =>
      import('./expert-profiles-list.page').then((m) => m.ExpertProfilesListPage),
    data: { permission: 'Community.Expert.ApproveRequest' },
    canMatch: [permissionGuard],
  },
];

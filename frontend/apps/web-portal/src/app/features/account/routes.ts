import { Routes } from '@angular/router';

export const ACCOUNT_ROUTES: Routes = [
  {
    path: 'profile',
    loadComponent: () => import('./profile.page').then((m) => m.ProfilePage),
  },
  {
    path: 'expert-request',
    loadComponent: () =>
      import('./expert-request.page').then((m) => m.ExpertRequestPage),
  },
  // 'follows' lives in Phase 07
];

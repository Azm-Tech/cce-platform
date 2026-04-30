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
  {
    path: 'follows',
    loadComponent: () =>
      import('../follows/follows.page').then((m) => m.FollowsPage),
  },
  {
    path: 'notifications',
    loadComponent: () =>
      import('../notifications/notifications-page.page').then((m) => m.NotificationsPage),
  },
];

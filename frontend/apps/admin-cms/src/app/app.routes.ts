import { Route } from '@angular/router';
import { autoLoginPartialRoutesGuard } from 'angular-auth-oidc-client';
import { ProfilePage } from './profile/profile.page';

export const appRoutes: Route[] = [
  { path: '', pathMatch: 'full', redirectTo: 'profile' },
  {
    path: 'profile',
    component: ProfilePage,
    canActivate: [autoLoginPartialRoutesGuard],
    title: 'CCE — Profile',
  },
  {
    path: 'users',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/identity/routes').then((m) => m.IDENTITY_ROUTES),
    title: 'CCE — Users',
  },
  {
    path: 'state-rep-assignments',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () =>
      import('./features/identity/state-rep-routes').then((m) => m.STATE_REP_ROUTES),
    title: 'CCE — State-Rep Assignments',
  },
  {
    path: 'experts',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/experts/routes').then((m) => m.EXPERTS_ROUTES),
    title: 'CCE — Experts',
  },
  {
    path: 'resources',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () =>
      import('./features/content/routes').then((m) => m.RESOURCES_ROUTES),
    title: 'CCE — Resources',
  },
  {
    path: 'country-resource-requests',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () =>
      import('./features/content/routes').then((m) => m.COUNTRY_RESOURCE_REQUEST_ROUTES),
    title: 'CCE — Country resource requests',
  },
  {
    path: 'news',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/publishing/routes').then((m) => m.NEWS_ROUTES),
    title: 'CCE — News',
  },
  {
    path: 'events',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/publishing/routes').then((m) => m.EVENTS_ROUTES),
    title: 'CCE — Events',
  },
  {
    path: 'pages',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/publishing/routes').then((m) => m.PAGES_ROUTES),
    title: 'CCE — Pages',
  },
  {
    path: 'homepage',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/publishing/routes').then((m) => m.HOMEPAGE_ROUTES),
    title: 'CCE — Homepage',
  },
  {
    path: 'taxonomies',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/taxonomies/routes').then((m) => m.TAXONOMIES_ROUTES),
    title: 'CCE — Taxonomies',
  },
  {
    path: 'community-moderation',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () =>
      import('./features/community-moderation/routes').then((m) => m.COMMUNITY_MODERATION_ROUTES),
    title: 'CCE — Community moderation',
  },
  {
    path: 'countries',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/countries/routes').then((m) => m.COUNTRIES_ROUTES),
    title: 'CCE — Countries',
  },
  {
    path: 'notifications',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () =>
      import('./features/notifications/routes').then((m) => m.NOTIFICATIONS_ROUTES),
    title: 'CCE — Notifications',
  },
  {
    path: 'reports',
    canActivate: [autoLoginPartialRoutesGuard],
    loadChildren: () => import('./features/reports/routes').then((m) => m.REPORTS_ROUTES),
    title: 'CCE — Reports',
  },
];

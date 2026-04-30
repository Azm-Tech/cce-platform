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
];

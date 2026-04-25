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
];

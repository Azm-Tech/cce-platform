import { Route } from '@angular/router';
import { devAuthGuard } from './core/auth/dev-auth.guard';
import { AuthCallbackPage } from './auth-callback/auth-callback.page';
import { ProfilePage } from './profile/profile.page';

export const appRoutes: Route[] = [
  { path: '', pathMatch: 'full', redirectTo: 'profile' },
  // Public OIDC redirect target — must NOT have devAuthGuard.
  // angular-auth-oidc-client reads the URL params from this page and
  // navigates the user to the originally-requested route once tokens
  // are stored.
  { path: 'auth/callback', component: AuthCallbackPage, title: 'CCE — Signing in…' },
  {
    path: 'profile',
    component: ProfilePage,
    canActivate: [devAuthGuard],
    title: 'CCE — Profile',
  },
  {
    path: 'users',
    canActivate: [devAuthGuard],
    loadChildren: () => import('./features/identity/routes').then((m) => m.IDENTITY_ROUTES),
    title: 'CCE — Users',
  },
  {
    path: 'state-rep-assignments',
    canActivate: [devAuthGuard],
    loadChildren: () =>
      import('./features/identity/state-rep-routes').then((m) => m.STATE_REP_ROUTES),
    title: 'CCE — State-Rep Assignments',
  },
  {
    path: 'experts',
    canActivate: [devAuthGuard],
    loadChildren: () => import('./features/experts/routes').then((m) => m.EXPERTS_ROUTES),
    title: 'CCE — Experts',
  },
  {
    path: 'resources',
    canActivate: [devAuthGuard],
    loadChildren: () =>
      import('./features/content/routes').then((m) => m.RESOURCES_ROUTES),
    title: 'CCE — Resources',
  },
  {
    path: 'country-resource-requests',
    canActivate: [devAuthGuard],
    loadChildren: () =>
      import('./features/content/routes').then((m) => m.COUNTRY_RESOURCE_REQUEST_ROUTES),
    title: 'CCE — Country resource requests',
  },
  {
    path: 'news',
    canActivate: [devAuthGuard],
    loadChildren: () => import('./features/publishing/routes').then((m) => m.NEWS_ROUTES),
    title: 'CCE — News',
  },
  {
    path: 'events',
    canActivate: [devAuthGuard],
    loadChildren: () => import('./features/publishing/routes').then((m) => m.EVENTS_ROUTES),
    title: 'CCE — Events',
  },
  {
    path: 'pages',
    canActivate: [devAuthGuard],
    loadChildren: () => import('./features/publishing/routes').then((m) => m.PAGES_ROUTES),
    title: 'CCE — Pages',
  },
  {
    path: 'homepage',
    canActivate: [devAuthGuard],
    loadChildren: () => import('./features/publishing/routes').then((m) => m.HOMEPAGE_ROUTES),
    title: 'CCE — Homepage',
  },
  {
    path: 'taxonomies',
    canActivate: [devAuthGuard],
    loadChildren: () => import('./features/taxonomies/routes').then((m) => m.TAXONOMIES_ROUTES),
    title: 'CCE — Taxonomies',
  },
  {
    path: 'community-moderation',
    canActivate: [devAuthGuard],
    loadChildren: () =>
      import('./features/community-moderation/routes').then((m) => m.COMMUNITY_MODERATION_ROUTES),
    title: 'CCE — Community moderation',
  },
  {
    path: 'countries',
    canActivate: [devAuthGuard],
    loadChildren: () => import('./features/countries/routes').then((m) => m.COUNTRIES_ROUTES),
    title: 'CCE — Countries',
  },
  {
    path: 'notifications',
    canActivate: [devAuthGuard],
    loadChildren: () =>
      import('./features/notifications/routes').then((m) => m.NOTIFICATIONS_ROUTES),
    title: 'CCE — Notifications',
  },
  {
    path: 'reports',
    canActivate: [devAuthGuard],
    loadChildren: () => import('./features/reports/routes').then((m) => m.REPORTS_ROUTES),
    title: 'CCE — Reports',
  },
  {
    path: 'audit',
    canActivate: [devAuthGuard],
    loadChildren: () => import('./features/audit/routes').then((m) => m.AUDIT_ROUTES),
    title: 'CCE — Audit log',
  },
  {
    path: 'translations',
    canActivate: [devAuthGuard],
    loadChildren: () =>
      import('./features/translations/routes').then((m) => m.TRANSLATIONS_ROUTES),
    title: 'CCE — Translations',
  },
  {
    path: 'settings',
    canActivate: [devAuthGuard],
    loadChildren: () => import('./features/settings/routes').then((m) => m.SETTINGS_ROUTES),
    title: 'CCE — Settings',
  },
];

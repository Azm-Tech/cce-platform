import { Route } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { permissionGuard } from './core/auth/permission.guard';
import { ShellComponent } from './core/layout/shell.component';
import { LoginPage } from './features/account/login.page';
import { ProfilePage } from './features/account/profile.page';

export const appRoutes: Route[] = [
  { path: 'login', component: LoginPage, title: 'CCE Admin — Sign in' },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'profile' },
      { path: 'profile', component: ProfilePage, title: 'CCE — Profile' },
      {
        path: 'users',
        canMatch: [permissionGuard],
        data: { permission: 'User.Read' },
        loadChildren: () => import('./features/identity/routes').then((m) => m.IDENTITY_ROUTES),
        title: 'CCE — Users',
      },
      {
        path: 'state-rep-assignments',
        canMatch: [permissionGuard],
        data: { permission: 'Role.Assign' },
        loadChildren: () =>
          import('./features/identity/state-rep-routes').then((m) => m.STATE_REP_ROUTES),
        title: 'CCE — State-Rep Assignments',
      },
      {
        path: 'experts',
        canMatch: [permissionGuard],
        data: { permission: 'Community.Expert.ApproveRequest' },
        loadChildren: () => import('./features/experts/routes').then((m) => m.EXPERTS_ROUTES),
        title: 'CCE — Experts',
      },
      {
        path: 'resources',
        canMatch: [permissionGuard],
        data: { permission: 'Resource.Center.Upload' },
        loadChildren: () => import('./features/content/routes').then((m) => m.RESOURCES_ROUTES),
        title: 'CCE — Resources',
      },
      {
        path: 'country-resource-requests',
        canMatch: [permissionGuard],
        data: { permission: 'Resource.Country.Approve' },
        loadChildren: () =>
          import('./features/content/routes').then((m) => m.COUNTRY_RESOURCE_REQUEST_ROUTES),
        title: 'CCE — Country resource requests',
      },
      {
        path: 'news',
        canMatch: [permissionGuard],
        data: { permission: 'News.Update' },
        loadChildren: () => import('./features/publishing/routes').then((m) => m.NEWS_ROUTES),
        title: 'CCE — News',
      },
      {
        path: 'events',
        canMatch: [permissionGuard],
        data: { permission: 'Event.Manage' },
        loadChildren: () => import('./features/publishing/routes').then((m) => m.EVENTS_ROUTES),
        title: 'CCE — Events',
      },
      {
        path: 'pages',
        canMatch: [permissionGuard],
        data: { permission: 'Page.Edit' },
        loadChildren: () => import('./features/publishing/routes').then((m) => m.PAGES_ROUTES),
        title: 'CCE — Pages',
      },
      {
        path: 'homepage',
        canMatch: [permissionGuard],
        data: { permission: 'Page.Edit' },
        loadChildren: () => import('./features/publishing/routes').then((m) => m.HOMEPAGE_ROUTES),
        title: 'CCE — Homepage',
      },
      {
        path: 'taxonomies',
        canMatch: [permissionGuard],
        data: { permission: 'Resource.Center.Upload' },
        loadChildren: () =>
          import('./features/taxonomies/routes').then((m) => m.TAXONOMIES_ROUTES),
        title: 'CCE — Taxonomies',
      },
      {
        path: 'community-moderation',
        canMatch: [permissionGuard],
        data: { permission: 'Community.Post.Moderate' },
        loadChildren: () =>
          import('./features/community-moderation/routes').then(
            (m) => m.COMMUNITY_MODERATION_ROUTES,
          ),
        title: 'CCE — Community moderation',
      },
      {
        path: 'countries',
        canMatch: [permissionGuard],
        data: { permission: 'Country.Profile.Update' },
        loadChildren: () => import('./features/countries/routes').then((m) => m.COUNTRIES_ROUTES),
        title: 'CCE — Countries',
      },
      {
        path: 'notifications',
        canMatch: [permissionGuard],
        data: { permission: 'Notification.TemplateManage' },
        loadChildren: () =>
          import('./features/notifications/routes').then((m) => m.NOTIFICATIONS_ROUTES),
        title: 'CCE — Notifications',
      },
      {
        path: 'reports',
        canMatch: [permissionGuard],
        data: { permission: 'Report.UserRegistrations' },
        loadChildren: () => import('./features/reports/routes').then((m) => m.REPORTS_ROUTES),
        title: 'CCE — Reports',
      },
      {
        path: 'audit',
        canMatch: [permissionGuard],
        data: { permission: 'Audit.Read' },
        loadChildren: () => import('./features/audit/routes').then((m) => m.AUDIT_ROUTES),
        title: 'CCE — Audit log',
      },
      {
        path: 'translations',
        canMatch: [permissionGuard],
        data: { permission: 'Translation.Manage' },
        loadChildren: () =>
          import('./features/translations/routes').then((m) => m.TRANSLATIONS_ROUTES),
        title: 'CCE — Translations',
      },
      {
        path: 'settings',
        canMatch: [permissionGuard],
        data: { permission: 'Settings.Manage' },
        loadChildren: () => import('./features/settings/routes').then((m) => m.SETTINGS_ROUTES),
        title: 'CCE — Settings',
      },
    ],
  },
];

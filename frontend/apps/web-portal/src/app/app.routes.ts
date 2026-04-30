import { Route } from '@angular/router';
import { HealthPage } from './health/health.page';

export const appRoutes: Route[] = [
  {
    path: '',
    pathMatch: 'full',
    loadChildren: () => import('./features/home/routes').then((m) => m.HOME_ROUTES),
  },
  {
    path: 'pages',
    loadChildren: () => import('./features/pages/routes').then((m) => m.STATIC_PAGES_ROUTES),
  },
  {
    path: 'search',
    loadComponent: () =>
      import('./features/search/search-placeholder.page').then((m) => m.SearchPlaceholderPage),
    title: 'CCE — Search',
  },
  { path: 'health', component: HealthPage, title: 'CCE — Health' },
];

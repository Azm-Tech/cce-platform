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
    path: 'knowledge-center',
    loadChildren: () =>
      import('./features/knowledge-center/routes').then((m) => m.KNOWLEDGE_CENTER_ROUTES),
    title: 'CCE — Knowledge Center',
  },
  {
    path: 'search',
    loadComponent: () =>
      import('./features/search/search-placeholder.page').then((m) => m.SearchPlaceholderPage),
    title: 'CCE — Search',
  },
  { path: 'health', component: HealthPage, title: 'CCE — Health' },
];

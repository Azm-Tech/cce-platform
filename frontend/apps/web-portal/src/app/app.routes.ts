import { Route } from '@angular/router';
import { HealthPage } from './health/health.page';
import { authGuard } from './core/auth/auth.guard';

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
    path: 'news',
    loadChildren: () => import('./features/news/routes').then((m) => m.NEWS_ROUTES),
    title: 'CCE — News',
  },
  {
    path: 'events',
    loadChildren: () => import('./features/events/routes').then((m) => m.EVENTS_ROUTES),
    title: 'CCE — Events',
  },
  {
    path: 'countries',
    loadChildren: () => import('./features/countries/routes').then((m) => m.COUNTRIES_ROUTES),
    title: 'CCE — Countries',
  },
  {
    path: 'search',
    loadComponent: () =>
      import('./features/search/search-results.page').then((m) => m.SearchResultsPage),
    title: 'CCE — Search',
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/account/register.page').then((m) => m.RegisterPage),
    title: 'CCE — Register',
  },
  {
    path: 'me',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/account/routes').then((m) => m.ACCOUNT_ROUTES),
    title: 'CCE — My account',
  },
  {
    path: 'community',
    loadChildren: () =>
      import('./features/community/routes').then((m) => m.COMMUNITY_ROUTES),
    title: 'CCE — Community',
  },
  {
    path: 'knowledge-maps',
    loadChildren: () =>
      import('./features/knowledge-maps/routes').then((m) => m.KNOWLEDGE_MAPS_ROUTES),
    title: 'CCE — Knowledge Maps',
  },
  {
    path: 'interactive-city',
    loadChildren: () =>
      import('./features/interactive-city/routes').then((m) => m.INTERACTIVE_CITY_ROUTES),
    title: 'CCE — Interactive City',
  },
  {
    path: 'assistant',
    loadChildren: () =>
      import('./features/assistant/routes').then((m) => m.ASSISTANT_ROUTES),
    title: 'CCE — Assistant',
  },
  { path: 'health', component: HealthPage, title: 'CCE — Health' },
];

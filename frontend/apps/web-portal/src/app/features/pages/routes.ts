import { Routes } from '@angular/router';

export const STATIC_PAGES_ROUTES: Routes = [
  {
    path: ':slug',
    loadComponent: () => import('./static-page.page').then((m) => m.StaticPagePage),
  },
];

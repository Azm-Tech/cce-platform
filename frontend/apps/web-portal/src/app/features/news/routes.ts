import { Routes } from '@angular/router';

export const NEWS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./news-list.page').then((m) => m.NewsListPage),
  },
  {
    path: ':slug',
    loadComponent: () => import('./news-detail.page').then((m) => m.NewsDetailPage),
  },
];

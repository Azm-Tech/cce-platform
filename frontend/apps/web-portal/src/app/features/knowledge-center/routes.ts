import { Routes } from '@angular/router';

export const KNOWLEDGE_CENTER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./resources-list.page').then((m) => m.ResourcesListPage),
  },
  {
    path: ':id',
    loadComponent: () => import('./resource-detail.page').then((m) => m.ResourceDetailPage),
  },
];

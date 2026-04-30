import { Routes } from '@angular/router';

export const EVENTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./events-list.page').then((m) => m.EventsListPage),
  },
  {
    path: ':id',
    loadComponent: () => import('./event-detail.page').then((m) => m.EventDetailPage),
  },
];

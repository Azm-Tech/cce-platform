import { Routes } from '@angular/router';

export const KNOWLEDGE_MAPS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./knowledge-maps-list.page').then((m) => m.KnowledgeMapsListPage),
  },
  {
    path: ':id',
    loadComponent: () => import('./map-viewer.page').then((m) => m.MapViewerPage),
  },
];

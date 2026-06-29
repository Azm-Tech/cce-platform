import { Routes } from '@angular/router';

export const KNOWLEDGE_MAPS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./map-viewer.page').then((m) => m.MapViewerPage),
  },
];

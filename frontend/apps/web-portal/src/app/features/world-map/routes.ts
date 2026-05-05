import { Routes } from '@angular/router';

export const WORLD_MAP_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./world-map.page').then((m) => m.WorldMapPage),
    title: 'CCE — Explore the World',
  },
];

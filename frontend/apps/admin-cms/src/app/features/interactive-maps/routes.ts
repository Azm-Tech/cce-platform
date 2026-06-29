import { Routes } from '@angular/router';

export const INTERACTIVE_MAPS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./interactive-map-detail.page').then((m) => m.InteractiveMapDetailPage),
  },
];

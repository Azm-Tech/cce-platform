import { Routes } from '@angular/router';

export const HOME_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./home-v2.page').then((m) => m.HomeV2Page),
  },
];

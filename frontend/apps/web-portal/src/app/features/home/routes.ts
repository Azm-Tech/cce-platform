import { Routes } from '@angular/router';

const USE_V2 = true;

export const HOME_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => USE_V2
      ? import('./home-v2.page').then((m) => m.HomeV2Page)
      : import('./home.page').then((m) => m.HomePage),
  },
];

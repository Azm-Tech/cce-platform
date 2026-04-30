import { Routes } from '@angular/router';

export const COUNTRIES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./countries-grid.page').then((m) => m.CountriesGridPage),
  },
  {
    path: ':id',
    loadComponent: () => import('./country-detail.page').then((m) => m.CountryDetailPage),
  },
];

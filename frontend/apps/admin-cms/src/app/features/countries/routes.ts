import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/permission.guard';

export const COUNTRIES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./countries-list.page').then((m) => m.CountriesListPage),
    data: { permission: 'Country.Profile.Update' },
    canMatch: [permissionGuard],
  },
  {
    path: ':id',
    loadComponent: () => import('./country-detail.page').then((m) => m.CountryDetailPage),
    data: { permission: 'Country.Profile.Update' },
    canMatch: [permissionGuard],
  },
];

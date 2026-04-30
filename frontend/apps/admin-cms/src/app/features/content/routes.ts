import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/permission.guard';

export const RESOURCES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./resources-list.page').then((m) => m.ResourcesListPage),
    data: { permission: 'Resource.Center.Upload' },
    canMatch: [permissionGuard],
  },
];

export const COUNTRY_RESOURCE_REQUEST_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./country-resource-request.page').then((m) => m.CountryResourceRequestPage),
    data: { permission: 'Resource.Country.Approve' },
    canMatch: [permissionGuard],
  },
];

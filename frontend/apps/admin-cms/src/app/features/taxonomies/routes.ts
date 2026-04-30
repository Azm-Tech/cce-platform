import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/permission.guard';

export const TAXONOMIES_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'categories',
  },
  {
    path: 'categories',
    loadComponent: () =>
      import('./resource-categories.page').then((m) => m.ResourceCategoriesPage),
    data: { permission: 'Resource.Center.Upload' },
    canMatch: [permissionGuard],
  },
  {
    path: 'topics',
    loadComponent: () => import('./topics.page').then((m) => m.TopicsPage),
    data: { permission: 'Community.Post.Moderate' },
    canMatch: [permissionGuard],
  },
];

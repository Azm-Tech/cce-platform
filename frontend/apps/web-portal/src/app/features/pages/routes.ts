import { Routes } from '@angular/router';

export const STATIC_PAGES_ROUTES: Routes = [
  {
    path: ':slug',
    loadComponent: () => import('./static-page.page').then((m) => m.StaticPagePage),
  },
];

export const ABOUT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./about.page').then((m) => m.AboutPage),
    title: 'CCE — About',
  },
];

export const POLICIES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./policies.page').then((m) => m.PoliciesPage),
    title: 'CCE — Policies & Terms',
  },
];

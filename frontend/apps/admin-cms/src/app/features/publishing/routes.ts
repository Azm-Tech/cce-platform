import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/permission.guard';

export const NEWS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./news-list.page').then((m) => m.NewsListPage),
    data: { permission: 'News.Update' },
    canMatch: [permissionGuard],
  },
];

export const EVENTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./events-list.page').then((m) => m.EventsListPage),
    data: { permission: 'Event.Manage' },
    canMatch: [permissionGuard],
  },
];

export const PAGES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages-list.page').then((m) => m.PagesListPage),
    data: { permission: 'Page.Edit' },
    canMatch: [permissionGuard],
  },
];

export const HOMEPAGE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./homepage.page').then((m) => m.HomepagePage),
    data: { permission: 'Page.Edit' },
    canMatch: [permissionGuard],
  },
];

export const ABOUT_SETTINGS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./about-settings.page').then((m) => m.AboutSettingsPage),
    data: { permission: 'Page.Edit' },
    canMatch: [permissionGuard],
  },
];

export const POLICIES_SETTINGS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./policies-settings.page').then((m) => m.PoliciesSettingsPage),
    data: { permission: 'Settings.Manage' },
    canMatch: [permissionGuard],
  },
];

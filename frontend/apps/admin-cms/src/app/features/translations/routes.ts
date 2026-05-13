import { Routes } from '@angular/router';

export const TRANSLATIONS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./translations.page').then((m) => m.TranslationsPage),
  },
];

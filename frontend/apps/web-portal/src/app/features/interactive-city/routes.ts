import { Routes } from '@angular/router';

export const INTERACTIVE_CITY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./interactive-city.page').then((m) => m.InteractiveCityPage),
  },
];

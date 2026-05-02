import { Routes } from '@angular/router';

export const INTERACTIVE_CITY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./scenario-builder.page').then((m) => m.ScenarioBuilderPage),
  },
];

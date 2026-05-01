import { Routes } from '@angular/router';

export const ASSISTANT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./assistant.page').then((m) => m.AssistantPage),
  },
];

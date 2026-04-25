import { Route } from '@angular/router';
import { HealthPage } from './health/health.page';

export const appRoutes: Route[] = [
  { path: '', pathMatch: 'full', redirectTo: 'health' },
  { path: 'health', component: HealthPage, title: 'CCE — Health' },
];

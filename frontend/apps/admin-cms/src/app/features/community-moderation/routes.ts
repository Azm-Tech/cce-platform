import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/permission.guard';

export const COMMUNITY_MODERATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./community-moderation.page').then((m) => m.CommunityModerationPage),
    data: { permission: 'Community.Post.Moderate' },
    canMatch: [permissionGuard],
  },
];

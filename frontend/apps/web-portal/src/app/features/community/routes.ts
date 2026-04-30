import { Routes } from '@angular/router';

export const COMMUNITY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./topics-list.page').then((m) => m.TopicsListPage),
  },
  {
    path: 'topics/:slug',
    loadComponent: () =>
      import('./topic-detail.page').then((m) => m.TopicDetailPage),
  },
  {
    path: 'posts/:id',
    loadComponent: () =>
      import('./post-detail.page').then((m) => m.PostDetailPage),
  },
];

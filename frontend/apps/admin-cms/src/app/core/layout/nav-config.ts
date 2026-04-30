export interface NavItem {
  labelKey: string;
  route: string;
  permission: string;
  icon: string;
}

export const NAV_ITEMS: readonly NavItem[] = [
  { labelKey: 'nav.users', route: '/users', permission: 'User.Read', icon: 'people' },
  { labelKey: 'nav.stateReps', route: '/state-rep-assignments', permission: 'Role.Assign', icon: 'badge' },
  { labelKey: 'nav.experts', route: '/experts', permission: 'Community.Expert.ApproveRequest', icon: 'school' },
  { labelKey: 'nav.resources', route: '/resources', permission: 'Resource.Center.Upload', icon: 'description' },
  { labelKey: 'nav.countryResourceRequests', route: '/country-resource-requests', permission: 'Resource.Country.Approve', icon: 'flag' },
  { labelKey: 'nav.news', route: '/news', permission: 'News.Update', icon: 'feed' },
  { labelKey: 'nav.events', route: '/events', permission: 'Event.Manage', icon: 'event' },
  { labelKey: 'nav.pages', route: '/pages', permission: 'Page.Edit', icon: 'web' },
  { labelKey: 'nav.taxonomies', route: '/taxonomies', permission: 'Resource.Center.Upload', icon: 'category' },
  { labelKey: 'nav.community', route: '/community-moderation', permission: 'Community.Post.Moderate', icon: 'forum' },
  { labelKey: 'nav.countries', route: '/countries', permission: 'Country.Profile.Update', icon: 'public' },
  { labelKey: 'nav.notifications', route: '/notifications', permission: 'Notification.TemplateManage', icon: 'notifications' },
  { labelKey: 'nav.reports', route: '/reports', permission: 'Report.UserRegistrations', icon: 'assessment' },
  { labelKey: 'nav.audit', route: '/audit', permission: 'Audit.Read', icon: 'history' },
];

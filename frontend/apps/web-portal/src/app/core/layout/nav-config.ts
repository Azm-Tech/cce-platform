export interface PrimaryNavItem {
  labelKey: string;
  route: string;
  icon: string;
}

export const PRIMARY_NAV: readonly PrimaryNavItem[] = [
  { labelKey: 'nav.home', route: '/', icon: 'home' },
  { labelKey: 'nav.knowledgeCenter', route: '/knowledge-center', icon: 'menu_book' },
  { labelKey: 'nav.news', route: '/news', icon: 'feed' },
  { labelKey: 'nav.events', route: '/events', icon: 'event' },
  { labelKey: 'nav.countries', route: '/countries', icon: 'public' },
  { labelKey: 'nav.community', route: '/community', icon: 'forum' },
];

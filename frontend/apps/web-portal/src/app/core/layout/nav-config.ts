export interface PrimaryNavItem {
  labelKey: string;
  route: string;
  icon: string;
}

export const PRIMARY_NAV: readonly PrimaryNavItem[] = [
  { labelKey: 'nav.home', route: '/', icon: 'home' },
  { labelKey: 'nav.knowledgeCenter', route: '/knowledge-center', icon: 'menu_book' },
  { labelKey: 'nav.knowledgeMaps', route: '/knowledge-maps', icon: 'account_tree' },
  { labelKey: 'nav.worldMap', route: '/explore', icon: 'travel_explore' },
  { labelKey: 'nav.assistant', route: '/assistant', icon: 'smart_toy' },
  { labelKey: 'nav.news', route: '/news', icon: 'feed' },
  { labelKey: 'nav.events', route: '/events', icon: 'event' },
  { labelKey: 'nav.countries', route: '/countries', icon: 'public' },
  { labelKey: 'nav.community', route: '/community', icon: 'forum' },
];

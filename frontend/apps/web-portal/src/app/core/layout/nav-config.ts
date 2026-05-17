/**
 * Header navigation config. Items are either:
 *   • A `NavLink` — single-route link that renders as a normal nav anchor
 *   • A `NavGroup` — has child links and renders as a mega-menu trigger
 *     button. The header opens a panel with the children laid out as
 *     icon + title + short description cards.
 *
 * Design intent for the "Discover" group: collapse the three exploration-
 * heavy entries (Knowledge Center, World Map, Countries) under one
 * label so the top-level row stays scannable at common laptop widths.
 */

export interface NavLink {
  kind: 'link';
  labelKey: string;
  route: string;
  icon: string;
  /** Optional one-line description shown only inside mega-menu panels. */
  descriptionKey?: string;
  /** Hero image URL shown on hover inside the mega-menu preview pane.
   *  Stable Unsplash CDN URLs are used so the menu always has a
   *  representative picture. */
  previewImage?: string;
}

export interface NavGroup {
  kind: 'group';
  /** Stable id for `aria-controls` and open/close state. */
  id: string;
  labelKey: string;
  icon: string;
  children: NavLink[];
}

export type PrimaryNavItem = NavLink | NavGroup;

export const PRIMARY_NAV: readonly PrimaryNavItem[] = [
  { kind: 'link', labelKey: 'nav.home', route: '/', icon: 'home' },
  {
    kind: 'group',
    id: 'discover',
    labelKey: 'nav.discover',
    icon: 'explore',
    children: [
      {
        kind: 'link',
        labelKey: 'nav.knowledgeCenter',
        route: '/knowledge-center',
        icon: 'menu_book',
        descriptionKey: 'nav.discoverDesc.knowledgeCenter',
        // Library shelves — represents curated long-form knowledge.
        previewImage: 'https://images.unsplash.com/photo-1507842217343-583bb7270b66?w=720&q=80&auto=format&fit=crop',
      },
      {
        kind: 'link',
        labelKey: 'nav.worldMap',
        route: '/explore',
        icon: 'travel_explore',
        descriptionKey: 'nav.discoverDesc.worldMap',
        // Globe / world map — represents the interactive city map.
        previewImage: 'https://images.unsplash.com/photo-1524661135-423995f22d0b?w=720&q=80&auto=format&fit=crop',
      },
      {
        kind: 'link',
        labelKey: 'nav.countries',
        route: '/countries',
        icon: 'public',
        descriptionKey: 'nav.discoverDesc.countries',
        // World flags — represents per-country profiles.
        previewImage: 'https://images.unsplash.com/photo-1495020689067-958852a7765e?w=720&q=80&auto=format&fit=crop',
      },
    ],
  },
  { kind: 'link', labelKey: 'nav.knowledgeMaps', route: '/knowledge-maps', icon: 'account_tree' },
  { kind: 'link', labelKey: 'nav.news', route: '/news', icon: 'feed' },
  { kind: 'link', labelKey: 'nav.events', route: '/events', icon: 'event' },
  { kind: 'link', labelKey: 'nav.community', route: '/community', icon: 'forum' },
];

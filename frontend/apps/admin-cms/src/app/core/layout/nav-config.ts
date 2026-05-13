export interface NavItem {
  labelKey: string;
  route: string;
  permission: string;
  icon: string;
}

export interface NavGroup {
  /** Stable id used for keying. */
  id: string;
  /** i18n key for the section heading. */
  labelKey: string;
  /** Material icon shown next to the heading. */
  icon: string;
  items: readonly NavItem[];
}

/**
 * Side-nav layout. Grouped so related admin features sit together:
 *
 *   - **People & Access** — user / role / community-moderation surfaces
 *   - **Publishing** — editorial CMS (pages, news, events, homepage)
 *   - **Knowledge** — knowledge-center resources + taxonomies + country
 *     resource requests + country profiles
 *   - **Operations** — notifications, reports, audit log
 *   - **System** — translations + global settings
 *
 * `SideNavComponent` filters items by permission and hides any
 * category whose items are all hidden, so a user with limited
 * permissions only sees the sections they can act on.
 */
export const NAV_GROUPS: readonly NavGroup[] = [
  {
    id: 'people',
    labelKey: 'nav.group.people',
    icon: 'groups',
    items: [
      { labelKey: 'nav.users', route: '/users', permission: 'User.Read', icon: 'people' },
      { labelKey: 'nav.stateReps', route: '/state-rep-assignments', permission: 'Role.Assign', icon: 'badge' },
      { labelKey: 'nav.experts', route: '/experts', permission: 'Community.Expert.ApproveRequest', icon: 'school' },
      { labelKey: 'nav.communityModeration', route: '/community-moderation', permission: 'Community.Post.Moderate', icon: 'forum' },
    ],
  },
  {
    id: 'publishing',
    labelKey: 'nav.group.publishing',
    icon: 'edit_note',
    items: [
      { labelKey: 'nav.pages', route: '/pages', permission: 'Page.Edit', icon: 'web' },
      { labelKey: 'nav.homepage', route: '/homepage', permission: 'Page.Edit', icon: 'home' },
      { labelKey: 'nav.news', route: '/news', permission: 'News.Update', icon: 'feed' },
      { labelKey: 'nav.events', route: '/events', permission: 'Event.Manage', icon: 'event' },
    ],
  },
  {
    id: 'knowledge',
    labelKey: 'nav.group.knowledge',
    icon: 'menu_book',
    items: [
      { labelKey: 'nav.resources', route: '/resources', permission: 'Resource.Center.Upload', icon: 'description' },
      { labelKey: 'nav.taxonomies', route: '/taxonomies', permission: 'Resource.Center.Upload', icon: 'category' },
      { labelKey: 'nav.countryResourceRequests', route: '/country-resource-requests', permission: 'Resource.Country.Approve', icon: 'flag' },
      { labelKey: 'nav.countries', route: '/countries', permission: 'Country.Profile.Update', icon: 'public' },
    ],
  },
  {
    id: 'operations',
    labelKey: 'nav.group.operations',
    icon: 'monitor_heart',
    items: [
      { labelKey: 'nav.notifications', route: '/notifications', permission: 'Notification.TemplateManage', icon: 'notifications' },
      { labelKey: 'nav.reports', route: '/reports', permission: 'Report.UserRegistrations', icon: 'assessment' },
      { labelKey: 'nav.audit', route: '/audit', permission: 'Audit.Read', icon: 'history' },
    ],
  },
  {
    id: 'system',
    labelKey: 'nav.group.system',
    icon: 'settings',
    items: [
      { labelKey: 'nav.translations', route: '/translations', permission: 'Translation.Manage', icon: 'translate' },
      { labelKey: 'nav.settings', route: '/settings', permission: 'Settings.Manage', icon: 'settings' },
    ],
  },
];

/** Backwards-compatible flat list — kept so any consumer that still
 *  imports `NAV_ITEMS` (e.g. older tests) keeps compiling. The
 *  grouped layout in `NAV_GROUPS` is the canonical source. */
export const NAV_ITEMS: readonly NavItem[] = NAV_GROUPS.flatMap((g) => g.items);

import { CcePermission } from '@frontend/contracts';

export interface NavItem {
  labelKey: string;
  route: string;
  permission: CcePermission;
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
      { labelKey: 'nav.users', route: '/users', permission: CcePermission.UserRead, icon: 'people' },
      { labelKey: 'nav.stateReps', route: '/state-rep-assignments', permission: CcePermission.RoleAssign, icon: 'badge' },
      { labelKey: 'nav.experts', route: '/experts', permission: CcePermission.CommunityExpertApprove, icon: 'school' },
      { labelKey: 'nav.communityModeration', route: '/community-moderation', permission: CcePermission.CommunityPostModerate, icon: 'forum' },
    ],
  },
  {
    id: 'publishing',
    labelKey: 'nav.group.publishing',
    icon: 'edit_note',
    items: [
      { labelKey: 'nav.pages', route: '/pages', permission: CcePermission.PageEdit, icon: 'web' },
      { labelKey: 'nav.homepage', route: '/homepage', permission: CcePermission.PageEdit, icon: 'home' },
      { labelKey: 'nav.aboutSettings', route: '/about-settings', permission: CcePermission.PageEdit, icon: 'info' },
      { labelKey: 'nav.policiesSettings', route: '/policies-settings', permission: CcePermission.SettingsManage, icon: 'policy' },
      { labelKey: 'nav.news', route: '/news', permission: CcePermission.NewsUpdate, icon: 'feed' },
      { labelKey: 'nav.events', route: '/events', permission: CcePermission.EventManage, icon: 'event' },
    ],
  },
  {
    id: 'knowledge',
    labelKey: 'nav.group.knowledge',
    icon: 'menu_book',
    items: [
      { labelKey: 'nav.resources', route: '/resources', permission: CcePermission.ResourceCenterUpload, icon: 'description' },
      { labelKey: 'nav.taxonomies', route: '/taxonomies', permission: CcePermission.ResourceCenterUpload, icon: 'category' },
      { labelKey: 'nav.countryResourceRequests', route: '/country-resource-requests', permission: CcePermission.ResourceCountryApprove, icon: 'flag' },
      { labelKey: 'nav.countries', route: '/countries', permission: CcePermission.CountryProfileUpdate, icon: 'public' },
    ],
  },
  {
    id: 'operations',
    labelKey: 'nav.group.operations',
    icon: 'monitor_heart',
    items: [
      { labelKey: 'nav.notifications', route: '/notifications', permission: CcePermission.NotificationTemplateManage, icon: 'notifications' },
      { labelKey: 'nav.reports', route: '/reports', permission: CcePermission.ReportUserRegistrations, icon: 'assessment' },
      { labelKey: 'nav.audit', route: '/audit', permission: CcePermission.AuditRead, icon: 'history' },
    ],
  },
  {
    id: 'system',
    labelKey: 'nav.group.system',
    icon: 'settings',
    items: [
      { labelKey: 'nav.translations', route: '/translations', permission: CcePermission.TranslationManage, icon: 'translate' },
      { labelKey: 'nav.settings', route: '/settings', permission: CcePermission.SettingsManage, icon: 'settings' },
    ],
  },
];

/** Backwards-compatible flat list — kept so any consumer that still
 *  imports `NAV_ITEMS` (e.g. older tests) keeps compiling. The
 *  grouped layout in `NAV_GROUPS` is the canonical source. */
export const NAV_ITEMS: readonly NavItem[] = NAV_GROUPS.flatMap((g) => g.items);

import { CcePermission } from '@frontend/contracts';

/**
 * The 8 streaming-CSV reports exposed by the Internal API at
 * /api/admin/reports/{slug}.csv. Each tile renders a card with a
 * date-range filter and a Download button.
 */
export interface ReportConfig {
  /** URL slug + filename root: `/api/admin/reports/{slug}.csv`. */
  readonly slug: string;
  /** i18n key root: `reports.<key>.title` / `.description`. */
  readonly key: string;
  /** Required permission to download. */
  readonly permission: CcePermission;
  /** Material icon for the card avatar. */
  readonly icon: string;
}

export const REPORTS: readonly ReportConfig[] = [
  { slug: 'users-registrations', key: 'usersRegistrations', permission: CcePermission.ReportUserRegistrations, icon: 'how_to_reg' },
  { slug: 'experts',             key: 'experts',             permission: CcePermission.ReportExpertList,         icon: 'school' },
  { slug: 'satisfaction-survey', key: 'satisfactionSurvey', permission: CcePermission.ReportSatisfactionSurvey, icon: 'sentiment_satisfied' },
  { slug: 'community-posts',     key: 'communityPosts',     permission: CcePermission.ReportCommunityPosts,     icon: 'forum' },
  { slug: 'news',                key: 'news',                permission: CcePermission.ReportNews,               icon: 'feed' },
  { slug: 'events',              key: 'events',              permission: CcePermission.ReportEvents,             icon: 'event' },
  { slug: 'resources',           key: 'resources',           permission: CcePermission.ReportResources,          icon: 'description' },
  { slug: 'country-profiles',    key: 'countryProfiles',    permission: CcePermission.ReportCountryProfiles,    icon: 'public' },
];

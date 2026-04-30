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
  readonly permission: string;
  /** Material icon for the card avatar. */
  readonly icon: string;
}

export const REPORTS: readonly ReportConfig[] = [
  { slug: 'users-registrations', key: 'usersRegistrations', permission: 'Report.UserRegistrations', icon: 'how_to_reg' },
  { slug: 'experts',             key: 'experts',             permission: 'Report.ExpertList',         icon: 'school' },
  { slug: 'satisfaction-survey', key: 'satisfactionSurvey', permission: 'Report.SatisfactionSurvey', icon: 'sentiment_satisfied' },
  { slug: 'community-posts',     key: 'communityPosts',     permission: 'Report.CommunityPosts',     icon: 'forum' },
  { slug: 'news',                key: 'news',                permission: 'Report.News',               icon: 'feed' },
  { slug: 'events',              key: 'events',              permission: 'Report.Events',             icon: 'event' },
  { slug: 'resources',           key: 'resources',           permission: 'Report.Resources',          icon: 'description' },
  { slug: 'country-profiles',    key: 'countryProfiles',    permission: 'Report.CountryProfiles',    icon: 'public' },
];

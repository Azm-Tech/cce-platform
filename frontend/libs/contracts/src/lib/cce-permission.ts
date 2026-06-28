/**
 * All CCE admin permissions. Use CcePermission.X instead of raw strings
 * so typos are caught at compile time.
 */
export const CcePermission = {
  // Identity
  UserRead:                   'User.Read',
  UserDelete:                 'User.Delete',
  RoleAssign:                 'Role.Assign',
  // Community
  CommunityExpertApprove:     'Community.Expert.ApproveRequest',
  CommunityPostModerate:      'Community.Post.Moderate',
  // Resource center
  ResourceCenterUpload:       'Resource.Center.Upload',
  ResourceCenterUpdate:       'Resource.Center.Update',
  ResourceCenterDelete:       'Resource.Center.Delete',
  ResourceCountryApprove:     'Resource.Country.Approve',
  // News
  NewsUpdate:                 'News.Update',
  NewsPublish:                'News.Publish',
  NewsDelete:                 'News.Delete',
  // Events & pages
  EventManage:                'Event.Manage',
  PageEdit:                   'Page.Edit',
  // Countries
  CountryProfileUpdate:       'Country.Profile.Update',
  // Operations
  NotificationTemplateManage: 'Notification.TemplateManage',
  // Reports
  ReportUserRegistrations:    'Report.UserRegistrations',
  ReportExpertList:           'Report.ExpertList',
  ReportSatisfactionSurvey:   'Report.SatisfactionSurvey',
  ReportCommunityPosts:       'Report.CommunityPosts',
  ReportNews:                 'Report.News',
  ReportEvents:               'Report.Events',
  ReportResources:            'Report.Resources',
  ReportCountryProfiles:      'Report.CountryProfiles',
  // System
  AuditRead:                  'Audit.Read',
  TranslationManage:          'Translation.Manage',
  SettingsManage:             'Settings.Manage',
} as const;

export type CcePermission = typeof CcePermission[keyof typeof CcePermission];

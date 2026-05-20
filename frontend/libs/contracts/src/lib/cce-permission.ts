/**
 * All CCE admin permissions. Use CcePermission.X instead of raw strings
 * so typos are caught at compile time.
 */
export const CcePermission = {
  UserRead:                   'User.Read',
  RoleAssign:                 'Role.Assign',
  CommunityExpertApprove:     'Community.Expert.ApproveRequest',
  CommunityPostModerate:      'Community.Post.Moderate',
  ResourceCenterUpload:       'Resource.Center.Upload',
  ResourceCountryApprove:     'Resource.Country.Approve',
  NewsUpdate:                 'News.Update',
  EventManage:                'Event.Manage',
  PageEdit:                   'Page.Edit',
  CountryProfileUpdate:       'Country.Profile.Update',
  NotificationTemplateManage: 'Notification.TemplateManage',
  ReportUserRegistrations:    'Report.UserRegistrations',
  AuditRead:                  'Audit.Read',
  TranslationManage:          'Translation.Manage',
  SettingsManage:             'Settings.Manage',
} as const;

export type CcePermission = typeof CcePermission[keyof typeof CcePermission];

export const CceAdminRole = {
  SuperAdmin:     'cce-super-admin',
  Admin:          'cce-admin',
  ContentManager: 'cce-content-manager',
} as const;

export type CceAdminRole = typeof CceAdminRole[keyof typeof CceAdminRole];

export const CcePortalRole = {
  StateRepresentative: 'cce-state-representative',
  Expert:              'cce-expert',
  User:                'cce-user',
} as const;

export type CcePortalRole = typeof CcePortalRole[keyof typeof CcePortalRole];

/**
 * Hand-defined DTOs for the Identity admin feature. The generated api-client emits
 * `Response = unknown` because Sub-3 endpoints did not declare Produces<T>(); these
 * types mirror the backend records (CCE.Application.Identity.Dtos.*) so feature
 * pages can rely on real typings.
 */

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface UserListItem {
  id: string;
  email: string | null;
  userName: string | null;
  roles: string[];
  isActive: boolean;
}

export type KnowledgeLevel = 'Beginner' | 'Intermediate' | 'Advanced';

export interface UserDetail {
  id: string;
  email: string | null;
  userName: string | null;
  localePreference: string;
  knowledgeLevel: KnowledgeLevel;
  interests: string[];
  countryId: string | null;
  avatarUrl: string | null;
  roles: string[];
  isActive: boolean;
}

export interface StateRepAssignment {
  id: string;
  userId: string;
  userName: string | null;
  countryId: string;
  assignedOn: string;
  assignedById: string;
  revokedOn: string | null;
  revokedById: string | null;
  isActive: boolean;
}

/** Roles known to the backend (CCE.Domain.RolePermissionMap.KnownRoles). */
export const KNOWN_ROLES = [
  'cce-super-admin',
  'cce-admin',
  'cce-content-manager',
  'cce-state-representative',
  'cce-community-expert',
  'cce-user',
] as const;
export type RoleName = (typeof KNOWN_ROLES)[number];

/** Enum-style const for type-safe role references. */
export const Role = {
  SuperAdmin: 'cce-super-admin',
  Admin: 'cce-admin',
  ContentManager: 'cce-content-manager',
  StateRepresentative: 'cce-state-representative',
  CommunityExpert: 'cce-community-expert',
  User: 'cce-user',
} as const satisfies Record<string, RoleName>;

/** All known roles with translation keys — used in filter dropdowns. */
export const KNOWN_ROLE_OPTIONS: readonly { value: string; labelKey: string }[] = [
  { value: 'cce-super-admin',          labelKey: 'users.role.superAdmin' },
  { value: 'cce-admin',                labelKey: 'users.role.admin' },
  { value: 'cce-content-manager',      labelKey: 'users.role.contentManager' },
  { value: 'cce-state-representative', labelKey: 'users.role.stateRepresentative' },
  { value: 'cce-community-expert',     labelKey: 'users.role.communityExpert' },
  { value: 'cce-user',                 labelKey: 'users.role.registeredUser' },
];

/** Roles a super-admin can assign when creating a new user. */
export const ASSIGNABLE_ROLES: readonly { value: string; labelKey: string }[] = [
  { value: 'cce-admin',                labelKey: 'users.role.admin' },
  { value: 'cce-content-manager',      labelKey: 'users.role.contentManager' },
  { value: 'cce-state-representative', labelKey: 'users.role.stateRepresentative' },
];

export interface CreateUserBody {
  firstName: string;
  lastName: string;
  email: string;
  phoneCountryCodeId: string;
  phoneNumber: string;
  countryId: string;
  role: string;
}

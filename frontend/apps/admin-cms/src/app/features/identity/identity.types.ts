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
  'SuperAdmin',
  'ContentManager',
  'StateRepresentative',
  'CommunityExpert',
  'RegisteredUser',
] as const;
export type RoleName = (typeof KNOWN_ROLES)[number];

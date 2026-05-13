/**
 * Admin-side community post row — mirrors the backend
 * `CCE.Application.Community.Queries.ListAdminPosts.AdminPostRow` DTO.
 */
export interface AdminPostRow {
  id: string;
  topicId: string;
  topicNameEn: string;
  topicNameAr: string;
  authorId: string;
  content: string;
  locale: 'ar' | 'en';
  isAnswerable: boolean;
  isAnswered: boolean;
  isDeleted: boolean;
  createdOn: string;
  deletedOn: string | null;
  replyCount: number;
}

/** Status filter values for the admin posts list. */
export type AdminPostStatus =
  | 'all'
  | 'active'
  | 'deleted'
  | 'question'
  | 'answered';

export const ADMIN_POST_STATUSES: readonly AdminPostStatus[] = [
  'all', 'active', 'question', 'answered', 'deleted',
] as const;

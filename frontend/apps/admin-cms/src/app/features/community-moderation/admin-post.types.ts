/**
 * Admin-side community post row — mirrors the backend
 * `CCE.Application.Community.Queries.ListAdminPosts.AdminPostRow` DTO.
 */

/** Full post detail fetched from the public community API. */
export interface AdminPostDetail {
  id: string;
  topicId: string;
  topicNameAr: string | null;
  topicNameEn: string | null;
  authorId: string;
  authorName: string | null;
  type: string;
  title: string | null;
  content: string | null;
  locale: string;
  isAnswerable: boolean;
  answeredReplyId: string | null;
  upvoteCount: number;
  downvoteCount: number;
  commentsCount: number;
  createdOn: string;
}

/** Reply fetched from the public community API for a given post. */
export interface AdminPostReply {
  id: string;
  postId: string;
  authorId: string;
  authorName: string | null;
  content: string | null;
  locale: string | null;
  upvoteCount: number;
  isByExpert: boolean;
  createdOn: string;
}

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

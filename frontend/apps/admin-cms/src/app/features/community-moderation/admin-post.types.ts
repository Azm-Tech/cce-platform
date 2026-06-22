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
  /** Post title (preferred for the table). */
  title?: string | null;
  content: string;
  /** Post type: 'Info' | 'Question' | 'Poll'. */
  type?: string;
  locale: 'ar' | 'en';
  isAnswerable: boolean;
  isAnswered: boolean;
  isDeleted: boolean;
  createdOn: string;
  deletedOn: string | null;
  replyCount: number;
}

/** Post-type filter values — mirrors the web-portal feed type filter. */
export type AdminPostTypeFilter = 'all' | 'info' | 'question' | 'poll';

export const ADMIN_POST_TYPE_FILTERS: readonly AdminPostTypeFilter[] = [
  'all', 'info', 'question', 'poll',
] as const;

/** Maps a type filter to the backend `postType` enum (Info=0, Question=1, Poll=2). */
export const POST_TYPE_PARAM: Record<Exclude<AdminPostTypeFilter, 'all'>, 0 | 1 | 2> = {
  info: 0,
  question: 1,
  poll: 2,
};

/** Normalized post type kind for chip rendering. */
export type PostTypeKind = 'info' | 'question' | 'poll';

import type { PagedResult } from '../knowledge-center/shared.types';

export interface PublicTopic {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  slug: string;
  parentId: string | null;
  iconUrl: string | null;
  orderIndex: number;
}

export interface PublicPost {
  id: string;
  topicId: string;
  authorId: string;
  content: string;
  locale: 'ar' | 'en';
  isAnswerable: boolean;
  answeredReplyId: string | null;
  createdOn: string;
}

export interface PublicPostReply {
  id: string;
  postId: string;
  authorId: string;
  content: string;
  locale: 'ar' | 'en';
  parentReplyId: string | null;
  isByExpert: boolean;
  createdOn: string;
}

export interface CreatePostPayload {
  topicId: string;
  content: string;
  locale: 'ar' | 'en';
  isAnswerable: boolean;
}

export interface CreateReplyPayload {
  content: string;
  locale: 'ar' | 'en';
  parentReplyId?: string | null;
}

export type { PagedResult };

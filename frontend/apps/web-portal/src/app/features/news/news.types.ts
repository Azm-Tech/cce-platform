import type { PagedResult } from '../knowledge-center/shared.types';

export interface NewsArticle {
  id: string;
  titleAr: string;
  titleEn: string;
  contentAr: string;
  contentEn: string;
  authorId: string;
  featuredImageUrl: string | null;
  publishedOn: string | null;
  isFeatured: boolean;
  isPublished: boolean;
  topicId?: string | null;
  topicNameAr?: string | null;
  topicNameEn?: string | null;
  tags?: string[];
}

export type { PagedResult };

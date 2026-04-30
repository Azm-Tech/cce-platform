import type { PagedResult } from '../knowledge-center/shared.types';

export interface NewsArticle {
  id: string;
  titleAr: string;
  titleEn: string;
  contentAr: string;
  contentEn: string;
  slug: string;
  authorId: string;
  featuredImageUrl: string | null;
  publishedOn: string | null;
  isFeatured: boolean;
  isPublished: boolean;
}

export type { PagedResult };

import type { PagedResult } from '../knowledge-center/shared.types';

/**
 * Mirrors backend CCE.Application.Search.SearchableType. Wire shape is
 * the enum *string* (System.Text.Json default), e.g. `"type": "News"`.
 */
export type SearchableType = 'News' | 'Events' | 'Resources' | 'Pages' | 'KnowledgeMaps';

export const SEARCHABLE_TYPES: readonly SearchableType[] = [
  'News',
  'Events',
  'Resources',
  'Pages',
  'KnowledgeMaps',
] as const;

export interface SearchHit {
  id: string;
  type: SearchableType;
  titleAr: string;
  titleEn: string;
  excerptAr: string;
  excerptEn: string;
  score: number;
}

export type { PagedResult };

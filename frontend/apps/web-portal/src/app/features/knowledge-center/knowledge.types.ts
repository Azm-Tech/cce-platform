import type { PagedResult } from './shared.types';

export type ResourceType =
  | 'Paper' | 'Article' | 'Study' | 'Presentation' | 'ScientificPaper'
  | 'Report' | 'Book' | 'Research' | 'CceGuide' | 'Media';

export const RESOURCE_TYPES: readonly ResourceType[] = [
  'Paper', 'Article', 'Study', 'Presentation', 'ScientificPaper',
  'Report', 'Book', 'Research', 'CceGuide', 'Media',
];

/** Integer values sent to / received from the backend API. */
export const RESOURCE_TYPE_VALUE: Record<ResourceType, number> = {
  Paper: 0, Article: 1, Study: 2, Presentation: 3, ScientificPaper: 4,
  Report: 5, Book: 6, Research: 7, CceGuide: 8, Media: 9,
};

export const RESOURCE_TYPE_FROM_VALUE: Record<number, ResourceType> = {
  0: 'Paper', 1: 'Article', 2: 'Study', 3: 'Presentation', 4: 'ScientificPaper',
  5: 'Report', 6: 'Book', 7: 'Research', 8: 'CceGuide', 9: 'Media',
};

export function normalizeResourceType(raw: ResourceType | number | string): ResourceType {
  if (typeof raw === 'number') return RESOURCE_TYPE_FROM_VALUE[raw] ?? 'Paper';
  if (typeof raw === 'string' && /^\d+$/.test(raw)) return RESOURCE_TYPE_FROM_VALUE[Number(raw)] ?? 'Paper';
  return raw as ResourceType;
}

export interface ResourceCategory {
  id: string;
  nameAr: string;
  nameEn: string;
  slug: string;
  parentId: string | null;
  orderIndex: number;
}

export interface ResourceListItem {
  id: string;
  titleAr: string;
  titleEn: string;
  descriptionAr?: string;
  descriptionEn?: string;
  resourceType: ResourceType;
  categoryId: string;
  categoryNameAr?: string;
  categoryNameEn?: string;
  countryId?: string | null;
  countryIds?: string[];
  countryNames?: string[];
  assetFileId?: string;
  assetFileName?: string;
  publishedOn: string | null;
  viewCount: number;
}

/** A "key result" highlight card on the resource detail page. */
export interface ResourceHighlight {
  titleAr: string;
  titleEn: string;
  textAr: string;
  textEn: string;
}

export interface Resource extends ResourceListItem {
  descriptionAr: string;
  descriptionEn: string;
  uploadedById: string;
  assetFileId: string;
  isCenterManaged: boolean;
  /** Optional — rendered as the "Key results" grid when present. */
  highlights?: ResourceHighlight[];
}

export type { PagedResult };

import type { PagedResult } from './shared.types';

export type ResourceType = 'Pdf' | 'Video' | 'Image' | 'Link' | 'Document';

export const RESOURCE_TYPES: readonly ResourceType[] = ['Pdf', 'Video', 'Image', 'Link', 'Document'];

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
  resourceType: ResourceType;
  categoryId: string;
  countryId: string | null;
  publishedOn: string | null;
  viewCount: number;
}

export interface Resource extends ResourceListItem {
  descriptionAr: string;
  descriptionEn: string;
  uploadedById: string;
  assetFileId: string;
  isCenterManaged: boolean;
}

export type { PagedResult };

/**
 * Hand-defined DTOs for the Content (resources + assets + country-resource-requests)
 * feature. Mirrors CCE.Application.Content.Dtos.*.
 */

import type { PagedResult } from '../identity/identity.types';

export type ResourceType =
  | 'Paper' | 'Article' | 'Study' | 'Presentation' | 'ScientificPaper'
  | 'Report' | 'Book' | 'Research' | 'CceGuide' | 'Media';

export const RESOURCE_TYPES: readonly ResourceType[] = [
  'Paper', 'Article', 'Study', 'Presentation', 'ScientificPaper',
  'Report', 'Book', 'Research', 'CceGuide', 'Media',
];

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

export type VirusScanStatus = 'Pending' | 'Clean' | 'Infected' | 'Failed';

export type CountryResourceRequestStatus = 'Pending' | 'Approved' | 'Rejected';

export enum AdminContentType {
  Resource = 'resource',
  News = 'news',
  Event = 'event',
}

export const ADMIN_CONTENT_TYPE_API_VALUE: Record<AdminContentType, number> = {
  [AdminContentType.Resource]: 0,
  [AdminContentType.News]: 1,
  [AdminContentType.Event]: 2,
};

export function adminContentTypeFromApiValue(n: number): AdminContentType {
  switch (n) {
    case 1: return AdminContentType.News;
    case 2: return AdminContentType.Event;
    default: return AdminContentType.Resource;
  }
}

export const AdminContentRequestStatus = { Pending: 0, Approved: 1, Rejected: 2 } as const;
export type AdminContentRequestStatusValue = typeof AdminContentRequestStatus[keyof typeof AdminContentRequestStatus];

export function adminContentRequestStatusKey(s: AdminContentRequestStatusValue): 'pending' | 'approved' | 'rejected' {
  if (s === AdminContentRequestStatus.Approved) return 'approved';
  if (s === AdminContentRequestStatus.Rejected) return 'rejected';
  return 'pending';
}

/** Unified DTO — CountryContentRequestDto from the internal API */
export interface AdminCountryContentRequest {
  id: string;
  countryId: string;
  requestedById: string;
  type: AdminContentType;
  status: AdminContentRequestStatusValue;
  proposedTitleAr: string | null;
  proposedTitleEn: string | null;
  proposedDescriptionAr: string | null;
  proposedDescriptionEn: string | null;
  proposedResourceType: number | null;
  proposedAssetFileId: string | null;
  proposedTopicId: string | null;
  proposedStartsOn: string | null;
  proposedEndsOn: string | null;
  proposedLocationAr: string | null;
  proposedLocationEn: string | null;
  proposedOnlineMeetingUrl: string | null;
  submittedOn: string;
  adminNotesAr: string | null;
  adminNotesEn: string | null;
  processedById: string | null;
  processedOn: string | null;
}

export interface Resource {
  id: string;
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  resourceType: ResourceType;
  /** Localized Arabic label for the resource type — returned by live API. */
  resourceTypeAr?: string | null;
  categoryId: string;
  /** Localized category names — returned by the list/detail API. */
  categoryNameAr?: string;
  categoryNameEn?: string;
  topicId: string | null;
  countryIds: string[];
  uploadedById: string;
  publishedBy?: string | null;
  assetFileId: string;
  publishedOn: string | null;
  viewCount: number;
  isCenterManaged: boolean;
  isPublished: boolean;
  rowVersion: string;
}

export interface AssetFile {
  id: string;
  url: string;
  originalFileName: string;
  sizeBytes: number;
  mimeType: string;
  uploadedById: string;
  uploadedOn: string;
  virusScanStatus: VirusScanStatus;
  scannedOn: string | null;
}

export interface CreateResourceBody {
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  resourceType: ResourceType;
  categoryId: string;
  topicId?: string | null;
  countryIds?: string[];
  assetFileId: string;
}

export interface UpdateResourceBody {
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  resourceType: ResourceType;
  categoryId: string;
  topicId?: string | null;
  countryIds?: string[];
  rowVersion: string;
}

export interface CountryResourceRequest {
  id: string;
  countryId: string;
  requestedById: string;
  status: CountryResourceRequestStatus;
  proposedTitleAr: string;
  proposedTitleEn: string;
  proposedDescriptionAr: string;
  proposedDescriptionEn: string;
  proposedResourceType: ResourceType;
  proposedAssetFileId: string;
  submittedOn: string;
  adminNotesAr: string | null;
  adminNotesEn: string | null;
  processedById: string | null;
  processedOn: string | null;
}

export interface ApproveCountryResourceRequestBody {
  adminNotesAr?: string | null;
  adminNotesEn?: string | null;
}
export interface RejectCountryResourceRequestBody {
  adminNotesAr: string;
  adminNotesEn: string;
}

export type { PagedResult };

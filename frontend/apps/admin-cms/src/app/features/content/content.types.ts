/**
 * Hand-defined DTOs for the Content (resources + assets + country-resource-requests)
 * feature. Mirrors CCE.Application.Content.Dtos.*.
 */

import type { PagedResult } from '../identity/identity.types';

export type ResourceType = 'Pdf' | 'Video' | 'Image' | 'Link' | 'Document';
export const RESOURCE_TYPES: readonly ResourceType[] = ['Pdf', 'Video', 'Image', 'Link', 'Document'];

export type VirusScanStatus = 'Pending' | 'Clean' | 'Infected' | 'Failed';

export type CountryResourceRequestStatus = 'Pending' | 'Approved' | 'Rejected';

export interface Resource {
  id: string;
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  resourceType: ResourceType;
  categoryId: string;
  countryId: string | null;
  uploadedById: string;
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
  countryId?: string | null;
  assetFileId: string;
}

export interface UpdateResourceBody {
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  resourceType: ResourceType;
  categoryId: string;
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

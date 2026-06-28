import type { PagedResult } from '../identity/identity.types';

export interface Country {
  id: string;
  isoAlpha3: string;
  isoAlpha2: string;
  nameAr: string;
  nameEn: string;
  regionAr: string;
  regionEn: string;
  flagUrl: string;
  dialCode: string;
  isActive: boolean;
  isCceCountry: boolean;
  cceClassification: string | null;
  ccePerformanceScore: number | null;
  cceTotalIndex: number | null;
}


export interface UpdateCountryBody {
  nameAr: string;
  nameEn: string;
  regionAr: string;
  regionEn: string;
  isActive: boolean;
}

/** NDC document as returned by the profile GET (read shape). The PUT body
 *  accepts just the `ndcAssetId` string. */
export interface NdcDocument {
  assetId: string;
  originalFileName: string;
}

export interface CountryProfile {
  id: string;
  countryId: string;
  descriptionAr: string;
  descriptionEn: string;
  keyInitiativesAr: string;
  keyInitiativesEn: string;
  contactInfoAr: string | null;
  contactInfoEn: string | null;
  population: number | null;
  areaSqKm: number | null;
  gdpPerCapita: number | null;
  /** Read with `ndcDocument?.assetId ?? ndcAssetId` — endpoints vary. */
  ndcDocument: NdcDocument | null;
  ndcAssetId: string | null;
  lastUpdatedById: string;
  lastUpdatedOn: string;
  rowVersion: string;
}

export interface UpsertCountryProfileBody {
  descriptionAr: string;
  descriptionEn: string;
  keyInitiativesAr: string;
  keyInitiativesEn: string;
  contactInfoAr?: string | null;
  contactInfoEn?: string | null;
  population?: number | null;
  areaSqKm?: number | null;
  gdpPerCapita?: number | null;
  ndcAssetId?: string | null;
  rowVersion: string;
}

export type { PagedResult };

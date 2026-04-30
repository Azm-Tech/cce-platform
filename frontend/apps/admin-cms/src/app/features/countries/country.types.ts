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
  isActive: boolean;
}

export interface UpdateCountryBody {
  nameAr: string;
  nameEn: string;
  regionAr: string;
  regionEn: string;
  isActive: boolean;
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
  rowVersion: string;
}

export type { PagedResult };

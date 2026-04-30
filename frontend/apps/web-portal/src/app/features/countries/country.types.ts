export interface Country {
  id: string;
  isoAlpha3: string;
  isoAlpha2: string;
  nameAr: string;
  nameEn: string;
  regionAr: string;
  regionEn: string;
  flagUrl: string;
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
  lastUpdatedOn: string;
}

export interface KapsarcSnapshot {
  id: string;
  countryId: string;
  classification: string;
  performanceScore: number;
  totalIndex: number;
  snapshotTakenOn: string;
  sourceVersion: string | null;
}

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

  /** Optional richer detail — present on mock data, may be added to
   *  real backend payloads in a future API revision. */
  subScores?: KapsarcSubScores;
  /** Year-over-year change in totalIndex (positive = improving). */
  trendYoY?: number;
  /** Rank within the regional cohort (1 = best). */
  regionalRank?: number;
  /** Total countries in the same regional cohort. */
  regionalCohortSize?: number;
  /** Renewable-energy share of the national grid, in percent. */
  renewableSharePct?: number;
  /** Energy intensity (TJ per million USD of GDP). Lower = better. */
  energyIntensity?: number;
  /** Carbon intensity (tCO₂e per million USD of GDP). Lower = better. */
  carbonIntensity?: number;
}

export interface KapsarcSubScores {
  /** Decarbonisation of electricity / power generation. */
  power: number;
  /** Industrial-process efficiency + heat. */
  industry: number;
  /** Transport electrification + mode shift. */
  transport: number;
  /** Building-stock retrofit + new-build standards. */
  buildings: number;
  /** Carbon-sink restoration, land-use, agriculture. */
  landUse: number;
}

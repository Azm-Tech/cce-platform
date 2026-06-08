export interface CountryMeta {
  populationMillions: number;
  populationTrend: number;
  areaMillionKm2: number;
  administrativeDivisions: number;
  gdpPerCapita: number;
  gdpTrend: number;
  energyDensityPerMJ: number;
  isFoundingPartner: boolean;
}

export interface CountryCardStats {
  emissionReductionPct: number;
  emissionTrend: 'up' | 'down' | 'flat';
  globalRank: number;
  totalCountries: number;
  cceClassification: string;
}

export interface CountryAchievement {
  titleAr: string;
  titleEn: string;
  descAr: string;
  descEn: string;
  date: string;
}

export interface CountryCode {
  id: string;
  dialCode: string;
  name: { ar: string; en: string };
  isActive: boolean;
}

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
  countryId: string;
  isoAlpha3: string;
  nameAr: string;
  nameEn: string;
  flagUrl: string | null;
  descriptionAr: string;
  descriptionEn: string;
  keyInitiativesAr: string | null;
  keyInitiativesEn: string | null;
  contactInfoAr: string | null;
  contactInfoEn: string | null;
  population: number | null;
  areaSqKm: number | null;
  gdpPerCapita: number | null;
  ndcDocument: string | null;
  cceClassification: string | null;
  ccePerformanceScore: number | null;
  cceTotalIndex: number | null;
  cceSnapshotTakenOn: string | null;
  lastUpdatedOn: string | null;
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

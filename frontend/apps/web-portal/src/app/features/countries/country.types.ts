export enum ContentType {
  Resource = 'resource',
  News     = 'news',
  Event    = 'event',
}

export const CONTENT_TYPE_API_VALUE: Record<ContentType, number> = {
  [ContentType.Resource]: 0,
  [ContentType.News]:     1,
  [ContentType.Event]:    2,
};

export function contentTypeFromApiValue(n: number): ContentType {
  switch (n) {
    case 1:  return ContentType.News;
    case 2:  return ContentType.Event;
    default: return ContentType.Resource;
  }
}

export const ContentRequestStatus = { Pending: 0, Approved: 1, Rejected: 2 } as const;
export type ContentRequestStatusValue = typeof ContentRequestStatus[keyof typeof ContentRequestStatus];

export function contentRequestStatusKey(status: ContentRequestStatusValue): 'pending' | 'approved' | 'rejected' {
  if (status === ContentRequestStatus.Approved) return 'approved';
  if (status === ContentRequestStatus.Rejected) return 'rejected';
  return 'pending';
}

/** Unified DTO returned by GET /api/state/requests and GET /api/state/requests/{id} */
export interface CountryContentRequest {
  id: string;
  countryId: string;
  requestedById: string;
  type: ContentType;
  status: ContentRequestStatusValue;
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

/** NDC document as returned by profile GET endpoints (read shape). The PUT
 *  bodies accept just the `ndcAssetId` string. */
export interface NdcDocument {
  assetId: string;
  originalFileName: string;
}

/** State rep country profile — GET /api/state/profile */
export interface StateProfile {
  id: string;
  countryId: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  keyInitiativesAr: string | null;
  keyInitiativesEn: string | null;
  contactInfoAr: string | null;
  contactInfoEn: string | null;
  population: number | null;
  areaSqKm: number | null;
  gdpPerCapita: number | null;
  /** Some endpoints return the NDC as an object, others as a bare id —
   *  read with `ndcDocument?.assetId ?? ndcAssetId`. */
  ndcDocument: NdcDocument | null;
  ndcAssetId: string | null;
  cceClassification: string | null;
  ccePerformanceScore: number | null;
  cceTotalIndex: number | null;
  cceSnapshotTakenOn: string | null;
  lastUpdatedById: string;
  lastUpdatedOn: string;
}

/** Body for PUT /api/state/profile/{countryId} */
export interface UpdateStateProfileBody {
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  keyInitiativesAr?: string | null;
  keyInitiativesEn?: string | null;
  contactInfoAr?: string | null;
  contactInfoEn?: string | null;
  population?: number | null;
  areaSqKm?: number | null;
  gdpPerCapita?: number | null;
  ndcAssetId?: string | null;
}

export interface SubmitRequestContent {
  type: ContentType;
  titleAr: string;
  titleEn: string;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
  resourceType?: number | null;
  categoryId?: string | null;
  assetFileId?: string | null;
  countryIds?: string[] | null;
  startsOn?: string | null;
  endsOn?: string | null;
  locationAr?: string | null;
  locationEn?: string | null;
  onlineMeetingUrl?: string | null;
  contentAr?: string | null;
  contentEn?: string | null;
  featuredImageAssetId?: string | null;
  topicId?: string | null;
  knowledgeLevelId?: string | null;
  jobSectorId?: string | null;
}

/** Body for POST /api/state/requests */
export interface SubmitRequestBody {
  countryId: string;
  content: SubmitRequestContent;
}

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
  isCceCountry: boolean;
  cceClassification: string | null;
  ccePerformanceScore: number | null;
  cceTotalIndex: number | null;
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
  ndcDocument: NdcDocument | null;
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

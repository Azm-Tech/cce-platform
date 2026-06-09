/**
 * Mirrors backend CCE.Domain.Identity.KnowledgeLevel. Wire shape is
 * the enum *string* (System.Text.Json default).
 */
export type KnowledgeLevel = 'Beginner' | 'Intermediate' | 'Advanced';

export const KNOWLEDGE_LEVELS: readonly KnowledgeLevel[] = [
  'Beginner',
  'Intermediate',
  'Advanced',
] as const;

/**
 * Mirrors backend CCE.Domain.Identity.ExpertRegistrationStatus.
 */
export type ExpertRegistrationStatus = 'Pending' | 'Approved' | 'Rejected';

export interface UserProfile {
  id: string;
  email: string | null;
  userName: string | null;
  firstName: string | null;
  lastName: string | null;
  phoneNumber: string | null;
  jobTitle: string | null;
  organizationName: string | null;
  localePreference: string;
  knowledgeLevel: KnowledgeLevel;
  interests: string[];
  countryId: string | null;
  countryCodeId: string | null;
  avatarUrl: string | null;
}

export interface UpdateMyProfilePayload {
  firstName: string;
  lastName: string;
  jobTitle: string;
  organizationName: string;
  localePreference: string;
  knowledgeLevel: KnowledgeLevel;
  interests?: string[];
  avatarUrl?: string | null;
  countryId?: string | null;
  countryCodeId?: string | null;
}

export interface ExpertRequestStatus {
  id: string;
  requestedById: string;
  requestedBioAr: string;
  requestedBioEn: string;
  requestedTags: string[];
  submittedOn: string;
  status: ExpertRegistrationStatus;
  processedOn: string | null;
  rejectionReasonAr: string | null;
  rejectionReasonEn: string | null;
}

export interface SubmitExpertRequestPayload {
  requestedBioAr: string;
  requestedBioEn: string;
  requestedTags?: string[];
  cvAssetFileId?: string | null;
}

export interface ServiceRatingPayload {
  rating: number;
  commentAr?: string | null;
  commentEn?: string | null;
  page: string;
  locale: 'ar' | 'en';
}

export interface EvaluationPayload {
  overallSatisfaction?: number;
  easeOfUse?: number;
  contentSuitability?: number;
  feedback?: string | null;
}

export type PersonalizedKnowledgeLevel = 'High' | 'Medium' | 'Low';
export type SectorOfWork = 'Government' | 'Academic' | 'Private';

export const PERSONALIZED_KNOWLEDGE_LEVELS: readonly PersonalizedKnowledgeLevel[] =
  ['High', 'Medium', 'Low'] as const;

export const SECTORS_OF_WORK: readonly SectorOfWork[] =
  ['Government', 'Academic', 'Private'] as const;

/** Payload for the US019 dedicated preferences endpoint (not yet deployed). */
export interface PersonalizedSuggestionsPayload {
  interests: string[];
  knowledgeLevel: PersonalizedKnowledgeLevel;
  sectorOfWork: SectorOfWork;
  countryId: string;
}

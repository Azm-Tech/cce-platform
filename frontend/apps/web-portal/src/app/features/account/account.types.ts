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
  localePreference: string;
  knowledgeLevel: KnowledgeLevel;
  interests: string[];
  countryId: string | null;
  avatarUrl: string | null;
}

export interface UpdateMyProfilePayload {
  localePreference: string;
  knowledgeLevel: KnowledgeLevel;
  interests?: string[];
  avatarUrl?: string | null;
  countryId?: string | null;
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
}

export interface ServiceRatingPayload {
  rating: number;
  commentAr?: string | null;
  commentEn?: string | null;
  page: string;
  locale: 'ar' | 'en';
}

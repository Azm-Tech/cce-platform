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

export type InterestTopicCategory = 'carbon_area' | 'knowledge_assessment' | 'job_sector';

export interface InterestTopicOption {
  id: string;
  nameAr: string;
  nameEn: string;
  category: InterestTopicCategory;
  isActive: boolean;
}

export interface InterestQuestion {
  category: InterestTopicCategory;
  titleAr: string;
  titleEn: string;
  type: 'multiple' | 'single';
  options: InterestTopicOption[];
}

export interface MyInterests {
  carbonAreaTopics: InterestTopicOption[];
  knowledgeAssessmentTopic: InterestTopicOption | null;
  jobSectorTopic: InterestTopicOption | null;
  targetCountryId: string | null;
}

export interface UpdateMyInterestsPayload {
  carbonAreaIds: string[];
  knowledgeAssessmentId: string;
  jobSectorId: string;
  targetCountryId: string;
}

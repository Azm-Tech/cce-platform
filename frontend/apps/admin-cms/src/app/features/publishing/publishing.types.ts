/** DTOs for the Publishing feature (News + Events + Pages + Homepage). */

import type { PagedResult } from '../identity/identity.types';

export type PageType = 'AboutPlatform' | 'TermsOfService' | 'PrivacyPolicy' | 'Custom';
export const PAGE_TYPES: readonly PageType[] = ['AboutPlatform', 'TermsOfService', 'PrivacyPolicy', 'Custom'];

export type HomepageSectionType = 'Hero' | 'FeaturedNews' | 'FeaturedResources' | 'UpcomingEvents';
export const HOMEPAGE_SECTION_TYPES: readonly HomepageSectionType[] = [
  'Hero',
  'FeaturedNews',
  'FeaturedResources',
  'UpcomingEvents',
];

export interface News {
  id: string;
  titleAr: string;
  titleEn: string;
  contentAr: string;
  contentEn: string;
  slug: string;
  authorId: string;
  featuredImageUrl: string | null;
  publishedOn: string | null;
  isFeatured: boolean;
  isPublished: boolean;
  rowVersion: string;
}

export interface CreateNewsBody {
  titleAr: string;
  titleEn: string;
  contentAr: string;
  contentEn: string;
  slug: string;
  featuredImageUrl?: string | null;
}

export interface UpdateNewsBody extends CreateNewsBody {
  rowVersion: string;
}

export interface Event {
  id: string;
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  startsOn: string;
  endsOn: string;
  locationAr: string | null;
  locationEn: string | null;
  onlineMeetingUrl: string | null;
  featuredImageUrl: string | null;
  iCalUid: string;
  rowVersion: string;
}

export interface CreateEventBody {
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  startsOn: string;
  endsOn: string;
  locationAr?: string | null;
  locationEn?: string | null;
  onlineMeetingUrl?: string | null;
  featuredImageUrl?: string | null;
}

export interface UpdateEventBody {
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  locationAr?: string | null;
  locationEn?: string | null;
  onlineMeetingUrl?: string | null;
  featuredImageUrl?: string | null;
  rowVersion: string;
}

export interface RescheduleEventBody {
  startsOn: string;
  endsOn: string;
  rowVersion: string;
}

export interface Page {
  id: string;
  slug: string;
  pageType: PageType;
  titleAr: string;
  titleEn: string;
  contentAr: string;
  contentEn: string;
  rowVersion: string;
}

export interface CreatePageBody {
  slug: string;
  pageType: PageType;
  titleAr: string;
  titleEn: string;
  contentAr: string;
  contentEn: string;
}

export interface UpdatePageBody {
  titleAr: string;
  titleEn: string;
  contentAr: string;
  contentEn: string;
  rowVersion: string;
}

export interface HomepageSection {
  id: string;
  sectionType: HomepageSectionType;
  orderIndex: number;
  contentAr: string;
  contentEn: string;
  isActive: boolean;
}

export interface CreateHomepageSectionBody {
  sectionType: HomepageSectionType;
  orderIndex: number;
  contentAr: string;
  contentEn: string;
}

export interface UpdateHomepageSectionBody {
  contentAr: string;
  contentEn: string;
  isActive: boolean;
}

export interface ReorderHomepageSectionsBody {
  assignments: { id: string; orderIndex: number }[];
}

// ---- Platform Settings ----

export interface HomepageSettings {
  videoUrl: string | null;
  objectiveAr: string | null;
  objectiveEn: string | null;
  cceConceptsAr: string | null;
  cceConceptsEn: string | null;
  participatingCountryIds: string[];
}

export interface UpdateHomepageSettingsBody {
  videoUrl?: string | null;
  objectiveAr?: string | null;
  objectiveEn?: string | null;
  cceConceptsAr?: string | null;
  cceConceptsEn?: string | null;
  participatingCountryIds?: string[];
}

export interface GlossaryTerm {
  id: string;
  termAr: string;
  termEn: string;
  definitionAr: string;
  definitionEn: string;
}

export interface GlossaryTermBody {
  termAr: string;
  termEn: string;
  definitionAr: string;
  definitionEn: string;
}

export interface KnowledgePartner {
  id: string;
  nameAr: string;
  nameEn: string;
  logoUrl: string | null;
  websiteUrl: string | null;
  descriptionAr: string | null;
  descriptionEn: string | null;
}

export interface KnowledgePartnerBody {
  nameAr: string;
  nameEn: string;
  logoUrl?: string | null;
  websiteUrl?: string | null;
  descriptionAr?: string | null;
  descriptionEn?: string | null;
}

export interface AboutSettings {
  descriptionAr: string;
  descriptionEn: string;
  howToUseVideoUrl: string | null;
  glossaryTerms: GlossaryTerm[];
  knowledgePartners: KnowledgePartner[];
}

export interface UpdateAboutSettingsBody {
  descriptionAr?: string;
  descriptionEn?: string;
  howToUseVideoUrl?: string | null;
}

export interface ApiPolicySection {
  id: string;
  type: number;
  title: { ar: string; en: string };
  content: { ar: string; en: string };
  orderIndex: number;
}

export interface PolicySection {
  id: string;
  type: number;
  titleAr: string;
  titleEn: string;
  contentAr: string;
  contentEn: string;
  orderIndex: number;
}

export interface PolicySectionBody {
  type?: number;
  titleAr: string;
  titleEn: string;
  contentAr: string;
  contentEn: string;
}

export interface PoliciesSettings {
  sections: PolicySection[];
}

export type { PagedResult };

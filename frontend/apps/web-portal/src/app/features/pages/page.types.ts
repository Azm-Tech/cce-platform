export type PageType = 'AboutPlatform' | 'TermsOfService' | 'PrivacyPolicy' | 'Custom';

export interface PublicPage {
  id: string;
  slug: string;
  pageType: PageType;
  titleAr: string;
  titleEn: string;
  contentAr: string;
  contentEn: string;
}

export interface GlossaryTerm {
  id: string;
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

export interface AboutContent {
  descriptionAr: string;
  descriptionEn: string;
  howToUseVideoUrl: string | null;
  glossaryTerms: GlossaryTerm[];
  knowledgePartners: KnowledgePartner[];
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

export interface PoliciesContent {
  sections: PolicySection[];
}

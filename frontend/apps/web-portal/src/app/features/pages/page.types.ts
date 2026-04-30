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

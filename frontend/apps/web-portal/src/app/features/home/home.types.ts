export type HomepageSectionType =
  | 'Hero'
  | 'FeaturedNews'
  | 'FeaturedResources'
  | 'UpcomingEvents'
  | 'NewsletterSignup';

export interface HomepageSection {
  id: string;
  sectionType: HomepageSectionType;
  orderIndex: number;
  contentAr: string;
  contentEn: string;
  isActive: boolean;
}

export interface ParticipatingCountry {
  id: string;
  nameAr: string;
  nameEn: string;
  flagUrl: string | null;
}

export interface HomepageSettings {
  videoUrl: string | null;
  objectiveAr: string | null;
  objectiveEn: string | null;
  cceConceptsAr: string | null;
  cceConceptsEn: string | null;
  participatingCountries: ParticipatingCountry[];
  sections: HomepageSection[];
}

export type HomepageSectionType = 'Hero' | 'FeaturedNews' | 'FeaturedResources' | 'UpcomingEvents';

export interface HomepageSection {
  id: string;
  sectionType: HomepageSectionType;
  orderIndex: number;
  contentAr: string;
  contentEn: string;
  isActive: boolean;
}

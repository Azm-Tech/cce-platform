import type { PagedResult } from '../knowledge-center/shared.types';

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
}

export type { PagedResult };

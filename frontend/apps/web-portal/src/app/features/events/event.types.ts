import type { PagedResult } from '../knowledge-center/shared.types';

/** Speaker entry — optional; backend does not send these yet. The detail
 *  page renders the speakers grid only when the array is non-empty. */
export interface EventSpeaker {
  nameAr: string;
  nameEn: string;
  roleAr: string | null;
  roleEn: string | null;
  imageUrl: string | null;
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
  /** Topic categorization — returned by the API; shown as a chip (US010 AC3). */
  topicId?: string | null;
  topicNameAr?: string | null;
  topicNameEn?: string | null;
  tags?: string[];
  /** Optional — not in the API yet; UI hides the section when absent. */
  speakers?: EventSpeaker[] | null;
  /** Optional — not in the API yet; UI hides the Outcomes tab when absent. */
  outcomesAr?: string | null;
  outcomesEn?: string | null;
}

export type { PagedResult };

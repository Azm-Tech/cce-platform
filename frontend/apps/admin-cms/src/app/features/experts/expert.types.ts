/**
 * Hand-defined DTOs for the Expert workflow feature. Mirrors
 * CCE.Application.Identity.Dtos.Expert{Request,Profile}Dto.
 */

import type { PagedResult } from '../identity/identity.types';

export type ExpertRegistrationStatus = 'Pending' | 'Approved' | 'Rejected';

export const EXPERT_STATUSES: readonly ExpertRegistrationStatus[] = [
  'Pending',
  'Approved',
  'Rejected',
];

export interface ExpertRequest {
  id: string;
  requestedById: string;
  requestedByUserName: string | null;
  requestedBioAr: string;
  requestedBioEn: string;
  requestedTags: string[];
  submittedOn: string;
  status: ExpertRegistrationStatus;
  processedById: string | null;
  processedOn: string | null;
  rejectionReasonAr: string | null;
  rejectionReasonEn: string | null;
}

export interface ExpertProfile {
  id: string;
  userId: string;
  userName: string | null;
  bioAr: string;
  bioEn: string;
  expertiseTags: string[];
  academicTitleAr: string;
  academicTitleEn: string;
  approvedOn: string;
  approvedById: string;
}

export interface ApproveExpertRequestBody {
  academicTitleAr: string;
  academicTitleEn: string;
}

export interface RejectExpertRequestBody {
  rejectionReasonAr: string;
  rejectionReasonEn: string;
}

export type { PagedResult };

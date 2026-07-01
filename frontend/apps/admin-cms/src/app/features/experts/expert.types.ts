/**
 * Hand-defined DTOs for the Expert workflow feature. Mirrors
 * CCE.Application.Identity.Dtos.Expert{Request,Profile}Dto.
 */

import type { StatusBadgeConfig } from '@frontend/ui-kit';
import type { PagedResult } from '../identity/identity.types';

export type ExpertRegistrationStatus = 'Pending' | 'Approved' | 'Rejected';

export const EXPERT_STATUSES: readonly ExpertRegistrationStatus[] = [
  'Pending',
  'Approved',
  'Rejected',
];

/** Badge colour + label per status. Keyed lowercase to match the API values;
 *  the badge matches case-insensitively so the capitalized enum works too. */
export const EXPERT_STATUS_BADGES: StatusBadgeConfig = {
  pending: { tone: 'warning', labelKey: 'experts.status.pending' },
  approved: { tone: 'success', labelKey: 'experts.status.approved' },
  rejected: { tone: 'danger', labelKey: 'experts.status.rejected' },
};

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
  cvAssetFileId: string | null;
  cvUrl: string | null;
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

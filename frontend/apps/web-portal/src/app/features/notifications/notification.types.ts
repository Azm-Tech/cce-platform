import type { PagedResult } from '../knowledge-center/shared.types';

export type NotificationStatus = 'Pending' | 'Sent' | 'Failed' | 'Read';
export type NotificationChannel = 'Email' | 'Sms' | 'InApp';

export interface UserNotification {
  id: string;
  templateId: string;
  renderedSubjectAr: string;
  renderedSubjectEn: string;
  renderedBody: string;
  renderedLocale: string;
  channel: NotificationChannel;
  sentOn: string | null;
  readOn: string | null;
  status: NotificationStatus;
}

export type { PagedResult };

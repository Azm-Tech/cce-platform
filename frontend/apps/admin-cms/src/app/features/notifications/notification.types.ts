import type { PagedResult } from '../identity/identity.types';

export type NotificationChannel = 'Email' | 'Sms' | 'InApp';
export const NOTIFICATION_CHANNELS: readonly NotificationChannel[] = ['Email', 'Sms', 'InApp'];

export interface NotificationTemplate {
  id: string;
  code: string;
  subjectAr: string;
  subjectEn: string;
  bodyAr: string;
  bodyEn: string;
  channel: NotificationChannel;
  variableSchemaJson: string;
  isActive: boolean;
}

export interface CreateNotificationTemplateBody {
  code: string;
  subjectAr: string;
  subjectEn: string;
  bodyAr: string;
  bodyEn: string;
  channel: NotificationChannel;
  variableSchemaJson: string;
}

export interface UpdateNotificationTemplateBody {
  subjectAr: string;
  subjectEn: string;
  bodyAr: string;
  bodyEn: string;
  isActive: boolean;
}

export type { PagedResult };

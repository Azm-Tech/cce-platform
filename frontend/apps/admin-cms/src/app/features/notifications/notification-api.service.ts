import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  CreateNotificationTemplateBody,
  NotificationChannel,
  NotificationTemplate,
  PagedResult,
  UpdateNotificationTemplateBody,
} from './notification.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class NotificationApiService {
  private readonly http = inject(HttpClient);

  async list(opts: { page?: number; pageSize?: number; channel?: NotificationChannel; isActive?: boolean } = {}): Promise<Result<PagedResult<NotificationTemplate>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.channel) params = params.set('channel', opts.channel);
    if (opts.isActive !== undefined) params = params.set('isActive', String(opts.isActive));
    return this.run(() => firstValueFrom(
      this.http.get<PagedResult<NotificationTemplate>>('/api/admin/notification-templates', { params }),
    ));
  }

  async create(body: CreateNotificationTemplateBody): Promise<Result<NotificationTemplate>> {
    return this.run(() =>
      firstValueFrom(this.http.post<NotificationTemplate>('/api/admin/notification-templates', body)),
    );
  }

  async update(id: string, body: UpdateNotificationTemplateBody): Promise<Result<NotificationTemplate>> {
    return this.run(() =>
      firstValueFrom(this.http.put<NotificationTemplate>(`/api/admin/notification-templates/${id}`, body)),
    );
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      const error = toFeatureError(err as HttpErrorResponse);
      return { ok: false, error };
    }
  }
}

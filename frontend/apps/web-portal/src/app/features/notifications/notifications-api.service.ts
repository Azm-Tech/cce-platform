import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { NotificationStatus, PagedResult, UserNotification } from './notification.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);

  async list(opts: {
    page?: number;
    pageSize?: number;
    status?: NotificationStatus;
  } = {}): Promise<Result<PagedResult<UserNotification>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.status) params = params.set('status', opts.status);
    return this.run(() =>
      firstValueFrom(
        this.http.get<PagedResult<UserNotification>>('/api/me/notifications', { params }),
      ),
    );
  }

  async getUnreadCount(): Promise<Result<number>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<{ count: number }>('/api/me/notifications/unread-count'),
      );
      return res.count;
    });
  }

  async markRead(id: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.post(`/api/me/notifications/${encodeURIComponent(id)}/mark-read`, {}),
      );
    });
  }

  async markAllRead(): Promise<Result<number>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.post<{ marked: number }>('/api/me/notifications/mark-all-read', {}),
      );
      return res.marked;
    });
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

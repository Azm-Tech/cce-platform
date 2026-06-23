import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { NotificationStatus, PagedResult, UserNotification } from './notification.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

/** Web-portal responses are wrapped in `{ data: T }`. This unwraps them. */
function unwrap<T>(res: { data?: T } | T | null | undefined): T {
  if (res && typeof res === 'object' && 'data' in (res as object)) {
    const inner = (res as { data?: T }).data;
    if (inner !== undefined && inner !== null) return inner;
  }
  return res as T;
}

function unwrapPaged<T>(
  res: { data?: PagedResult<T> } | PagedResult<T> | null | undefined,
): PagedResult<T> {
  const inner = unwrap<PagedResult<T>>(res);
  if (inner && Array.isArray((inner as PagedResult<T>).items)) return inner as PagedResult<T>;
  return { items: [], total: 0, page: 1, pageSize: 0 };
}

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
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<{ data?: PagedResult<UserNotification> } | PagedResult<UserNotification>>(
          '/api/me/notifications',
          { params },
        ),
      );
      return unwrapPaged<UserNotification>(res);
    });
  }

  async getUnreadCount(): Promise<Result<number>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<{ data?: { count: number } } | { count: number }>(
          '/api/me/notifications/unread-count',
        ),
      );
      return unwrap<{ count: number }>(res)?.count ?? 0;
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
        this.http.post<{ data?: { marked: number } } | { marked: number }>(
          '/api/me/notifications/mark-all-read',
          {},
        ),
      );
      return unwrap<{ marked: number }>(res)?.marked ?? 0;
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

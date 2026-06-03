import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { Event, PagedResult } from './event.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class EventsApiService {
  private readonly http = inject(HttpClient);

  async listEvents(opts: {
    page?: number;
    pageSize?: number;
    from?: string;
    to?: string;
  } = {}): Promise<Result<PagedResult<Event>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.from) params = params.set('from', opts.from);
    if (opts.to) params = params.set('to', opts.to);
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<PagedResult<Event> | { data?: PagedResult<Event> }>(
          '/api/events',
          { params },
        ),
      );
      return unwrapPaged<Event>(res);
    });
  }

  async getEvent(id: string): Promise<Result<Event>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<Event | { data?: Event }>(`/api/events/${id}`),
      );
      return unwrapSingle<Event>(res);
    });
  }

  async downloadIcs(id: string): Promise<Result<Blob>> {
    try {
      const value = await firstValueFrom(
        this.http.get(`/api/events/${id}.ics`, { responseType: 'blob' }),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

function unwrapPaged<T>(
  res: PagedResult<T> | { data?: PagedResult<T> } | null | undefined,
): PagedResult<T> {
  if (!res) return { items: [], total: 0, page: 1, pageSize: 0 };
  if ('items' in res && Array.isArray((res as PagedResult<T>).items)) return res as PagedResult<T>;
  const inner = (res as { data?: PagedResult<T> }).data;
  if (inner && Array.isArray(inner.items)) return inner;
  return { items: [], total: 0, page: 1, pageSize: 0 };
}

function unwrapSingle<T>(res: T | { data?: T } | null | undefined): T {
  if (res && typeof res === 'object' && 'data' in (res as object)) {
    const inner = (res as { data?: T }).data;
    if (inner !== undefined && inner !== null) return inner;
  }
  return res as T;
}

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
    return this.run(() =>
      firstValueFrom(this.http.get<PagedResult<Event>>('/api/events', { params })),
    );
  }

  async getEvent(id: string): Promise<Result<Event>> {
    return this.run(() => firstValueFrom(this.http.get<Event>(`/api/events/${id}`)));
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

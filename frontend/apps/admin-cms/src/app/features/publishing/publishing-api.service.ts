import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '../../core/ui/error-formatter';
import type {
  CreateEventBody,
  CreateHomepageSectionBody,
  CreateNewsBody,
  CreatePageBody,
  Event,
  HomepageSection,
  News,
  PagedResult,
  Page,
  RescheduleEventBody,
  ReorderHomepageSectionsBody,
  UpdateEventBody,
  UpdateHomepageSectionBody,
  UpdateNewsBody,
  UpdatePageBody,
} from './publishing.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

/** Single API service handling News + Events + Pages + Homepage Sections. */
@Injectable({ providedIn: 'root' })
export class PublishingApiService {
  private readonly http = inject(HttpClient);

  // ---- News ----
  async listNews(opts: { page?: number; pageSize?: number; search?: string; isPublished?: boolean } = {}): Promise<Result<PagedResult<News>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    if (opts.isPublished !== undefined) params = params.set('isPublished', String(opts.isPublished));
    return this.run(() => firstValueFrom(this.http.get<PagedResult<News>>('/api/admin/news', { params })));
  }
  async createNews(body: CreateNewsBody): Promise<Result<News>> {
    return this.run(() => firstValueFrom(this.http.post<News>('/api/admin/news', body)));
  }
  async updateNews(id: string, body: UpdateNewsBody): Promise<Result<News>> {
    return this.run(() => firstValueFrom(this.http.put<News>(`/api/admin/news/${id}`, body)));
  }
  async deleteNews(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/news/${id}`)));
  }
  async publishNews(id: string): Promise<Result<News>> {
    return this.run(() => firstValueFrom(this.http.post<News>(`/api/admin/news/${id}/publish`, {})));
  }

  // ---- Events ----
  async listEvents(opts: { page?: number; pageSize?: number; search?: string } = {}): Promise<Result<PagedResult<Event>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    return this.run(() => firstValueFrom(this.http.get<PagedResult<Event>>('/api/admin/events', { params })));
  }
  async createEvent(body: CreateEventBody): Promise<Result<Event>> {
    return this.run(() => firstValueFrom(this.http.post<Event>('/api/admin/events', body)));
  }
  async updateEvent(id: string, body: UpdateEventBody): Promise<Result<Event>> {
    return this.run(() => firstValueFrom(this.http.put<Event>(`/api/admin/events/${id}`, body)));
  }
  async rescheduleEvent(id: string, body: RescheduleEventBody): Promise<Result<Event>> {
    return this.run(() =>
      firstValueFrom(this.http.post<Event>(`/api/admin/events/${id}/reschedule`, body)),
    );
  }
  async deleteEvent(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/events/${id}`)));
  }

  // ---- Pages ----
  async listPages(opts: { page?: number; pageSize?: number; search?: string } = {}): Promise<Result<PagedResult<Page>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    return this.run(() => firstValueFrom(this.http.get<PagedResult<Page>>('/api/admin/pages', { params })));
  }
  async createPage(body: CreatePageBody): Promise<Result<Page>> {
    return this.run(() => firstValueFrom(this.http.post<Page>('/api/admin/pages', body)));
  }
  async updatePage(id: string, body: UpdatePageBody): Promise<Result<Page>> {
    return this.run(() => firstValueFrom(this.http.put<Page>(`/api/admin/pages/${id}`, body)));
  }
  async deletePage(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/pages/${id}`)));
  }

  // ---- Homepage Sections ----
  async listHomepageSections(): Promise<Result<HomepageSection[]>> {
    return this.run(() => firstValueFrom(this.http.get<HomepageSection[]>('/api/admin/homepage-sections')));
  }
  async createHomepageSection(body: CreateHomepageSectionBody): Promise<Result<HomepageSection>> {
    return this.run(() =>
      firstValueFrom(this.http.post<HomepageSection>('/api/admin/homepage-sections', body)),
    );
  }
  async updateHomepageSection(id: string, body: UpdateHomepageSectionBody): Promise<Result<HomepageSection>> {
    return this.run(() =>
      firstValueFrom(this.http.put<HomepageSection>(`/api/admin/homepage-sections/${id}`, body)),
    );
  }
  async deleteHomepageSection(id: string): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(this.http.delete<void>(`/api/admin/homepage-sections/${id}`)),
    );
  }
  async reorderHomepageSections(body: ReorderHomepageSectionsBody): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(this.http.post<void>('/api/admin/homepage-sections/reorder', body)),
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

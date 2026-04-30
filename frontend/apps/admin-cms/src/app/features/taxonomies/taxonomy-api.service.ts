import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '../../core/ui/error-formatter';
import type {
  CreateResourceCategoryBody,
  CreateTopicBody,
  PagedResult,
  ResourceCategory,
  Topic,
  UpdateResourceCategoryBody,
  UpdateTopicBody,
} from './taxonomy.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class TaxonomyApiService {
  private readonly http = inject(HttpClient);

  // ---- Resource Categories ----
  async listCategories(opts: { page?: number; pageSize?: number; parentId?: string; isActive?: boolean } = {}): Promise<Result<PagedResult<ResourceCategory>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.parentId) params = params.set('parentId', opts.parentId);
    if (opts.isActive !== undefined) params = params.set('isActive', String(opts.isActive));
    return this.run(() => firstValueFrom(this.http.get<PagedResult<ResourceCategory>>('/api/admin/resource-categories', { params })));
  }
  async createCategory(body: CreateResourceCategoryBody): Promise<Result<ResourceCategory>> {
    return this.run(() => firstValueFrom(this.http.post<ResourceCategory>('/api/admin/resource-categories', body)));
  }
  async updateCategory(id: string, body: UpdateResourceCategoryBody): Promise<Result<ResourceCategory>> {
    return this.run(() => firstValueFrom(this.http.put<ResourceCategory>(`/api/admin/resource-categories/${id}`, body)));
  }
  async deleteCategory(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/resource-categories/${id}`)));
  }

  // ---- Topics ----
  async listTopics(opts: { page?: number; pageSize?: number; parentId?: string; isActive?: boolean; search?: string } = {}): Promise<Result<PagedResult<Topic>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.parentId) params = params.set('parentId', opts.parentId);
    if (opts.isActive !== undefined) params = params.set('isActive', String(opts.isActive));
    if (opts.search) params = params.set('search', opts.search);
    return this.run(() => firstValueFrom(this.http.get<PagedResult<Topic>>('/api/admin/topics', { params })));
  }
  async createTopic(body: CreateTopicBody): Promise<Result<Topic>> {
    return this.run(() => firstValueFrom(this.http.post<Topic>('/api/admin/topics', body)));
  }
  async updateTopic(id: string, body: UpdateTopicBody): Promise<Result<Topic>> {
    return this.run(() => firstValueFrom(this.http.put<Topic>(`/api/admin/topics/${id}`, body)));
  }
  async deleteTopic(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/topics/${id}`)));
  }

  // ---- Community moderation (soft-delete by-id) ----
  async softDeletePost(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/community/posts/${id}`)));
  }
  async softDeleteReply(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/community/replies/${id}`)));
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

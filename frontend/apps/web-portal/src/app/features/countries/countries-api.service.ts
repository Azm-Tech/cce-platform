import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { map } from 'rxjs/operators';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { PublicCountryDto } from '@frontend/api-client';
import {
  CONTENT_TYPE_API_VALUE,
  ContentRequestStatus,
  ContentType,
  contentTypeFromApiValue,
  type ContentRequestStatusValue,
  type Country, type CountryCode, type CountryContentRequest, type CountryProfile,
  type StateProfile, type SubmitRequestBody, type UpdateStateProfileBody,
} from './country.types';
import type { ResourceCategory } from '../knowledge-center/knowledge.types';

/**
 * Compile-time contract tripwire: every field this feature reads must
 * exist on the generated OpenAPI DTO. If the backend renames/removes a
 * field, regenerating types (pnpm nx run api-client:generate-types)
 * turns the drift into a build error here instead of a runtime blank page.
 */
// eslint-disable-next-line @typescript-eslint/no-unused-vars -- compile-time assertion only
type _CountryContractCheck = keyof Pick<
  PublicCountryDto,
  'id' | 'nameAr' | 'nameEn' | 'regionAr' | 'regionEn' | 'flagUrl' | 'isoAlpha2' | 'isoAlpha3'
>;

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class CountriesApiService {
  private readonly http = inject(HttpClient);

  private activeCountryCodesCache: Promise<Result<CountryCode[]>> | null = null;

  async listCountryCodes(opts: { search?: string; isActive?: boolean } = {}): Promise<Result<CountryCode[]>> {
    if (opts.isActive === true && !opts.search) {
      this.activeCountryCodesCache ??= this.fetchCountryCodes(opts);
      const result = await this.activeCountryCodesCache;
      if (!result.ok) this.activeCountryCodesCache = null;
      return result;
    }
    return this.fetchCountryCodes(opts);
  }

  private fetchCountryCodes(opts: { search?: string; isActive?: boolean }): Promise<Result<CountryCode[]>> {
    let params = new HttpParams();
    if (opts.search) params = params.set('search', opts.search);
    if (opts.isActive !== undefined) params = params.set('isActive', String(opts.isActive));
    return this.run(() =>
      firstValueFrom(
        this.http
          .get<{ data: CountryCode[] }>('/api/country-codes', { params })
          .pipe(map((res) => res.data)),
      ),
    );
  }

  async listCountries(opts: { search?: string } = {}): Promise<Result<Country[]>> {
    let params = new HttpParams();
    if (opts.search) params = params.set('search', opts.search);
    return this.run(async () => {
      // Live API wraps the list in an envelope: { data: { items: Country[] } }.
      const res = await firstValueFrom(
        this.http.get<Country[] | { data: Country[] | { items: Country[] } }>('/api/countries', { params }),
      );
      if (Array.isArray(res)) return res;
      const data = res.data;
      if (Array.isArray(data)) return data;
      return data?.items ?? [];
    });
  }

  async getById(id: string): Promise<Result<Country>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<Country | { data: Country }>(`/api/countries/${encodeURIComponent(id)}`),
      );
      if (res && typeof res === 'object' && 'data' in res && res.data) return res.data as Country;
      return res as Country;
    });
  }

  async getStateProfile(): Promise<Result<StateProfile>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<StateProfile | { data: StateProfile }>('/api/state/profile'),
      );
      if (res && typeof res === 'object' && 'data' in res && res.data) return res.data as StateProfile;
      return res as StateProfile;
    });
  }

  async listMyRequests(
    opts: { type?: ContentType; status?: number; page?: number; pageSize?: number } = {},
  ): Promise<Result<{ items: CountryContentRequest[]; total: number }>> {
    return this.run(async () => {
      let params = new HttpParams();
      if (opts.type !== undefined) params = params.set('type', CONTENT_TYPE_API_VALUE[opts.type]);
      if (opts.status !== undefined) params = params.set('status', opts.status);
      if (opts.page !== undefined) params = params.set('page', opts.page);
      if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
      const res = await firstValueFrom(
        this.http.get<
          | { data: { items: CountryContentRequest[]; total?: number } }
          | { items: CountryContentRequest[]; total?: number }
        >('/api/state/requests', { params }),
      );
      const envelope = 'data' in res
        ? (res as { data: { items: CountryContentRequest[]; total?: number } }).data
        : res as { items: CountryContentRequest[]; total?: number };
      const items = envelope?.items ?? [];
      const total = envelope?.total ?? items.length;
      return { items: items.map((r) => this.normalizeContentRequest(r)), total };
    });
  }

  async submitRequest(body: SubmitRequestBody): Promise<Result<CountryContentRequest>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.post<CountryContentRequest | { data: CountryContentRequest }>('/api/state/requests', body),
      );
      const raw = (res && typeof res === 'object' && 'data' in res && res.data)
        ? res.data as CountryContentRequest
        : res as CountryContentRequest;
      return this.normalizeContentRequest(raw);
    });
  }

  async updateStateProfile(countryId: string, body: UpdateStateProfileBody): Promise<Result<StateProfile>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.put<StateProfile | { data: StateProfile }>(
          `/api/state/profile/${encodeURIComponent(countryId)}`, body,
        ),
      );
      if (res && typeof res === 'object' && 'data' in res && res.data) return res.data as StateProfile;
      return res as StateProfile;
    });
  }

  async getProfile(countryId: string): Promise<Result<CountryProfile>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<CountryProfile | { data: CountryProfile }>(
          `/api/countries/${encodeURIComponent(countryId)}/profile`,
        ),
      );
      // Unwrap standard envelope { success, code, data: CountryProfile }
      if (res && typeof res === 'object' && 'data' in res && res.data) return res.data as CountryProfile;
      return res as CountryProfile;
    });
  }

  private normalizeContentRequest(r: CountryContentRequest): CountryContentRequest {
    const typeMap: Record<string, ContentType> = {
      Resource: ContentType.Resource,
      News:     ContentType.News,
      Event:    ContentType.Event,
    };
    const statusMap: Record<string, ContentRequestStatusValue> = {
      Pending:  ContentRequestStatus.Pending,
      Approved: ContentRequestStatus.Approved,
      Rejected: ContentRequestStatus.Rejected,
    };
    const rawType   = r.type   as unknown as string;
    const rawStatus = r.status as unknown as string | number;
    const normalizedStatus: ContentRequestStatusValue =
      typeof rawStatus === 'string'
        ? (statusMap[rawStatus] ?? ContentRequestStatus.Pending)
        : (rawStatus as ContentRequestStatusValue);
    return {
      ...r,
      type:   typeMap[rawType] ?? contentTypeFromApiValue(r.type as unknown as number),
      status: normalizedStatus,
    };
  }

  async listResourceCategories(): Promise<Result<ResourceCategory[]>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<ResourceCategory[] | { data: ResourceCategory[] }>('/api/state/resource-categories'),
      );
      return Array.isArray(res) ? res : (res as { data: ResourceCategory[] }).data ?? [];
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

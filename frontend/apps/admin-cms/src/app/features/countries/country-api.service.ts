import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '../../core/ui/error-formatter';
import type {
  Country,
  CountryProfile,
  PagedResult,
  UpdateCountryBody,
  UpsertCountryProfileBody,
} from './country.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class CountryApiService {
  private readonly http = inject(HttpClient);

  async listCountries(opts: { page?: number; pageSize?: number; search?: string; isActive?: boolean } = {}): Promise<Result<PagedResult<Country>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    if (opts.isActive !== undefined) params = params.set('isActive', String(opts.isActive));
    return this.run(() => firstValueFrom(this.http.get<PagedResult<Country>>('/api/admin/countries', { params })));
  }

  async getCountry(id: string): Promise<Result<Country>> {
    return this.run(() => firstValueFrom(this.http.get<Country>(`/api/admin/countries/${id}`)));
  }

  async updateCountry(id: string, body: UpdateCountryBody): Promise<Result<Country>> {
    return this.run(() => firstValueFrom(this.http.put<Country>(`/api/admin/countries/${id}`, body)));
  }

  async getProfile(countryId: string): Promise<Result<CountryProfile>> {
    return this.run(() =>
      firstValueFrom(this.http.get<CountryProfile>(`/api/admin/countries/${countryId}/profile`)),
    );
  }

  async upsertProfile(countryId: string, body: UpsertCountryProfileBody): Promise<Result<CountryProfile>> {
    return this.run(() =>
      firstValueFrom(this.http.put<CountryProfile>(`/api/admin/countries/${countryId}/profile`, body)),
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

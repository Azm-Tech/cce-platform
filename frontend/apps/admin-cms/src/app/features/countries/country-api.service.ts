import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
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

  async listCountries(opts: {
    page?: number;
    pageSize?: number;
    search?: string;
    sortBy?: 0 | 1 | 2;
    sortOrder?: 0 | 1;
    isCceCountry?: boolean;
  } = {}): Promise<Result<PagedResult<Country>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    if (opts.sortBy !== undefined) params = params.set('sortBy', opts.sortBy);
    if (opts.sortOrder !== undefined) params = params.set('sortOrder', opts.sortOrder);
    if (opts.isCceCountry !== undefined) params = params.set('isCceCountry', String(opts.isCceCountry));
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<{ data: PagedResult<Country> }>('/api/countries', { params }),
      );
      return res.data;
    });
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

import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { map } from 'rxjs/operators';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { Country, CountryCode, CountryProfile } from './country.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class CountriesApiService {
  private readonly http = inject(HttpClient);

  private activeCountryCodesCache: Promise<Result<CountryCode[]>> | null = null;

  async listCountryCodes(opts: { search?: string; isActive?: boolean } = {}): Promise<Result<CountryCode[]>> {
    if (opts.isActive === true && !opts.search) {
      this.activeCountryCodesCache ??= this.fetchCountryCodes(opts);
      return this.activeCountryCodesCache;
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
    return this.run(() =>
      firstValueFrom(this.http.get<Country[]>('/api/countries', { params })),
    );
  }

  async getProfile(countryId: string): Promise<Result<CountryProfile>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<CountryProfile>(`/api/countries/${encodeURIComponent(countryId)}/profile`),
      ),
    );
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

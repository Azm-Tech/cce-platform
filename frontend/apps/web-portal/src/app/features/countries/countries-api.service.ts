import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { map } from 'rxjs/operators';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { PublicCountryDto } from '@frontend/api-client';
import type { Country, CountryCode, CountryProfile } from './country.types';

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

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

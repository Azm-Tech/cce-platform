import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { Country, CountryProfile } from './country.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class CountriesApiService {
  private readonly http = inject(HttpClient);

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

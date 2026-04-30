import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { KapsarcSnapshot } from './country.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class KapsarcApiService {
  private readonly http = inject(HttpClient);

  /**
   * GETs the latest KAPSARC snapshot for a country.
   *
   * 404 is treated as a *valid empty state* (no snapshot has been published yet)
   * and resolves to `{ ok: true, value: null }` — not a feature error. This lets
   * the country detail page render the profile body even when the snapshot
   * isn't available.
   */
  async getLatestSnapshot(countryId: string): Promise<Result<KapsarcSnapshot | null>> {
    try {
      const value = await firstValueFrom(
        this.http.get<KapsarcSnapshot>(
          `/api/kapsarc/snapshots/${encodeURIComponent(countryId)}`,
        ),
      );
      return { ok: true, value };
    } catch (err) {
      const error = err as HttpErrorResponse;
      if (error.status === 404) {
        return { ok: true, value: null };
      }
      return { ok: false, error: toFeatureError(error) };
    }
  }
}

import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { CityTechnology } from './interactive-city.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class InteractiveCityApiService {
  private readonly http = inject(HttpClient);

  async listTechnologies(): Promise<Result<CityTechnology[]>> {
    try {
      const value = await firstValueFrom(
        this.http.get<CityTechnology[]>('/api/interactive-city/technologies'),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

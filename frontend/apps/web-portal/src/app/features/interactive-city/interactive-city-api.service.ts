import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  CityTechnology,
  RunRequest,
  RunResult,
  SaveRequest,
  SavedScenario,
} from './interactive-city.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class InteractiveCityApiService {
  private readonly http = inject(HttpClient);

  // ─── Anonymous-allowed ───
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

  async runScenario(req: RunRequest): Promise<Result<RunResult>> {
    try {
      const value = await firstValueFrom(
        this.http.post<RunResult>('/api/interactive-city/scenarios/run', req),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  // ─── Authenticated ───
  async listMyScenarios(): Promise<Result<SavedScenario[]>> {
    try {
      const value = await firstValueFrom(
        this.http.get<SavedScenario[]>('/api/me/interactive-city/scenarios'),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  async saveScenario(req: SaveRequest): Promise<Result<SavedScenario>> {
    try {
      const value = await firstValueFrom(
        this.http.post<SavedScenario>('/api/me/interactive-city/scenarios', req),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  async deleteMyScenario(id: string): Promise<Result<void>> {
    try {
      await firstValueFrom(
        this.http.delete<void>(
          `/api/me/interactive-city/scenarios/${encodeURIComponent(id)}`,
        ),
      );
      return { ok: true, value: undefined };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

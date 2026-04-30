import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  ExpertRequestStatus,
  ServiceRatingPayload,
  SubmitExpertRequestPayload,
  UpdateMyProfilePayload,
  UserProfile,
} from './account.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class AccountApiService {
  private readonly http = inject(HttpClient);

  async getProfile(): Promise<Result<UserProfile>> {
    return this.run(() => firstValueFrom(this.http.get<UserProfile>('/api/me')));
  }

  async updateProfile(payload: UpdateMyProfilePayload): Promise<Result<UserProfile>> {
    return this.run(() => firstValueFrom(this.http.put<UserProfile>('/api/me', payload)));
  }

  /**
   * GETs the user's expert-request status. 404 means the user has not
   * yet submitted a request — same "valid empty state" pattern as the
   * KAPSARC service in Phase 4.1; resolves to { ok: true, value: null }.
   */
  async getExpertStatus(): Promise<Result<ExpertRequestStatus | null>> {
    try {
      const value = await firstValueFrom(
        this.http.get<ExpertRequestStatus>('/api/me/expert-status'),
      );
      return { ok: true, value };
    } catch (err) {
      const error = err as HttpErrorResponse;
      if (error.status === 404) return { ok: true, value: null };
      return { ok: false, error: toFeatureError(error) };
    }
  }

  async submitExpertRequest(
    payload: SubmitExpertRequestPayload,
  ): Promise<Result<ExpertRequestStatus>> {
    return this.run(() =>
      firstValueFrom(
        this.http.post<ExpertRequestStatus>('/api/users/expert-request', payload),
      ),
    );
  }

  async submitServiceRating(payload: ServiceRatingPayload): Promise<Result<{ id: string }>> {
    return this.run(() =>
      firstValueFrom(
        this.http.post<{ id: string }>('/api/surveys/service-rating', payload),
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

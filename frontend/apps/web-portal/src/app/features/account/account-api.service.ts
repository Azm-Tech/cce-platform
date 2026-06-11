import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { map } from 'rxjs/operators';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  EvaluationPayload,
  ExpertRequestStatus,
  InterestQuestion,
  MyInterests,
  ServiceRatingPayload,
  SubmitExpertRequestPayload,
  UpdateMyInterestsPayload,
  UpdateMyProfilePayload,
  UserProfile,
} from './account.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class AccountApiService {
  private readonly http = inject(HttpClient);

  async getProfile(): Promise<Result<UserProfile>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<{ data: UserProfile }>('/api/me').pipe(map((res) => res.data)),
      ),
    );
  }

  async updateProfile(payload: UpdateMyProfilePayload): Promise<Result<UserProfile>> {
    return this.run(() =>
      firstValueFrom(
        this.http.put<{ data: UserProfile }>('/api/me', payload).pipe(map((res) => res.data)),
      ),
    );
  }

  /**
   * GETs the user's expert-request status. 404 means the user has not
   * yet submitted a request — same "valid empty state" pattern as the
   * KAPSARC service in Phase 4.1; resolves to { ok: true, value: null }.
   */
  async getExpertStatus(): Promise<Result<ExpertRequestStatus | null>> {
    try {
      const value = await firstValueFrom(
        this.http.get<{ data: ExpertRequestStatus }>('/api/me/expert-status').pipe(map((res) => res.data)),
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
        this.http.post<{ data: ExpertRequestStatus }>('/api/users/expert-request', payload).pipe(map((res) => res.data)),
      ),
    );
  }

  async getInterestQuestions(): Promise<Result<InterestQuestion[]>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<{ data: InterestQuestion[] }>('/api/interest-topics/questions').pipe(map(res => res.data)),
      ),
    );
  }

  async getMyInterests(): Promise<Result<MyInterests>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<{ data: MyInterests }>('/api/me/interests').pipe(map(res => res.data)),
      ),
    );
  }

  async updateMyInterests(payload: UpdateMyInterestsPayload): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(this.http.patch<void>('/api/me/interests', payload)),
    );
  }

  async submitEvaluation(payload: EvaluationPayload): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(this.http.post<void>('/api/evaluations', payload)),
    );
  }

  async submitServiceRating(payload: ServiceRatingPayload): Promise<Result<{ id: string }>> {
    return this.run(() =>
      firstValueFrom(
        this.http.post<{ id: string }>('/api/surveys/service-rating', payload),
      ),
    );
  }

  async requestEmailChange(newEmail: string): Promise<Result<{ verificationId: string }>> {
    return this.run(() =>
      firstValueFrom(
        this.http
          .post<{ data: { verificationId: string } }>('/api/me/email/request-change', { newEmail })
          .pipe(map((res) => res.data)),
      ),
    );
  }

  async requestPhoneChange(newPhone: string): Promise<Result<{ verificationId: string }>> {
    return this.run(() =>
      firstValueFrom(
        this.http
          .post<{ data: { verificationId: string } }>('/api/me/phone/request-change', { newPhone })
          .pipe(map((res) => res.data)),
      ),
    );
  }

  async confirmEmailChange(verificationId: string, code: string): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(this.http.post<void>('/api/me/email/confirm-change', { verificationId, code })),
    );
  }

  async confirmPhoneChange(verificationId: string, code: string): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(this.http.post<void>('/api/me/phone/confirm-change', { verificationId, code })),
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

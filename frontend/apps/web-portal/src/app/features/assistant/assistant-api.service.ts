import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

export interface AssistantQueryPayload {
  question: string;
  locale: 'ar' | 'en';
}

export interface AssistantReply {
  reply: string;
}

@Injectable({ providedIn: 'root' })
export class AssistantApiService {
  private readonly http = inject(HttpClient);

  async query(payload: AssistantQueryPayload): Promise<Result<AssistantReply>> {
    try {
      const value = await firstValueFrom(
        this.http.post<AssistantReply>('/api/assistant/query', payload),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

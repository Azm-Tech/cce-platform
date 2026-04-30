import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { PublicPage } from './page.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class PagesApiService {
  private readonly http = inject(HttpClient);

  async getBySlug(slug: string): Promise<Result<PublicPage>> {
    try {
      const value = await firstValueFrom(
        this.http.get<PublicPage>(`/api/pages/${encodeURIComponent(slug)}`),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

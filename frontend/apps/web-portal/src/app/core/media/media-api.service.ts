import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { map } from 'rxjs/operators';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';

export interface MediaAsset {
  id: string;
  url: string;
  storageKey: string;
}

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class MediaApiService {
  private readonly http = inject(HttpClient);

  async uploadFile(file: File): Promise<Result<MediaAsset>> {
    const body = new FormData();
    body.append('file', file);
    try {
      const asset = await firstValueFrom(
        this.http
          .post<{ data: MediaAsset }>('/api/media', body)
          .pipe(map((res) => res.data)),
      );
      return { ok: true, value: asset };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

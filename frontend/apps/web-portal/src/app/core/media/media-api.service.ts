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
    return this.upload('/api/media', file);
  }

  async uploadAsset(file: File): Promise<Result<MediaAsset>> {
    return this.upload('/api/assets', file);
  }

  async getAsset(id: string): Promise<Result<MediaAsset>> {
    try {
      const res = await firstValueFrom(
        this.http.get<MediaAsset | { data: MediaAsset }>(`/api/assets/${encodeURIComponent(id)}`),
      );
      const asset = (res && typeof res === 'object' && 'data' in res && res.data)
        ? res.data as MediaAsset
        : res as MediaAsset;
      return { ok: true, value: asset };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  private async upload(endpoint: string, file: File): Promise<Result<MediaAsset>> {
    const body = new FormData();
    body.append('file', file);
    try {
      const asset = await firstValueFrom(
        this.http
          .post<{ data: MediaAsset }>(endpoint, body)
          .pipe(map((res) => res.data)),
      );
      return { ok: true, value: asset };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

/**
 * Streaming-CSV download client. Calls /api/admin/reports/{slug}.csv with
 * optional from/to date params and resolves the blob; the caller saves it
 * to a download link.
 */
@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);

  async download(slug: string, opts: { from?: string; to?: string } = {}): Promise<Result<Blob>> {
    let params = new HttpParams();
    if (opts.from) params = params.set('from', opts.from);
    if (opts.to) params = params.set('to', opts.to);
    try {
      const blob = await firstValueFrom(
        this.http.get(`/api/admin/reports/${slug}.csv`, {
          params,
          responseType: 'blob',
        }),
      );
      return { ok: true, value: blob };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

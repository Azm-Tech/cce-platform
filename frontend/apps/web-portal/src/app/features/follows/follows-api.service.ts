import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import { FOLLOW_PATH_SEGMENT, type FollowEntityType, type MyFollows } from './follows.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class FollowsApiService {
  private readonly http = inject(HttpClient);

  async getMyFollows(): Promise<Result<MyFollows>> {
    return this.run(() => firstValueFrom(this.http.get<MyFollows>('/api/me/follows')));
  }

  async follow(type: FollowEntityType, id: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.post(this.urlFor(type, id), {}),
      );
    });
  }

  async unfollow(type: FollowEntityType, id: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(this.http.delete(this.urlFor(type, id)));
    });
  }

  private urlFor(type: FollowEntityType, id: string): string {
    return `/api/me/follows/${FOLLOW_PATH_SEGMENT[type]}/${encodeURIComponent(id)}`;
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

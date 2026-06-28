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
    return this.run(async () => {
      const raw = await firstValueFrom(
        this.http.get<{ data?: MyFollows } | MyFollows>('/api/me/follows'),
      );
      // Unwrap envelope if API returns { data: { ... } }
      const follows = (raw && typeof raw === 'object' && 'data' in raw && raw.data)
        ? raw.data as MyFollows
        : raw as MyFollows;
      return {
        topicIds: follows?.topicIds ?? [],
        userIds: follows?.userIds ?? [],
        postIds: follows?.postIds ?? [],
      };
    });
  }

  async follow(type: FollowEntityType, id: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.put(this.urlFor(type, id), { status: 1 }),
      );
    });
  }

  async unfollow(type: FollowEntityType, id: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.put(this.urlFor(type, id), { status: 0 }),
      );
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

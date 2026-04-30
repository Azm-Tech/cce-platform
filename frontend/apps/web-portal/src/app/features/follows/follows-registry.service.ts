import { Injectable, computed, inject, signal } from '@angular/core';
import { FollowsApiService } from './follows-api.service';
import type { FollowEntityType, MyFollows } from './follows.types';

/**
 * Singleton signal-cached follows registry. Both FollowsPage and the
 * [cceFollow] directive read from / write to this service so the UI
 * stays in sync without a network round-trip per consumer.
 *
 * Lazy: the first caller of `ensureLoaded()` triggers a single
 * GET /api/me/follows; subsequent calls reuse the cached state.
 */
@Injectable({ providedIn: 'root' })
export class FollowsRegistryService {
  private readonly api = inject(FollowsApiService);

  private readonly _state = signal<MyFollows | null>(null);
  readonly state = this._state.asReadonly();

  private loading = false;
  private loadPromise: Promise<void> | null = null;

  /** Idempotent: fires `getMyFollows()` once across N calls per session. */
  async ensureLoaded(): Promise<void> {
    if (this._state() !== null) return;
    if (this.loadPromise) return this.loadPromise;
    this.loading = true;
    this.loadPromise = (async () => {
      const res = await this.api.getMyFollows();
      this.loading = false;
      if (res.ok) this._state.set(res.value);
    })();
    return this.loadPromise;
  }

  /** Resets the cache (test-only; call after sign-out in production code). */
  reset(): void {
    this._state.set(null);
    this.loadPromise = null;
    this.loading = false;
  }

  /** Reactive lookup for use inside `computed()`. Returns false until loaded. */
  isFollowing(type: FollowEntityType, id: string): boolean {
    const s = this._state();
    if (!s) return false;
    return this.idsFor(s, type).includes(id);
  }

  /** Pure-signal helper for templates: returns a computed for the membership flag. */
  isFollowing$(type: FollowEntityType, id: string) {
    return computed(() => this.isFollowing(type, id));
  }

  /** Optimistically updates the cached state. */
  setFollowing(type: FollowEntityType, id: string, value: boolean): void {
    const current = this._state() ?? { topicIds: [], userIds: [], postIds: [] };
    const ids = this.idsFor(current, type);
    const has = ids.includes(id);
    if (value && has) return;
    if (!value && !has) return;
    const next = value
      ? this.withIds(current, type, [...ids, id])
      : this.withIds(current, type, ids.filter((x) => x !== id));
    this._state.set(next);
  }

  private idsFor(s: MyFollows, type: FollowEntityType): string[] {
    switch (type) {
      case 'topic': return s.topicIds;
      case 'user': return s.userIds;
      case 'post': return s.postIds;
    }
  }

  private withIds(s: MyFollows, type: FollowEntityType, ids: string[]): MyFollows {
    switch (type) {
      case 'topic': return { ...s, topicIds: ids };
      case 'user': return { ...s, userIds: ids };
      case 'post': return { ...s, postIds: ids };
    }
  }
}

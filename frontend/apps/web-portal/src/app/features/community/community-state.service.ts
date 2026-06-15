import { Injectable, computed, inject, signal } from '@angular/core';
import { CommunityApiService } from './community-api.service';
import type { CommunityDto } from './community.types';

@Injectable({ providedIn: 'root' })
export class CommunityStateService {
  private readonly api = inject(CommunityApiService);

  readonly community = signal<CommunityDto | null>(null);
  readonly communityId = computed(() => this.community()?.id ?? null);

  private loadPromise: Promise<void> | null = null;

  ensureLoaded(): Promise<void> {
    if (this.loadPromise) return this.loadPromise;
    this.loadPromise = this.api.listCommunities({ pageSize: 1 }).then((res) => {
      if (res.ok && res.value.items.length > 0) {
        this.community.set(res.value.items[0]);
      }
    });
    return this.loadPromise;
  }
}

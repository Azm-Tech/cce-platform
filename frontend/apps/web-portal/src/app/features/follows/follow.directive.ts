import { Directive, HostListener, OnInit, computed, inject, input } from '@angular/core';
import { ToastService } from '@frontend/ui-kit';
import { FollowsApiService } from './follows-api.service';
import { FollowsStoreService } from './follows-store.service';
import type { FollowEntityType } from './follows.types';

const FOLLOW_SUCCESS_KEY: Partial<Record<FollowEntityType, string>> = {
  topic: 'confirmations.CON010',
  post: 'confirmations.CON012',
};

const FOLLOW_ERROR_KEY: Record<FollowEntityType, string> = {
  topic: 'errors.ERR012',
  post: 'errors.ERR015',
  user: 'errors.server',
};

@Directive({
  selector: '[cceFollow]',
  standalone: true,
  exportAs: 'cceFollow',
})
export class FollowDirective implements OnInit {
  private readonly api = inject(FollowsApiService);
  private readonly registry = inject(FollowsStoreService);
  private readonly toast = inject(ToastService);

  readonly entityType = input.required<FollowEntityType>();
  readonly entityId = input<string>();

  readonly isFollowing = computed(() => {
    const id = this.entityId();
    if (!id) return false;
    return this.registry.isFollowing(this.entityType(), id);
  });

  ngOnInit(): void {
    void this.registry.ensureLoaded();
  }

  @HostListener('click')
  async onClick(): Promise<void> {
    const t = this.entityType();
    const id = this.entityId();
    if (!id) return;
    const wasFollowing = this.registry.isFollowing(t, id);

    this.registry.setFollowing(t, id, !wasFollowing);

    const res = wasFollowing
      ? await this.api.unfollow(t, id)
      : await this.api.follow(t, id);

    if (res.ok) {
      if (!wasFollowing) {
        const key = FOLLOW_SUCCESS_KEY[t];
        if (key) this.toast.success(key);
      }
    } else {
      this.registry.setFollowing(t, id, wasFollowing);
      this.toast.error(FOLLOW_ERROR_KEY[t]);
    }
  }
}

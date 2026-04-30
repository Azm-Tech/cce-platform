import { Directive, HostListener, OnInit, computed, inject, input } from '@angular/core';
import { FollowsApiService } from './follows-api.service';
import { FollowsRegistryService } from './follows-registry.service';
import type { FollowEntityType } from './follows.types';

/**
 * Single-button follow toggle. Drop on any element to bind a click
 * handler that flips the follow state for the given entity, with
 * optimistic updates against the shared FollowsRegistryService.
 *
 * Usage:
 *   <button mat-button cceFollow type="topic" entityId="t1">…</button>
 *
 * Templates that need the current state in their content can read
 * `directive.isFollowing()` via a template reference variable, or
 * pull the registry signal directly via inject().
 */
@Directive({
  selector: '[cceFollow]',
  standalone: true,
  exportAs: 'cceFollow',
})
export class FollowDirective implements OnInit {
  private readonly api = inject(FollowsApiService);
  private readonly registry = inject(FollowsRegistryService);

  readonly type = input.required<FollowEntityType>();
  readonly entityId = input.required<string>();

  readonly isFollowing = computed(() =>
    this.registry.isFollowing(this.type(), this.entityId()),
  );

  ngOnInit(): void {
    void this.registry.ensureLoaded();
  }

  @HostListener('click')
  async onClick(): Promise<void> {
    const type = this.type();
    const id = this.entityId();
    const wasFollowing = this.registry.isFollowing(type, id);

    // Optimistic flip.
    this.registry.setFollowing(type, id, !wasFollowing);

    const res = wasFollowing
      ? await this.api.unfollow(type, id)
      : await this.api.follow(type, id);

    if (!res.ok) {
      // Revert on failure.
      this.registry.setFollowing(type, id, wasFollowing);
    }
  }
}

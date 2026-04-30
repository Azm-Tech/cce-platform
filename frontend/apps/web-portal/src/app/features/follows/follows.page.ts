import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { FollowsApiService } from './follows-api.service';
import type { FollowEntityType, MyFollows } from './follows.types';

interface Section {
  type: FollowEntityType;
  ids: string[];
  titleKey: string;
  emptyKey: string;
  routePrefix: string;
}

@Component({
  selector: 'cce-follows-page',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatButtonModule, MatChipsModule, MatIconModule, MatProgressBarModule,
    TranslateModule,
  ],
  templateUrl: './follows.page.html',
  styleUrl: './follows.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FollowsPage implements OnInit {
  private readonly api = inject(FollowsApiService);
  private readonly toast = inject(ToastService);

  readonly state = signal<MyFollows | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly sections = computed<Section[]>(() => {
    const s = this.state();
    if (!s) return [];
    return [
      {
        type: 'topic',
        ids: s.topicIds,
        titleKey: 'follows.section.topics',
        emptyKey: 'follows.empty.topics',
        routePrefix: '/community/topics',
      },
      {
        type: 'user',
        ids: s.userIds,
        titleKey: 'follows.section.users',
        emptyKey: 'follows.empty.users',
        routePrefix: '/community/users',
      },
      {
        type: 'post',
        ids: s.postIds,
        titleKey: 'follows.section.posts',
        emptyKey: 'follows.empty.posts',
        routePrefix: '/community/posts',
      },
    ];
  });

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getMyFollows();
    this.loading.set(false);
    if (res.ok) this.state.set(res.value);
    else this.errorKind.set(res.error.kind);
  }

  async unfollow(type: FollowEntityType, id: string): Promise<void> {
    // Optimistic remove from local state.
    const before = this.state();
    if (!before) return;
    this.state.set(this.removeId(before, type, id));

    const res = await this.api.unfollow(type, id);
    if (res.ok) {
      this.toast.success('follows.unfollowToast');
    } else {
      // Restore on error.
      this.state.set(this.insertId(this.state() ?? before, type, id));
      this.toast.error('follows.errorToast');
    }
  }

  retry(): void {
    void this.load();
  }

  private removeId(s: MyFollows, type: FollowEntityType, id: string): MyFollows {
    switch (type) {
      case 'topic': return { ...s, topicIds: s.topicIds.filter((x) => x !== id) };
      case 'user': return { ...s, userIds: s.userIds.filter((x) => x !== id) };
      case 'post': return { ...s, postIds: s.postIds.filter((x) => x !== id) };
    }
  }

  private insertId(s: MyFollows, type: FollowEntityType, id: string): MyFollows {
    if (this.hasId(s, type, id)) return s;
    switch (type) {
      case 'topic': return { ...s, topicIds: [...s.topicIds, id] };
      case 'user': return { ...s, userIds: [...s.userIds, id] };
      case 'post': return { ...s, postIds: [...s.postIds, id] };
    }
  }

  private hasId(s: MyFollows, type: FollowEntityType, id: string): boolean {
    switch (type) {
      case 'topic': return s.topicIds.includes(id);
      case 'user': return s.userIds.includes(id);
      case 'post': return s.postIds.includes(id);
    }
  }
}

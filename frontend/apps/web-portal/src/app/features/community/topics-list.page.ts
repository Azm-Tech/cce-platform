import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { CommunityApiService } from './community-api.service';
import { TopicCardComponent } from './topic-card.component';
import type { PublicTopic } from './community.types';

@Component({
  selector: 'cce-topics-list-page',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule, MatProgressBarModule,
    TranslateModule, TopicCardComponent,
  ],
  templateUrl: './topics-list.page.html',
  styleUrl: './topics-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicsListPage implements OnInit {
  private readonly api = inject(CommunityApiService);
  private readonly localeService = inject(LocaleService);

  readonly rows = signal<PublicTopic[]>([]);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly locale = this.localeService.locale;

  readonly empty = computed(
    () => !this.loading() && this.rows().length === 0 && !this.errorKind(),
  );

  /** Sorted ascending by orderIndex (stable order across renders). */
  readonly sortedRows = computed(() =>
    [...this.rows()].sort((a, b) => a.orderIndex - b.orderIndex),
  );

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listTopics();
    this.loading.set(false);
    if (res.ok) this.rows.set(res.value);
    else this.errorKind.set(res.error.kind);
  }

  retry(): void {
    void this.load();
  }
}

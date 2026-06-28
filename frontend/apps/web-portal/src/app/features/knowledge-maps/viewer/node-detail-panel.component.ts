
import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  computed,
  input,
  output,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { iconDataUri } from '../lib/node-icons';
import type {
  InteractiveMapNode,
  NodeDetailEvent,
  NodeDetailNews,
  NodeDetailPost,
  NodeDetailResource,
  NodeDetails,
} from '../knowledge-maps.types';

/** A merged news/event row shape for the "Related News & Events" section. */
export interface NewsEventRow {
  kind: 'news' | 'event';
  id: string;
  titleAr: string;
  titleEn: string;
  date: string;
  featuredImageUrl?: string | null;
}

export type DrawerLinkKind = 'resource' | 'news' | 'event' | 'post';

/**
 * Node-detail drawer. Slides over the graph canvas when a map node is
 * selected and surfaces the node's topic content — description, related
 * sources, news & events, and community posts — from
 * GET /api/interactive-maps/nodes/{id}/details.
 *
 * Renders nothing when `node()` is null. The page positions the drawer
 * as an absolute overlay on the trailing edge.
 */
@Component({
  selector: 'cce-node-detail-panel',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, TranslocoModule],
  templateUrl: './node-detail-panel.component.html',
  styleUrl: './node-detail-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NodeDetailPanelComponent {
  readonly node = input<InteractiveMapNode | null>(null);
  /** Full node list of the active tab — used to resolve the parent eyebrow. */
  readonly allNodes = input<InteractiveMapNode[]>([]);
  readonly details = input<NodeDetails | null>(null);
  readonly loading = input<boolean>(false);
  readonly locale = input<'ar' | 'en'>('en');

  readonly closed = output<void>();
  readonly linkActivated = output<{ kind: DrawerLinkKind; id: string }>();

  readonly name = computed(() => {
    const n = this.node();
    if (!n) return '';
    return this.locale() === 'ar' ? n.nameAr : n.nameEn;
  });

  readonly parent = computed<InteractiveMapNode | null>(() => {
    const n = this.node();
    if (!n?.parentId) return null;
    return this.allNodes().find((p) => p.id === n.parentId) ?? null;
  });

  readonly parentName = computed(() => {
    const p = this.parent();
    if (!p) return '';
    return this.locale() === 'ar' ? p.nameAr : p.nameEn;
  });

  readonly description = computed(() => {
    const d = this.details();
    if (!d) return '';
    return this.locale() === 'ar' ? d.topic.descriptionAr : d.topic.descriptionEn;
  });

  readonly topicName = computed(() => {
    const d = this.details();
    if (!d) return '';
    return this.locale() === 'ar' ? d.topic.nameAr : d.topic.nameEn;
  });

  readonly resources = computed<NodeDetailResource[]>(() => this.details()?.resources ?? []);
  readonly posts = computed<NodeDetailPost[]>(() => this.details()?.posts ?? []);

  /** News then events, merged into one display list with a normalized date. */
  readonly newsEvents = computed<NewsEventRow[]>(() => {
    const d = this.details();
    if (!d) return [];
    const news: NewsEventRow[] = d.news.map((n: NodeDetailNews) => ({
      kind: 'news',
      id: n.id,
      titleAr: n.titleAr,
      titleEn: n.titleEn,
      date: n.publishedOn,
      featuredImageUrl: n.featuredImageUrl,
    }));
    const events: NewsEventRow[] = d.events.map((e: NodeDetailEvent) => ({
      kind: 'event',
      id: e.id,
      titleAr: e.titleAr,
      titleEn: e.titleEn,
      date: e.startsOn,
      featuredImageUrl: e.featuredImageUrl,
    }));
    return [...news, ...events];
  });

  readonly hasAnyContent = computed(
    () =>
      this.resources().length > 0 || this.newsEvents().length > 0 || this.posts().length > 0,
  );

  iconSrc(): string {
    return iconDataUri(this.node()?.iconKey);
  }

  /** Localized title for a resource / news / event row. */
  titleOf(item: { titleAr: string; titleEn: string }): string {
    return this.locale() === 'ar' ? item.titleAr : item.titleEn;
  }

  formatDate(iso: string): string {
    if (!iso) return '';
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return '';
    return new Intl.DateTimeFormat(this.locale() === 'ar' ? 'ar' : 'en', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    }).format(d);
  }

  resourceTypeKey(type: string): string {
    return `knowledgeMaps.resourceType.${type || 'other'}`;
  }

  postTypeKey(type: string): string {
    return `knowledgeMaps.postType.${type || 'info'}`;
  }

  postInitial(post: NodeDetailPost): string {
    return (post.title?.trim()?.charAt(0) ?? '•').toUpperCase();
  }

  onClose(): void {
    this.closed.emit();
  }

  onLink(kind: DrawerLinkKind, id: string): void {
    this.linkActivated.emit({ kind, id });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.node()) this.closed.emit();
  }
}


import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { GraphCanvasComponent } from './viewer/graph-canvas.component';
import { MapViewerStore } from './viewer/map-viewer-store.service';
import { NodeDetailPanelComponent } from './viewer/node-detail-panel.component';

/**
 * Top-level page for the knowledge-map viewer at /knowledge-maps.
 *
 * The system holds a single interactive map, so this page loads that one
 * map directly (no id, no tabs, no list/search — matching the Figma, which
 * is just the graph). Provides MapViewerStore at the component level so each
 * route activation gets a fresh state container. Renders the graph canvas
 * full-bleed with the node-detail drawer overlaying the trailing edge.
 */
@Component({
  selector: 'cce-map-viewer-page',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    TranslocoModule,
    GraphCanvasComponent,
    NodeDetailPanelComponent,
  ],
  providers: [MapViewerStore],
  templateUrl: './map-viewer.page.html',
  styleUrl: './map-viewer.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapViewerPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);
  readonly store = inject(MapViewerStore);

  readonly locale = this.localeService.locale;
  readonly mirrored = computed(() => this.locale() === 'ar');

  /** All nodes of the map — passed to the detail panel for parent/child resolution. */
  readonly allNodes = computed(() => this.store.nodes());

  async ngOnInit(): Promise<void> {
    await this.store.loadMap();

    // Optional deep-link: ?node=<id> opens that node's drawer on load.
    const node = this.route.snapshot.queryParams['node'];
    if (node) this.store.selectNode(node);
  }

  onNodeClick(id: string): void {
    this.store.selectNode(id);
  }

  onPanelClosed(): void {
    this.store.selectNode(null);
  }

  /** Drawer link navigation — route to the related content's detail page. */
  onPanelLink(e: { kind: 'resource' | 'news' | 'event' | 'post'; id: string }): void {
    const path: Record<typeof e.kind, string[]> = {
      resource: ['/knowledge-center', e.id],
      news: ['/news', e.id],
      event: ['/events', e.id],
      post: ['/community/posts', e.id],
    };
    void this.router.navigate(path[e.kind]);
  }

  retry(): void {
    void this.store.retry();
  }
}

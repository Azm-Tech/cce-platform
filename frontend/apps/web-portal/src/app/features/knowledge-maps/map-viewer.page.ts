import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { MapViewerStore } from './viewer/map-viewer-store.service';
import { parseUrlState } from './viewer/url-state';

/**
 * Top-level page for the knowledge-map viewer at /knowledge-maps/:id.
 *
 * Provides MapViewerStore at the component level so each route
 * activation gets a fresh state container. Hydrates URL query params
 * into the store before opening the active tab. Renders progress /
 * not-found / error / active-tab-header + a placeholder where Phase 02
 * GraphCanvas will mount.
 */
@Component({
  selector: 'cce-map-viewer-page',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatButtonModule, MatIconModule, MatProgressBarModule,
    TranslateModule,
  ],
  providers: [MapViewerStore],
  templateUrl: './map-viewer.page.html',
  styleUrl: './map-viewer.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapViewerPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  readonly store = inject(MapViewerStore);

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    // Hydrate non-route URL state into the store before opening the tab.
    const url = parseUrlState(this.route.snapshot.queryParams);
    this.store.setSearch(url.q);
    this.store.setFilters(url.filters);
    this.store.setViewMode(url.view);
    if (url.node) this.store.selectNode(url.node);

    await this.store.openTab(id);
  }

  retry(): void {
    void this.store.retry();
  }
}

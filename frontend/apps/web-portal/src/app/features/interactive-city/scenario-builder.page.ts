import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { ScenarioBuilderStore } from './builder/scenario-builder-store.service';
import { ScenarioHeaderComponent } from './builder/scenario-header.component';
import { SelectedListComponent } from './builder/selected-list.component';
import { TechnologyCatalogComponent } from './builder/technology-catalog.component';
import { buildUrlPatch, parseUrlState } from './lib/url-state';

/**
 * Top-level page for /interactive-city. Owns URL hydration and the
 * 200ms-debounced sync-back effect, then delegates rendering to the
 * sub-components in Phase 02–04. Provides ScenarioBuilderStore at the
 * component level so each route activation gets a fresh state.
 */
@Component({
  selector: 'cce-scenario-builder-page',
  standalone: true,
  imports: [
    DecimalPipe,
    TranslateModule,
    MatButtonModule,
    MatProgressBarModule,
    ScenarioHeaderComponent,
    TechnologyCatalogComponent,
    SelectedListComponent,
  ],
  providers: [ScenarioBuilderStore],
  templateUrl: './scenario-builder.page.html',
  styleUrl: './scenario-builder.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScenarioBuilderPage implements OnInit {
  readonly store = inject(ScenarioBuilderStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  /** Set true after the URL is hydrated so the sync effect doesn't fire
   *  on initial subscription before the parsed values are applied. */
  private readonly hydrated = signal<boolean>(false);
  private syncPending: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    // effect() must be created in the injection context (constructor or
    // field initializer). It's a no-op until `hydrated()` flips true in
    // ngOnInit.
    effect(() => {
      if (!this.hydrated()) return;
      const patch = buildUrlPatch(this.store.toUrlState());
      if (this.syncPending) clearTimeout(this.syncPending);
      this.syncPending = setTimeout(() => {
        this.router.navigate([], {
          queryParams: patch,
          queryParamsHandling: 'merge',
          replaceUrl: true,
        });
      }, 200);
    });
  }

  ngOnInit(): void {
    // Hydrate from URL before init() so the load doesn't race the first paint.
    const urlState = parseUrlState(this.route.snapshot.queryParamMap);
    this.store.markHydrating(true);
    this.store.applyUrlState(urlState);
    this.store.markHydrating(false);

    // Now allow the sync effect to write back on subsequent edits.
    this.hydrated.set(true);

    // Kick off the data loads.
    void this.store.init();
  }

  retryCatalog(): void {
    void this.store.init();
  }
}

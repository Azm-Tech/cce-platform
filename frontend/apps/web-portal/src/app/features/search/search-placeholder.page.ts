import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateModule } from '@ngx-translate/core';
import { map } from 'rxjs';

/**
 * Phase 5 will replace this with the real search results page consuming
 * `/api/search`. v0.1.0-Phase-1 ships this placeholder so the header
 * search box has a working route to navigate to (otherwise pressing Enter
 * would 404 the SPA).
 */
@Component({
  selector: 'cce-search-placeholder',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <section class="cce-search-placeholder">
      <h1>{{ 'search.title' | translate }}</h1>
      <p>{{ 'search.coming' | translate }}</p>
      @if (query()) {
        <p class="cce-search-placeholder__query"><code>q = {{ query() }}</code></p>
      }
    </section>
  `,
  styles: [
    `:host { display: block; padding: 2rem 1.5rem; max-width: 800px; margin: 0 auto; text-align: center; }
     .cce-search-placeholder h1 { margin-bottom: 0.75rem; }
     .cce-search-placeholder__query { color: rgba(0, 0, 0, 0.6); }`,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchPlaceholderPage {
  private readonly route = inject(ActivatedRoute);
  readonly query = toSignal(
    this.route.queryParamMap.pipe(map((p) => p.get('q') ?? '')),
    { initialValue: '' },
  );
}

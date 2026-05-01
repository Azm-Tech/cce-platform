import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

/**
 * Phase 1.2 stub. Replaced wholesale by Task 1.4 with the real
 * MapViewerPage shell (URL hydration + error states + active-tab
 * header). Kept here only so the lazy route loadComponent resolves
 * against a real module while later phases land.
 */
@Component({
  selector: 'cce-map-viewer-page',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <section class="cce-map-viewer">
      {{ 'knowledgeMaps.title' | translate }} (viewer scaffold)
    </section>
  `,
  styles: [`:host { display: block; padding: 1.5rem; }`],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapViewerPage {}

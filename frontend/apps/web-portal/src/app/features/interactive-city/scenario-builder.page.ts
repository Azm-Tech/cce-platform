import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ScenarioBuilderStore } from './builder/scenario-builder-store.service';

/**
 * Top-level page for /interactive-city. Phase 01 fills in the layout and
 * URL hydration; Phase 02–04 wire the sub-components into the slots.
 */
@Component({
  selector: 'cce-scenario-builder-page',
  standalone: true,
  imports: [TranslateModule],
  providers: [ScenarioBuilderStore],
  templateUrl: './scenario-builder.page.html',
  styleUrl: './scenario-builder.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScenarioBuilderPage {}

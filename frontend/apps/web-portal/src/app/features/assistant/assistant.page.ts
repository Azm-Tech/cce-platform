import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { AssistantStore } from './thread/assistant-store.service';

/**
 * Top-level page for /assistant. Phase 01 wires the SSE backend; Phase 02
 * fills in the layout slots. Provides AssistantStore so each route
 * activation gets a fresh thread.
 */
@Component({
  selector: 'cce-assistant-page',
  standalone: true,
  imports: [TranslateModule],
  providers: [AssistantStore],
  templateUrl: './assistant.page.html',
  styleUrl: './assistant.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssistantPage {}

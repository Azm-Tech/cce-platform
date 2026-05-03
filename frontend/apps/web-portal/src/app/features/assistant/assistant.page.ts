import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { AssistantStore } from './thread/assistant-store.service';
import { ComposeBoxComponent } from './thread/compose-box.component';
import { MessageListComponent } from './thread/message-list.component';

/**
 * Top-level page for /assistant. Owns the AssistantStore (provided per
 * route so each activation gets a fresh thread) and renders the
 * scrollable message list above a sticky compose box. Phase 03 adds
 * the URL ?q= deep-link auto-send and the clear-thread confirm dialog.
 */
@Component({
  selector: 'cce-assistant-page',
  standalone: true,
  imports: [
    TranslateModule,
    MatButtonModule,
    MatIconModule,
    MessageListComponent,
    ComposeBoxComponent,
  ],
  providers: [AssistantStore],
  templateUrl: './assistant.page.html',
  styleUrl: './assistant.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssistantPage {
  readonly store = inject(AssistantStore);

  clear(): void {
    this.store.clear();
  }
}

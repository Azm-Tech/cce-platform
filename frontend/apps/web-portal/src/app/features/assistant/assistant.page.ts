import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import {
  ConfirmDialogComponent,
  type ConfirmDialogData,
} from '../interactive-city/builder/confirm-dialog.component';
import { AssistantStore } from './thread/assistant-store.service';
import { ComposeBoxComponent } from './thread/compose-box.component';
import { MessageListComponent } from './thread/message-list.component';

/**
 * Top-level page for /assistant. Owns the AssistantStore (provided per
 * route so each activation gets a fresh thread) and renders the
 * scrollable message list above a sticky compose box.
 *
 * On init: if `?q=…` is present and the thread is empty, auto-sends
 * the question (deep-link entry from search results / external links).
 * Removes `q` from the URL on success so the deep link doesn't double-fire.
 *
 * Clear-thread button confirms via the shared ConfirmDialogComponent
 * (reused from Sub-8) when the thread is non-empty.
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
export class AssistantPage implements OnInit {
  readonly store = inject(AssistantStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap.get('q');
    if (q && q.trim() !== '' && this.store.messages().length === 0) {
      // Strip the q param so a refresh doesn't re-fire the auto-send.
      void this.router.navigate([], {
        queryParams: { q: null },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
      void this.store.sendMessage(q);
    }
  }

  async clear(): Promise<void> {
    if (this.store.messages().length === 0 || this.store.streaming()) return;
    const data: ConfirmDialogData = {
      titleKey: 'assistant.thread.clearConfirmTitle',
      bodyKey: 'assistant.thread.clearConfirmBody',
      confirmKey: 'assistant.thread.clearConfirm',
      cancelKey: 'assistant.thread.clearCancel',
      dangerous: true,
    };
    const ref = this.dialog.open<ConfirmDialogComponent, ConfirmDialogData, boolean>(
      ConfirmDialogComponent,
      { data, width: '420px' },
    );
    const ok = await firstValueFrom(ref.afterClosed());
    if (ok) this.store.clear();
  }
}

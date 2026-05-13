import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import {
  ConfirmDialogComponent,
  type ConfirmDialogData,
} from '../interactive-city/builder/confirm-dialog.component';
import { AssistantStore } from './thread/assistant-store.service';
import { ComposeBoxComponent } from './thread/compose-box.component';
import { MessageListComponent } from './thread/message-list.component';

interface SuggestedPrompt {
  id: string;
  icon: string;
  tag: string;
  tier: 'policy' | 'energy' | 'circular' | 'finance' | 'cities' | 'tech';
  text: string;
}

const SUGGESTED_PROMPTS: SuggestedPrompt[] = [
  { id: 'p1', icon: 'gavel',          tag: 'POLICY',           tier: 'policy',   text: "What are Saudi Vision 2030's headline carbon-reduction targets?" },
  { id: 'p2', icon: 'wb_sunny',       tag: 'ENERGY',           tier: 'energy',   text: 'Compare green hydrogen production economics at NEOM vs. Yanbu.' },
  { id: 'p3', icon: 'recycling',      tag: 'CIRCULAR ECONOMY', tier: 'circular', text: 'Explain industrial symbiosis with examples from Yanbu Industrial City.' },
  { id: 'p4', icon: 'account_balance',tag: 'FINANCE',          tier: 'finance',  text: 'How do compliance and voluntary carbon markets differ?' },
  { id: 'p5', icon: 'apartment',      tag: 'CITIES',           tier: 'cities',   text: 'What sustainability KPIs does Riyadh use to track progress?' },
  { id: 'p6', icon: 'memory',         tag: 'TECH',             tier: 'tech',     text: 'How does CCUS work, and where is it being deployed today?' },
];

/**
 * Assistant dialog — overlay version of the AI assistant.
 *
 * The shell's bottom-left FAB opens this dialog instead of navigating
 * to the dedicated `/assistant` page. The dialog provides its own
 * `AssistantStore` instance so the conversation lives only as long as
 * the dialog is open (closing it clears the thread). Visually mirrors
 * the full page: hero + suggested prompts on first open, conversation
 * thread + compose box afterwards.
 */
@Component({
  selector: 'cce-assistant-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    TranslateModule,
    MessageListComponent,
    ComposeBoxComponent,
  ],
  providers: [AssistantStore],
  templateUrl: './assistant.dialog.html',
  styleUrl: './assistant.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssistantDialogComponent {
  readonly store = inject(AssistantStore);
  private readonly dialogRef = inject(MatDialogRef<AssistantDialogComponent>);
  private readonly dialog = inject(MatDialog);

  readonly suggestedPrompts = SUGGESTED_PROMPTS;
  readonly isEmpty = computed(
    () => this.store.messages().length === 0 && !this.store.streaming(),
  );

  useSuggestion(text: string): void {
    if (this.store.streaming()) return;
    void this.store.sendMessage(text);
  }

  /**
   * Wipe the thread — but only after the user confirms via the shared
   * ConfirmDialogComponent. Without this guard the user could lose
   * their entire conversation with a single accidental tap.
   */
  async clearThread(): Promise<void> {
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

  close(): void {
    this.dialogRef.close();
  }
}

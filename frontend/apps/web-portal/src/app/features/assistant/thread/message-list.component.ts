import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Stub for Phase 02 — scroll-y message list with auto-scroll + aria-live.
 */
@Component({
  selector: 'cce-message-list',
  standalone: true,
  imports: [],
  templateUrl: './message-list.component.html',
  styleUrl: './message-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MessageListComponent {}

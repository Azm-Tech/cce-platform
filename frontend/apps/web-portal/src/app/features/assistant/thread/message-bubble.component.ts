import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Stub for Phase 02 — single role-styled message bubble with citation footer.
 */
@Component({
  selector: 'cce-message-bubble',
  standalone: true,
  imports: [],
  templateUrl: './message-bubble.component.html',
  styleUrl: './message-bubble.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MessageBubbleComponent {}

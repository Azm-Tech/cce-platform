import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MAT_SNACK_BAR_DATA, MatSnackBarRef } from '@angular/material/snack-bar';

/** Pre-resolved (already localized) content for the live notification toast. */
export interface NotificationToastData {
  /** Localized subject — the headline. */
  title: string;
  /** Localized body snippet (plain text, may be empty). */
  body: string;
  /** Short time label, e.g. "now". */
  time: string;
  /** "View" action label. */
  actionLabel: string;
  /** Close-button aria-label. */
  dismissLabel: string;
  /** Material icon name for the leading chip. */
  icon: string;
  /** Auto-dismiss duration (ms) — drives the depleting time bar. */
  durationMs: number;
}

/**
 * Branded live-notification toast. Rendered in a snackbar overlay via
 * `MatSnackBar.openFromComponent`; the host panel chrome is neutralized in
 * `_fancy.scss` (`.cce-notif-toast`) so this card is the whole visual.
 *
 * Default change detection (overlay boundary) — content is plain pre-localized
 * strings, so no Transloco pipe is used here.
 */
@Component({
  selector: 'cce-notification-toast',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './notification-toast.component.html',
  styleUrl: './notification-toast.component.scss',
  changeDetection: ChangeDetectionStrategy.Default,
})
export class NotificationToastComponent {
  readonly data = inject<NotificationToastData>(MAT_SNACK_BAR_DATA);
  private readonly ref = inject<MatSnackBarRef<NotificationToastComponent>>(MatSnackBarRef);

  /** Dismiss without acting. */
  close(): void {
    this.ref.dismiss();
  }

  /** Dismiss and signal the "View" action (host opens the drawer). */
  view(): void {
    this.ref.dismissWithAction();
  }
}

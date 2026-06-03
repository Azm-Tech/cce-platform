import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { TranslocoModule } from '@jsverse/transloco';
import { ToastService } from '@frontend/ui-kit';

/**
 * Reusable share menu — US-011.
 *
 * Opens a menu with three sharing options:
 *   • Copy link  → navigator.clipboard
 *   • Email      → opens `mailto:?subject=…&body=…` in a new tab
 *   • Native share (mobile) → navigator.share(), only shown when available
 *
 * On success fires the CON003 toast; on failure fires ERR004.
 * Pass `title` and (optionally) `url` — url defaults to the current page URL.
 */
@Component({
  selector: 'cce-share-menu',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatMenuModule, TranslocoModule],
  template: `
    <button
      type="button"
      mat-stroked-button
      class="cce-share-menu__trigger"
      [matMenuTriggerFor]="menu"
      [attr.aria-label]="'share.button' | transloco"
    >
      <mat-icon>share</mat-icon>
      <span>{{ 'share.button' | transloco }}</span>
    </button>
    <mat-menu #menu="matMenu">
      <button mat-menu-item (click)="copyLink()">
        <mat-icon>link</mat-icon>
        <span>{{ 'share.copyLink' | transloco }}</span>
      </button>
      <button mat-menu-item (click)="shareByEmail()">
        <mat-icon>email</mat-icon>
        <span>{{ 'share.email' | transloco }}</span>
      </button>
      @if (canNativeShare()) {
        <button mat-menu-item (click)="shareNative()">
          <mat-icon>ios_share</mat-icon>
          <span>{{ 'share.native' | transloco }}</span>
        </button>
      }
    </mat-menu>
  `,
  styles: [`
    .cce-share-menu__trigger { display: inline-flex; align-items: center; gap: 0.4rem; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShareMenuComponent {
  private readonly toast = inject(ToastService);

  /** The title of the item being shared (used in mailto subject + native share). */
  readonly title = input<string>('');
  /** Optional URL override; defaults to the current page URL when omitted. */
  readonly url = input<string | null>(null);

  readonly canNativeShare = signal(
    typeof navigator !== 'undefined' && typeof navigator.share === 'function',
  );

  readonly currentUrl = computed(() => {
    const explicit = this.url();
    if (explicit) return explicit;
    return typeof window !== 'undefined' ? window.location.href : '';
  });

  async copyLink(): Promise<void> {
    const link = this.currentUrl();
    if (!link) {
      this.toast.error('errors.ERR004');
      return;
    }
    try {
      await navigator.clipboard.writeText(link);
      this.toast.success('confirmations.CON003');
    } catch {
      this.toast.error('errors.ERR004');
    }
  }

  shareByEmail(): void {
    const link = this.currentUrl();
    if (!link) {
      this.toast.error('errors.ERR004');
      return;
    }
    const subject = encodeURIComponent(this.title() || link);
    const body = encodeURIComponent(`${this.title()}\n\n${link}`);
    try {
      window.location.href = `mailto:?subject=${subject}&body=${body}`;
      this.toast.success('confirmations.CON003');
    } catch {
      this.toast.error('errors.ERR004');
    }
  }

  async shareNative(): Promise<void> {
    const link = this.currentUrl();
    if (!link || typeof navigator === 'undefined' || typeof navigator.share !== 'function') {
      this.toast.error('errors.ERR004');
      return;
    }
    try {
      await navigator.share({ title: this.title(), url: link });
      this.toast.success('confirmations.CON003');
    } catch (err) {
      // User cancellation throws — silently ignore AbortError, surface anything else.
      if ((err as DOMException).name !== 'AbortError') {
        this.toast.error('errors.ERR004');
      }
    }
  }
}

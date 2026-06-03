import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { ToastService } from '@frontend/ui-kit';
import { EventsApiService } from '../../features/events/events-api.service';

export type CalendarProvider = 'google' | 'outlook' | 'apple' | 'yahoo';

export interface CalendarEventInput {
  id: string;
  title: string;
  description?: string | null;
  location?: string | null;
  startsOn: string; // ISO datetime
  endsOn: string;
}

/**
 * Reusable "Add to Calendar" split button — US-013.
 *
 * Option B: a smart primary button (auto-picked from the user's OS —
 * Apple on macOS/iOS, Outlook on Windows, Google on Android, Apple/ICS
 * as the cross-platform fallback) plus a dropdown caret that exposes
 * the remaining providers.
 *
 * Google / Outlook / Yahoo open a deep-link in a new tab.
 * Apple Calendar triggers the existing `/api/events/{id}.ics` download.
 *
 * Fires confirmations.CON004 on success and errors.ERR006 on failure.
 */
@Component({
  selector: 'cce-calendar-menu',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    TranslocoModule,
  ],
  template: `
    <div class="cce-calendar-menu" role="group">
      <button
        type="button"
        mat-stroked-button
        class="cce-calendar-menu__primary"
        [disabled]="downloading()"
        (click)="open(primary())"
      >
        @if (downloading()) {
          <mat-spinner diameter="18" />
        } @else {
          <mat-icon>event_available</mat-icon>
        }
        <span>{{ ('events.calendar.addTo.' + primary()) | transloco }}</span>
      </button>
      <button
        type="button"
        mat-stroked-button
        class="cce-calendar-menu__caret"
        [matMenuTriggerFor]="menu"
        [attr.aria-label]="'events.calendar.moreOptions' | transloco"
        [disabled]="downloading()"
      >
        <mat-icon>arrow_drop_down</mat-icon>
      </button>
      <mat-menu #menu="matMenu">
        @for (p of others(); track p) {
          <button mat-menu-item (click)="open(p)">
            <mat-icon>{{ iconFor(p) }}</mat-icon>
            <span>{{ ('events.calendar.addTo.' + p) | transloco }}</span>
          </button>
        }
      </mat-menu>
    </div>
  `,
  styles: [`
    .cce-calendar-menu { display: inline-flex; }
    .cce-calendar-menu__primary {
      display: inline-flex; align-items: center; gap: 0.4rem;
      border-end-end-radius: 0; border-start-end-radius: 0;
    }
    .cce-calendar-menu__caret {
      min-width: 0; padding: 0 0.4rem;
      border-end-start-radius: 0; border-start-start-radius: 0;
      border-inline-start: 0;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarMenuComponent {
  private readonly api = inject(EventsApiService);
  private readonly toast = inject(ToastService);

  readonly event = input.required<CalendarEventInput>();

  readonly downloading = signal(false);

  /** All four providers, in display order. */
  private readonly providers: readonly CalendarProvider[] = ['google', 'apple', 'outlook', 'yahoo'];

  /** Auto-pick the primary provider from the user's OS / user-agent. */
  readonly primary = computed<CalendarProvider>(() => {
    if (typeof navigator === 'undefined') return 'apple';
    const ua = navigator.userAgent.toLowerCase();
    if (/iphone|ipad|ipod|macintosh/.test(ua)) return 'apple';
    if (/android/.test(ua)) return 'google';
    if (/windows|win32|win64/.test(ua)) return 'outlook';
    return 'apple';
  });

  readonly others = computed(() => this.providers.filter((p) => p !== this.primary()));

  iconFor(p: CalendarProvider): string {
    switch (p) {
      case 'google':  return 'event';
      case 'outlook': return 'mail';
      case 'apple':   return 'calendar_month';
      case 'yahoo':   return 'event_note';
    }
  }

  open(p: CalendarProvider): void {
    if (p === 'apple') {
      void this.downloadIcs();
    } else {
      this.openProviderUrl(p);
    }
  }

  private openProviderUrl(p: CalendarProvider): void {
    const url = this.buildUrl(p);
    if (!url) {
      this.toast.error('errors.ERR006');
      return;
    }
    const win = window.open(url, '_blank', 'noopener,noreferrer');
    if (!win) {
      this.toast.error('errors.ERR006');
      return;
    }
    this.toast.success('confirmations.CON004');
  }

  private async downloadIcs(): Promise<void> {
    const e = this.event();
    this.downloading.set(true);
    const res = await this.api.downloadIcs(e.id);
    this.downloading.set(false);
    if (!res.ok) {
      this.toast.error('errors.ERR006');
      return;
    }
    try {
      const url = URL.createObjectURL(res.value);
      const a = document.createElement('a');
      a.href = url;
      a.download = this.icsFilename();
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
      this.toast.success('confirmations.CON004');
    } catch {
      this.toast.error('errors.ERR006');
    }
  }

  private buildUrl(p: CalendarProvider): string | null {
    const e = this.event();
    const title = encodeURIComponent(e.title);
    const description = encodeURIComponent(e.description ?? '');
    const location = encodeURIComponent(e.location ?? '');
    const start = new Date(e.startsOn);
    const end = new Date(e.endsOn);
    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return null;

    switch (p) {
      case 'google': {
        const fmt = (d: Date) => d.toISOString().replace(/[-:]|\.\d{3}/g, '');
        return (
          `https://calendar.google.com/calendar/render?action=TEMPLATE` +
          `&text=${title}&dates=${fmt(start)}/${fmt(end)}` +
          `&details=${description}&location=${location}`
        );
      }
      case 'outlook': {
        return (
          `https://outlook.live.com/calendar/0/deeplink/compose?path=/calendar/action/compose&rru=addevent` +
          `&subject=${title}&startdt=${encodeURIComponent(start.toISOString())}` +
          `&enddt=${encodeURIComponent(end.toISOString())}` +
          `&body=${description}&location=${location}`
        );
      }
      case 'yahoo': {
        const fmt = (d: Date) => d.toISOString().replace(/[-:]|\.\d{3}/g, '');
        const durMins = Math.max(0, Math.round((end.getTime() - start.getTime()) / 60000));
        const dur = `${String(Math.floor(durMins / 60)).padStart(2, '0')}${String(durMins % 60).padStart(2, '0')}`;
        return (
          `https://calendar.yahoo.com/?v=60&title=${title}&st=${fmt(start)}` +
          `&dur=${dur}&desc=${description}&in_loc=${location}`
        );
      }
      case 'apple':
        return null; // handled via ICS download
    }
  }

  private icsFilename(): string {
    const safe = this.event().title.replace(/[^a-zA-Z0-9_-]+/g, '-').replace(/^-+|-+$/g, '') || 'event';
    return `${safe}.ics`;
  }
}

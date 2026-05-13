import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { EventsApiService } from './events-api.service';
import type { Event } from './event.types';

type TimeBucket = 'upcoming' | 'today' | 'live' | 'past';

/**
 * Public event detail page — modern + simple, brand greens.
 *
 * Hero banner with the event image (or brand-gradient fallback) + a
 * floating day-pill, type/status chips, big title, and a meta strip
 * (date · time · duration · venue). Action bar surfaces the primary
 * CTAs: Add to calendar (.ics download), Join online / Open in maps,
 * Share (copy link).
 */
@Component({
  selector: 'cce-event-detail',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink,
    MatButtonModule, MatIconModule, MatProgressBarModule, MatProgressSpinnerModule,
    TranslateModule,
  ],
  templateUrl: './event-detail.page.html',
  styleUrl: './event-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventDetailPage implements OnInit {
  private readonly api = inject(EventsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);
  private readonly toast = inject(ToastService);
  private readonly t = inject(TranslateService);

  readonly event = signal<Event | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly downloading = signal(false);
  readonly imageFailed = signal(false);

  readonly locale = this.localeService.locale;

  readonly notFound = computed(() => this.errorKind() === 'not-found');

  readonly title = computed(() => {
    const e = this.event();
    if (!e) return '';
    return this.locale() === 'ar' ? e.titleAr : e.titleEn;
  });

  readonly description = computed(() => {
    const e = this.event();
    if (!e) return '';
    return this.locale() === 'ar' ? e.descriptionAr : e.descriptionEn;
  });

  /** Resolved venue line: online URL when online, locale-aware location
   *  otherwise. Returns null when both are missing — UI shows "TBA". */
  readonly venue = computed<string | null>(() => {
    const e = this.event();
    if (!e) return null;
    if (e.onlineMeetingUrl) return e.onlineMeetingUrl;
    return this.locale() === 'ar' ? e.locationAr : e.locationEn;
  });

  readonly isOnline = computed(() => !!this.event()?.onlineMeetingUrl);

  /** Has-image guard that also flips false when the image URL fails to
   *  load (the template binds an `(error)` handler to set imageFailed). */
  readonly hasImage = computed(() => {
    const e = this.event();
    return !!e?.featuredImageUrl && !this.imageFailed();
  });

  /** Live time bucket: live (in progress), today (starts today, not yet
   *  started), upcoming (>= tomorrow), past (already ended). */
  readonly timeBucket = computed<TimeBucket>(() => {
    const e = this.event();
    if (!e) return 'upcoming';
    const now = Date.now();
    const start = new Date(e.startsOn).getTime();
    const end = new Date(e.endsOn).getTime();
    if (Number.isNaN(start) || Number.isNaN(end)) return 'upcoming';
    if (start <= now && end >= now) return 'live';
    if (end < now) return 'past';
    const startDate = new Date(start);
    const today = new Date(now);
    const sameDay =
      startDate.getFullYear() === today.getFullYear() &&
      startDate.getMonth() === today.getMonth() &&
      startDate.getDate() === today.getDate();
    if (sameDay) return 'today';
    return 'upcoming';
  });

  /** Localized countdown / past-time string used in the hero. */
  readonly countdown = computed<string>(() => {
    const e = this.event();
    if (!e) return '';
    const bucket = this.timeBucket();
    if (bucket === 'live') return this.t.instant('events.detail.countdownLive');
    if (bucket === 'today') return this.t.instant('events.detail.countdownToday');

    const now = Date.now();
    const target =
      bucket === 'past'
        ? new Date(e.endsOn).getTime()
        : new Date(e.startsOn).getTime();
    if (Number.isNaN(target)) return '';
    const diffMs = Math.abs(target - now);

    const minutes = Math.floor(diffMs / 60_000);
    const hours = Math.floor(diffMs / 3_600_000);
    const days = Math.floor(diffMs / 86_400_000);
    const weeks = Math.floor(diffMs / 604_800_000);

    let value = 0;
    let unitKey = '';
    if (weeks >= 2) {
      value = weeks; unitKey = 'events.detail.unitWeeks';
    } else if (days >= 1) {
      value = days; unitKey = days === 1 ? 'events.detail.unitDay' : 'events.detail.unitDays';
    } else if (hours >= 1) {
      value = hours; unitKey = hours === 1 ? 'events.detail.unitHour' : 'events.detail.unitHours';
    } else {
      value = Math.max(1, minutes);
      unitKey = value === 1 ? 'events.detail.unitMinute' : 'events.detail.unitMinutes';
    }
    const unit = this.t.instant(unitKey);
    const phrase = bucket === 'past'
      ? 'events.detail.countdownEndedAgo'
      : 'events.detail.countdownIn';
    return this.t.instant(phrase, { value, unit });
  });

  /** Type chip key (online / in person / webinar). */
  readonly typeTagKey = computed<string>(() => {
    const e = this.event();
    if (!e) return 'events.tagInPerson';
    return e.onlineMeetingUrl ? 'events.tagOnline' : 'events.tagInPerson';
  });

  /** Skeleton lines used while loading. */
  readonly skeletons = Array.from({ length: 3 });

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorKind.set('not-found');
      return;
    }
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getEvent(id);
    this.loading.set(false);
    if (res.ok) this.event.set(res.value);
    else this.errorKind.set(res.error.kind);
  }

  retry(): void {
    void this.ngOnInit();
  }

  onImageError(): void {
    this.imageFailed.set(true);
  }

  async exportToCalendar(): Promise<void> {
    const e = this.event();
    if (!e) return;
    this.downloading.set(true);
    const res = await this.api.downloadIcs(e.id);
    this.downloading.set(false);
    if (!res.ok) {
      this.toast.error(`errors.${res.error.kind}`);
      return;
    }
    this.saveBlob(res.value, this.filenameFor(e));
    this.toast.success('events.export.toast');
  }

  /** Open the event's meeting link (online events). Opens in a new
   *  tab with `noopener` for safety. */
  openMeeting(): void {
    const url = this.event()?.onlineMeetingUrl;
    if (!url) return;
    window.open(url, '_blank', 'noopener,noreferrer');
  }

  /** Open the event's location in the user's default map provider. */
  openInMaps(): void {
    const loc = this.locale() === 'ar'
      ? this.event()?.locationAr
      : this.event()?.locationEn;
    if (!loc) return;
    const q = encodeURIComponent(loc);
    window.open(`https://www.google.com/maps/search/?api=1&query=${q}`, '_blank', 'noopener,noreferrer');
  }

  async copyLink(): Promise<void> {
    const url = window.location.href;
    try {
      await navigator.clipboard.writeText(url);
      this.toast.success('events.detail.shareCopiedToast');
    } catch {
      window.prompt('Copy link', url);
    }
  }

  private filenameFor(e: Event): string {
    const safeTitle = e.titleEn.replace(/[^a-zA-Z0-9_-]+/g, '-').replace(/^-+|-+$/g, '') || 'event';
    return `${safeTitle}.ics`;
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}

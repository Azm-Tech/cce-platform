import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { CalendarMenuComponent, type CalendarEventInput } from '../../shared/calendar-menu/calendar-menu.component';
import { ShareMenuComponent } from '../../shared/share-menu/share-menu.component';
import type { Event as EventModel } from './event.types';

/**
 * Public event card — vertical layout matching the unified News & Events
 * design:
 *   [ image header with type-badge (upcoming / past / today) ]
 *   [ meta row: date + venue (or Zoom) ]
 *   [ title ]
 *   [ excerpt (description) ]
 *   [ footer: share + (upcoming only) add-to-calendar ]
 */
@Component({
  selector: 'cce-event-card',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink, MatIconModule, TranslocoModule,
    CalendarMenuComponent, ShareMenuComponent,
  ],
  template: `
    <article class="cce-event-card">
      <a class="cce-event-card__media"
         [routerLink]="['/events', event().id]"
         [attr.aria-label]="title()">
        @if (event().featuredImageUrl; as src) {
          <img [src]="src" [alt]="title()" loading="lazy" referrerpolicy="no-referrer" />
        } @else {
          <span class="cce-event-card__media-icon" aria-hidden="true">
            <mat-icon>image</mat-icon>
          </span>
        }
        <span class="cce-event-card__badge"
              [class.cce-event-card__badge--past]="timeBucket() === 'past'"
              [class.cce-event-card__badge--today]="timeBucket() === 'today'">
          <mat-icon aria-hidden="true">event</mat-icon>
          @if (timeBucket() === 'today') {
            {{ 'events.tagToday' | transloco }}
          } @else if (timeBucket() === 'past') {
            {{ 'events.tagPast' | transloco }}
          } @else {
            {{ 'events.tagUpcoming' | transloco }}
          }
        </span>
      </a>

      <div class="cce-event-card__content">
        <div class="cce-event-card__meta">
          <span class="cce-event-card__meta-item">
            <mat-icon aria-hidden="true">calendar_today</mat-icon>
            {{ event().startsOn | date:'longDate' }}
          </span>
          @if (venueLabel(); as v) {
            <span class="cce-event-card__meta-item cce-event-card__meta-loc">
              <mat-icon aria-hidden="true">{{ event().onlineMeetingUrl ? 'videocam' : 'location_on' }}</mat-icon>
              {{ v }}
            </span>
          }
        </div>

        @if (topicLabel(); as topic) {
          <span class="cce-event-card__topic">{{ topic }}</span>
        }

        <a class="cce-event-card__title-link"
           [routerLink]="['/events', event().id]"
           [attr.aria-label]="title()">
          <h3 class="cce-event-card__title">{{ title() }}</h3>
        </a>

        @if (excerpt()) {
          <p class="cce-event-card__excerpt">{{ excerpt() }}</p>
        }
      </div>

      <footer class="cce-event-card__foot">
        <cce-share-menu [title]="title()" [url]="absoluteUrl()" />
        @if (timeBucket() !== 'past') {
          <cce-calendar-menu [event]="calendarPayload()" />
        }
      </footer>
    </article>
  `,
  styleUrl: './event-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventCardComponent {
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  readonly event = input.required<EventModel>();
  readonly locale = input<'ar' | 'en'>('en');

  readonly title = computed(() => {
    const e = this.event();
    return this.locale() === 'ar' ? e.titleAr : e.titleEn;
  });

  /** Localized topic chip (US010 AC3 — list shows Title, Date, Topic). */
  readonly topicLabel = computed<string | null>(() => {
    const e = this.event();
    return (this.locale() === 'ar' ? e.topicNameAr : e.topicNameEn) || null;
  });

  readonly excerpt = computed(() => {
    const e = this.event();
    const content = this.locale() === 'ar' ? e.descriptionAr : e.descriptionEn;
    const stripped = (content ?? '').replace(/<[^>]*>/g, '').trim();
    return stripped.length > 140 ? stripped.slice(0, 140) + '…' : stripped;
  });

  /** Show the online URL label when online; otherwise locale-aware location. */
  readonly venueLabel = computed<string | null>(() => {
    this.locale(); // reactive dependency for language switch
    const e = this.event();
    if (e.onlineMeetingUrl) {
      const url = e.onlineMeetingUrl.toLowerCase();
      if (url.includes('zoom')) return this.transloco.translate('events.venue.zoom');
      if (url.includes('teams.microsoft')) return this.transloco.translate('events.venue.teams');
      if (url.includes('meet.google')) return this.transloco.translate('events.venue.meet');
      return this.transloco.translate('events.venue.online');
    }
    return this.locale() === 'ar' ? e.locationAr : e.locationEn;
  });

  readonly timeBucket = computed<'today' | 'upcoming' | 'past'>(() => {
    const e = this.event();
    const now = Date.now();
    const start = new Date(e.startsOn).getTime();
    const end = new Date(e.endsOn).getTime();
    if (Number.isNaN(start)) return 'upcoming';
    const sameDay = (a: number, b: number) => {
      const da = new Date(a);
      const db = new Date(b);
      return da.getFullYear() === db.getFullYear()
        && da.getMonth() === db.getMonth()
        && da.getDate() === db.getDate();
    };
    if (sameDay(start, now) || (start <= now && end >= now)) return 'today';
    if (start < now) return 'past';
    return 'upcoming';
  });

  readonly calendarPayload = computed<CalendarEventInput>(() => {
    const e = this.event();
    const loc = this.locale() === 'ar' ? e.locationAr : e.locationEn;
    const desc = this.locale() === 'ar' ? e.descriptionAr : e.descriptionEn;
    return {
      id: e.id,
      title: this.title(),
      description: desc,
      location: e.onlineMeetingUrl ?? loc,
      startsOn: e.startsOn,
      endsOn: e.endsOn,
    };
  });

  readonly absoluteUrl = computed<string | null>(() => {
    if (typeof window === 'undefined') return null;
    const tree = this.router.createUrlTree(['/events', this.event().id]);
    return new URL(tree.toString(), window.location.origin).toString();
  });
}

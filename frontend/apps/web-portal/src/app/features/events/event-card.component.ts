import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import type { Event as EventModel } from './event.types';

/**
 * Public event card. Same horizontal 75/25 layout as news + topic
 * cards. The eyebrow shows ONLINE / IN PERSON / WEBINAR + a relative-
 * time indicator (TODAY / UPCOMING / PAST). The media column shows
 * the event image when present, otherwise a brand gradient + the
 * event icon (`event` for in-person, `videocam` for online).
 */
@Component({
  selector: 'cce-event-card',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, MatIconModule, TranslateModule],
  template: `
    <a class="cce-event-card" [routerLink]="['/events', event().id]"
       [attr.aria-label]="title()">
      <div class="cce-event-card__media"
           [class.cce-event-card__media--placeholder]="!event().featuredImageUrl">
        @if (event().featuredImageUrl; as src) {
          <img [src]="src" [alt]="title()" loading="lazy" referrerpolicy="no-referrer" />
        } @else {
          <span class="cce-event-card__media-icon" aria-hidden="true">
            <mat-icon>{{ event().onlineMeetingUrl ? 'videocam' : 'event' }}</mat-icon>
          </span>
        }
        <!-- Day-pill on the media: shows the day number + month. -->
        <span class="cce-event-card__day-pill" aria-hidden="true">
          <span class="cce-event-card__day-pill-day">{{ event().startsOn | date:'d' }}</span>
          <span class="cce-event-card__day-pill-mon">{{ event().startsOn | date:'MMM' }}</span>
        </span>
      </div>

      <div class="cce-event-card__content">
        <span class="cce-event-card__eyebrow-row">
          <span class="cce-event-card__eyebrow"
                [class.cce-event-card__eyebrow--online]="event().onlineMeetingUrl">
            @if (event().onlineMeetingUrl) {
              <mat-icon aria-hidden="true">videocam</mat-icon>
              {{ 'events.tagOnline' | translate }}
            } @else {
              <mat-icon aria-hidden="true">place</mat-icon>
              {{ 'events.tagInPerson' | translate }}
            }
          </span>
          <span class="cce-event-card__when"
                [class.cce-event-card__when--past]="timeBucket() === 'past'"
                [class.cce-event-card__when--today]="timeBucket() === 'today'">
            @if (timeBucket() === 'today') {
              {{ 'events.tagToday' | translate }}
            } @else if (timeBucket() === 'past') {
              {{ 'events.tagPast' | translate }}
            } @else {
              {{ 'events.tagUpcoming' | translate }}
            }
          </span>
        </span>

        <h3 class="cce-event-card__title">{{ title() }}</h3>

        <ul class="cce-event-card__meta" role="list">
          <li>
            <mat-icon aria-hidden="true">event</mat-icon>
            {{ event().startsOn | date:'mediumDate' }}
          </li>
          <li>
            <mat-icon aria-hidden="true">schedule</mat-icon>
            {{ event().startsOn | date:'shortTime' }}
          </li>
          @if (locationLabel(); as loc) {
            <li class="cce-event-card__meta-loc">
              <mat-icon aria-hidden="true">location_on</mat-icon>
              {{ loc }}
            </li>
          }
        </ul>

        <div class="cce-event-card__foot">
          <span class="cce-event-card__cta">
            {{ 'events.viewEvent' | translate }}
            <mat-icon aria-hidden="true">arrow_forward</mat-icon>
          </span>
        </div>
      </div>
    </a>
  `,
  styleUrl: './event-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventCardComponent {
  readonly event = input.required<EventModel>();
  readonly locale = input<'ar' | 'en'>('en');

  readonly title = computed(() => {
    const e = this.event();
    return this.locale() === 'ar' ? e.titleAr : e.titleEn;
  });

  readonly locationLabel = computed<string | null>(() => {
    const e = this.event();
    if (e.onlineMeetingUrl) return null; // covered by ONLINE chip
    return this.locale() === 'ar' ? e.locationAr : e.locationEn;
  });

  /** Bucket the event into today / upcoming / past for the time chip. */
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
}

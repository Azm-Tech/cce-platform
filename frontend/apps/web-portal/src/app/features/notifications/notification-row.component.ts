import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import type { UserNotification } from './notification.types';

@Component({
  selector: 'cce-notification-row',
  standalone: true,
  imports: [CommonModule, DatePipe, MatButtonModule, MatIconModule, TranslateModule],
  template: `
    <article
      class="cce-notification-row"
      [class.cce-notification-row--unread]="isUnread()"
      role="listitem"
    >
      <span class="cce-notification-row__dot" aria-hidden="true"></span>

      <div class="cce-notification-row__body">
        <header class="cce-notification-row__header">
          <h3 class="cce-notification-row__subject">{{ subject() }}</h3>
          <span class="cce-notification-row__channel">
            {{ ('notifications.channel.' + notification().channel) | translate }}
          </span>
        </header>
        <p class="cce-notification-row__excerpt">{{ excerpt() }}</p>
        @if (notification().sentOn) {
          <p class="cce-notification-row__date">
            <small>{{ notification().sentOn | date:'medium' }}</small>
          </p>
        }
      </div>

      @if (isUnread()) {
        <button
          type="button"
          mat-icon-button
          class="cce-notification-row__action"
          [attr.aria-label]="'notifications.markRead' | translate"
          (click)="markRead.emit(notification().id)"
        >
          <mat-icon>done</mat-icon>
        </button>
      }
    </article>
  `,
  styleUrl: './notification-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationRowComponent {
  readonly notification = input.required<UserNotification>();
  readonly locale = input<'ar' | 'en'>('en');
  readonly markRead = output<string>();

  readonly isUnread = computed(() => this.notification().status !== 'Read');

  readonly subject = computed(() => {
    const n = this.notification();
    return this.locale() === 'ar' ? n.renderedSubjectAr : n.renderedSubjectEn;
  });

  readonly excerpt = computed(() => {
    const stripped = this.notification().renderedBody.replace(/<[^>]*>/g, '').trim();
    return stripped.length > 200 ? stripped.slice(0, 200) + '…' : stripped;
  });
}

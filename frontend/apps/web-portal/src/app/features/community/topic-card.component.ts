import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import type { PublicTopic } from './community.types';

/**
 * Public community topic card. Same horizontal 75/25 layout as the
 * news card. Topics don't have hero images, so the media column is
 * always a brand gradient with the topic icon (or default `forum`).
 */
@Component({
  selector: 'cce-topic-card',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, TranslateModule],
  template: `
    <a class="cce-topic-card" [routerLink]="['/community', 'topics', topic().slug]"
       [attr.aria-label]="name()">
      <div class="cce-topic-card__media">
        <span class="cce-topic-card__media-icon" aria-hidden="true">
          @if (topic().iconUrl; as src) {
            <img [src]="src" [alt]="name()" />
          } @else {
            <mat-icon>forum</mat-icon>
          }
        </span>
      </div>

      <div class="cce-topic-card__content">
        <span class="cce-topic-card__eyebrow">{{ 'community.tag' | translate }}</span>
        <h3 class="cce-topic-card__title">{{ name() }}</h3>
        <p class="cce-topic-card__excerpt">{{ description() }}</p>

        <div class="cce-topic-card__foot">
          <span class="cce-topic-card__cta">
            {{ 'community.openTopic' | translate }}
            <mat-icon aria-hidden="true">arrow_forward</mat-icon>
          </span>
        </div>
      </div>
    </a>
  `,
  styleUrl: './topic-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicCardComponent {
  readonly topic = input.required<PublicTopic>();
  readonly locale = input<'ar' | 'en'>('en');

  readonly name = computed(() => {
    const t = this.topic();
    return this.locale() === 'ar' ? t.nameAr : t.nameEn;
  });

  readonly description = computed(() => {
    const t = this.topic();
    const raw = this.locale() === 'ar' ? t.descriptionAr : t.descriptionEn;
    const stripped = raw.replace(/<[^>]*>/g, '').trim();
    return stripped.length > 180 ? stripped.slice(0, 180) + '…' : stripped;
  });
}

import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import type { PublicTopic } from './community.types';

@Component({
  selector: 'cce-topic-card',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatIconModule, TranslateModule],
  template: `
    <a class="cce-topic-card" [routerLink]="['/community', 'topics', topic().slug]">
      <mat-card>
        <mat-card-header>
          @if (topic().iconUrl) {
            <img mat-card-avatar [src]="topic().iconUrl" [alt]="name()" class="cce-topic-card__icon" />
          } @else {
            <mat-icon mat-card-avatar class="cce-topic-card__icon-fallback" aria-hidden="true">forum</mat-icon>
          }
          <mat-card-title>{{ name() }}</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p class="cce-topic-card__description">{{ description() }}</p>
        </mat-card-content>
      </mat-card>
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
    return stripped.length > 160 ? stripped.slice(0, 160) + '…' : stripped;
  });
}

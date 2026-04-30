import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import type { ResourceListItem } from './knowledge.types';

@Component({
  selector: 'cce-resource-card',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, MatCardModule, MatIconModule, TranslateModule],
  template: `
    <a class="cce-resource-card" [routerLink]="['/knowledge-center', resource().id]">
      <mat-card>
        <mat-card-header>
          <mat-icon mat-card-avatar>{{ iconFor(resource().resourceType) }}</mat-icon>
          <mat-card-title>{{ title() }}</mat-card-title>
          <mat-card-subtitle>
            {{ ('resources.type.' + resource().resourceType) | translate }}
          </mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p class="cce-resource-card__meta">
            <span>{{ 'resources.viewCount' | translate: { count: resource().viewCount } }}</span>
            @if (resource().publishedOn) {
              <span class="cce-resource-card__date">
                · {{ resource().publishedOn | date: 'mediumDate' }}
              </span>
            }
          </p>
        </mat-card-content>
      </mat-card>
    </a>
  `,
  styleUrl: './resource-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceCardComponent {
  readonly resource = input.required<ResourceListItem>();
  readonly locale = input<'ar' | 'en'>('en');

  readonly title = computed(() =>
    this.locale() === 'ar' ? this.resource().titleAr : this.resource().titleEn,
  );

  iconFor(type: ResourceListItem['resourceType']): string {
    switch (type) {
      case 'Pdf': return 'picture_as_pdf';
      case 'Video': return 'play_circle';
      case 'Image': return 'image';
      case 'Link': return 'link';
      case 'Document': return 'description';
    }
  }
}

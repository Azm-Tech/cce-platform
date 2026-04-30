import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { TranslateModule } from '@ngx-translate/core';
import type { SearchHit, SearchableType } from './search.types';

/**
 * Resolves a search hit to its canonical detail URL, when one exists.
 *
 * v0.1.0 limitation: News + Pages search hits are NOT linked because
 * their detail routes use :slug and SearchHitDto carries only `id`.
 * Phase 9 polish backlog will either extend SearchHitDto with `slug`
 * or add a /news/by-id/:id redirect route.
 */
function resolveDetailLink(hit: SearchHit): string | null {
  switch (hit.type) {
    case 'Events':
      return `/events/${hit.id}`;
    case 'Resources':
      return `/knowledge-center/${hit.id}`;
    case 'KnowledgeMaps':
      // Phase 9 ships a placeholder; deep link not yet available.
      return `/knowledge-maps`;
    case 'News':
    case 'Pages':
      return null;
  }
}

@Component({
  selector: 'cce-search-hit',
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink, MatCardModule, TranslateModule],
  template: `
    @if (detailLink(); as link) {
      <a class="cce-search-hit cce-search-hit--linked" [routerLink]="link">
        <ng-container *ngTemplateOutlet="card" />
      </a>
    } @else {
      <div class="cce-search-hit">
        <ng-container *ngTemplateOutlet="card" />
      </div>
    }

    <ng-template #card>
      <mat-card>
        <mat-card-header>
          <mat-card-title>{{ title() }}</mat-card-title>
          <mat-card-subtitle>
            <span class="cce-search-hit__type">
              {{ ('searchType.' + hit().type) | translate }}
            </span>
            <span
              class="cce-search-hit__score"
              [attr.aria-label]="'search.score' | translate"
            >
              {{ hit().score | number:'1.2-2' }}
            </span>
          </mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p class="cce-search-hit__excerpt">{{ excerpt() }}</p>
        </mat-card-content>
      </mat-card>
    </ng-template>
  `,
  styleUrl: './search-hit.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchHitComponent {
  readonly hit = input.required<SearchHit>();
  readonly locale = input<'ar' | 'en'>('en');

  readonly title = computed(() => {
    const h = this.hit();
    return this.locale() === 'ar' ? h.titleAr : h.titleEn;
  });

  readonly excerpt = computed(() => {
    const h = this.hit();
    const raw = this.locale() === 'ar' ? h.excerptAr : h.excerptEn;
    const stripped = raw.replace(/<[^>]*>/g, '').trim();
    return stripped.length > 200 ? stripped.slice(0, 200) + '…' : stripped;
  });

  readonly detailLink = computed(() => resolveDetailLink(this.hit()));

  // Re-export for template/spec use.
  static linkFor(hit: SearchHit): string | null {
    return resolveDetailLink(hit);
  }
}

export type { SearchableType };

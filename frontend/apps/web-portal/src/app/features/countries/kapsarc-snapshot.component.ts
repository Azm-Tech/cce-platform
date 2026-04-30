import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { TranslateModule } from '@ngx-translate/core';
import type { KapsarcSnapshot } from './country.types';

@Component({
  selector: 'cce-kapsarc-snapshot',
  standalone: true,
  imports: [CommonModule, DatePipe, DecimalPipe, MatCardModule, TranslateModule],
  template: `
    <mat-card class="cce-kapsarc-snapshot">
      <mat-card-header>
        <mat-card-title>{{ 'kapsarc.title' | translate }}</mat-card-title>
        <mat-card-subtitle>
          {{ 'kapsarc.snapshotTakenOn' | translate }}:
          {{ snapshot().snapshotTakenOn | date:'mediumDate' }}
        </mat-card-subtitle>
      </mat-card-header>
      <mat-card-content>
        <dl class="cce-kapsarc-snapshot__metrics">
          <div class="cce-kapsarc-snapshot__metric">
            <dt>{{ 'kapsarc.classification' | translate }}</dt>
            <dd>
              <span class="cce-kapsarc-snapshot__badge">{{ snapshot().classification }}</span>
            </dd>
          </div>
          <div class="cce-kapsarc-snapshot__metric">
            <dt>{{ 'kapsarc.performanceScore' | translate }}</dt>
            <dd>{{ snapshot().performanceScore | number:'1.2-2' }}</dd>
          </div>
          <div class="cce-kapsarc-snapshot__metric">
            <dt>{{ 'kapsarc.totalIndex' | translate }}</dt>
            <dd>{{ snapshot().totalIndex | number:'1.2-2' }}</dd>
          </div>
        </dl>
        @if (snapshot().sourceVersion) {
          <p class="cce-kapsarc-snapshot__source">
            <small>
              {{ 'kapsarc.sourceVersion' | translate }}: {{ snapshot().sourceVersion }}
            </small>
          </p>
        }
      </mat-card-content>
    </mat-card>
  `,
  styleUrl: './kapsarc-snapshot.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KapsarcSnapshotComponent {
  readonly snapshot = input.required<KapsarcSnapshot>();
  readonly locale = input<'ar' | 'en'>('en');
}

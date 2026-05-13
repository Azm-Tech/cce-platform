import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { TranslateModule } from '@ngx-translate/core';
import type { KapsarcSnapshot } from './country.types';

interface SubScoreRow {
  key: 'power' | 'industry' | 'transport' | 'buildings' | 'landUse';
  label: string;
  icon: string;
  value: number;
}

@Component({
  selector: 'cce-kapsarc-snapshot',
  standalone: true,
  imports: [CommonModule, DatePipe, DecimalPipe, MatCardModule, TranslateModule],
  template: `
    <div class="cce-kapsarc-snapshot">
      <header class="cce-kapsarc-snapshot__header">
        <span class="cce-kapsarc-snapshot__badge">{{ snapshot().classification }}</span>
        @if (trendYoY() !== null) {
          <span
            class="cce-kapsarc-snapshot__trend"
            [class.cce-kapsarc-snapshot__trend--up]="(trendYoY()! > 0)"
            [class.cce-kapsarc-snapshot__trend--down]="(trendYoY()! < 0)">
            {{ trendYoY()! > 0 ? '▲' : trendYoY()! < 0 ? '▼' : '◆' }}
            {{ trendYoY()! | number:'1.1-1' }} YoY
          </span>
        }
        @if (snapshot().regionalRank && snapshot().regionalCohortSize) {
          <span class="cce-kapsarc-snapshot__rank">
            #{{ snapshot().regionalRank }} of {{ snapshot().regionalCohortSize }} in cohort
          </span>
        }
      </header>

      <!-- Top-line metrics -->
      <dl class="cce-kapsarc-snapshot__metrics">
        <div class="cce-kapsarc-snapshot__metric">
          <dt>Performance</dt>
          <dd>
            {{ snapshot().performanceScore | number:'1.1-1' }}
            <span class="cce-kapsarc-snapshot__metric-unit">/ 100</span>
          </dd>
        </div>
        <div class="cce-kapsarc-snapshot__metric">
          <dt>Total Index</dt>
          <dd>
            {{ snapshot().totalIndex | number:'1.1-1' }}
            <span class="cce-kapsarc-snapshot__metric-unit">/ 100</span>
          </dd>
        </div>
        @if (snapshot().renewableSharePct !== undefined) {
          <div class="cce-kapsarc-snapshot__metric">
            <dt>Renewable Share</dt>
            <dd>
              {{ snapshot().renewableSharePct! | number:'1.1-1' }}<span class="cce-kapsarc-snapshot__metric-unit">%</span>
            </dd>
          </div>
        }
        @if (snapshot().energyIntensity !== undefined) {
          <div class="cce-kapsarc-snapshot__metric">
            <dt>Energy Intensity</dt>
            <dd>
              {{ snapshot().energyIntensity! | number:'1.2-2' }}
              <span class="cce-kapsarc-snapshot__metric-unit">TJ/$M GDP</span>
            </dd>
          </div>
        }
        @if (snapshot().carbonIntensity !== undefined) {
          <div class="cce-kapsarc-snapshot__metric">
            <dt>Carbon Intensity</dt>
            <dd>
              {{ snapshot().carbonIntensity! | number:'1.0-0' }}
              <span class="cce-kapsarc-snapshot__metric-unit">tCO₂e/$M GDP</span>
            </dd>
          </div>
        }
      </dl>

      <!-- Sub-dimension scores -->
      @if (subScoreRows().length > 0) {
        <div class="cce-kapsarc-snapshot__sub-header">
          <span>By dimension</span>
          <small>0–100</small>
        </div>
        <ul class="cce-kapsarc-snapshot__sub-scores" role="list">
          @for (row of subScoreRows(); track row.key) {
            <li class="cce-kapsarc-snapshot__sub-row">
              <span class="cce-kapsarc-snapshot__sub-icon" aria-hidden="true">{{ row.icon }}</span>
              <span class="cce-kapsarc-snapshot__sub-label">{{ row.label }}</span>
              <div class="cce-kapsarc-snapshot__sub-bar" [attr.aria-label]="row.label + ' score ' + row.value">
                <div
                  class="cce-kapsarc-snapshot__sub-bar-fill"
                  [class.cce-kapsarc-snapshot__sub-bar-fill--high]="row.value >= 75"
                  [class.cce-kapsarc-snapshot__sub-bar-fill--mid]="row.value >= 50 && row.value < 75"
                  [class.cce-kapsarc-snapshot__sub-bar-fill--low]="row.value < 50"
                  [style.width.%]="row.value">
                </div>
              </div>
              <span class="cce-kapsarc-snapshot__sub-value">{{ row.value }}</span>
            </li>
          }
        </ul>
      }

      <p class="cce-kapsarc-snapshot__source">
        Snapshot · {{ snapshot().snapshotTakenOn | date:'mediumDate' }}
        @if (snapshot().sourceVersion) {
          · {{ snapshot().sourceVersion }}
        }
      </p>
    </div>
  `,
  styleUrl: './kapsarc-snapshot.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KapsarcSnapshotComponent {
  readonly snapshot = input.required<KapsarcSnapshot>();
  readonly locale = input<'ar' | 'en'>('en');

  readonly trendYoY = computed<number | null>(() => {
    const v = this.snapshot().trendYoY;
    return v === undefined || v === null ? null : v;
  });

  readonly subScoreRows = computed<SubScoreRow[]>(() => {
    const sub = this.snapshot().subScores;
    if (!sub) return [];
    return [
      { key: 'power',     label: 'Power',     icon: '⚡', value: Math.round(sub.power) },
      { key: 'industry',  label: 'Industry',  icon: '🏭', value: Math.round(sub.industry) },
      { key: 'transport', label: 'Transport', icon: '🚆', value: Math.round(sub.transport) },
      { key: 'buildings', label: 'Buildings', icon: '🏢', value: Math.round(sub.buildings) },
      { key: 'landUse',   label: 'Land Use',  icon: '🌳', value: Math.round(sub.landUse) },
    ];
  });
}

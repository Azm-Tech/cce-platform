import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

/** Semantic colour tones, mapped to design tokens in the component styles. */
export type StatusBadgeTone = 'success' | 'warning' | 'danger' | 'info' | 'brand' | 'neutral';

/** Display config for one status value: its colour tone + i18n label key. */
export interface StatusBadgeEntry {
  tone: StatusBadgeTone;
  labelKey: string;
}

/**
 * Maps each status value to its badge config. Define one of these next to a
 * status enum/type so the colour lives with the status definition, e.g.:
 *
 *   export const EXPERT_STATUS_BADGES: StatusBadgeConfig = {
 *     pending:  { tone: 'warning', labelKey: 'experts.status.pending' },
 *     approved: { tone: 'success', labelKey: 'experts.status.approved' },
 *     rejected: { tone: 'danger',  labelKey: 'experts.status.rejected' },
 *   };
 */
export type StatusBadgeConfig = Record<string, StatusBadgeEntry>;

/**
 * Reusable coloured status pill. Give it a `value` (any status string, any
 * casing) and a `config` map; it renders a token-coloured badge with the
 * localized label. Unknown values fall back to a neutral badge showing the raw
 * value, so it degrades gracefully across enums.
 */
@Component({
  selector: 'cce-status-badge',
  standalone: true,
  imports: [TranslocoModule],
  template: `
    <span class="cce-status-badge cce-status-badge--{{ entry().tone }}">
      {{ entry().labelKey | transloco }}
    </span>
  `,
  styles: [
    `
      :host {
        display: inline-flex;
      }

      .cce-status-badge {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 2px 10px;
        border-radius: 999px;
        font-size: 0.78rem;
        font-weight: 600;
        line-height: 1.7;
        white-space: nowrap;
      }

      /* Leading dot in the badge's own colour for a stronger at-a-glance cue. */
      .cce-status-badge::before {
        content: '';
        flex: none;
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background: currentColor;
      }

      .cce-status-badge--success {
        background: rgba(var(--success--600-rgb), 0.12);
        color: var(--success--700);
      }

      .cce-status-badge--warning {
        background: rgba(var(--warning--500-rgb), 0.16);
        color: var(--warning--700);
      }

      .cce-status-badge--danger {
        background: rgba(var(--danger--500-rgb), 0.12);
        color: var(--danger--600);
      }

      .cce-status-badge--info {
        background: rgba(var(--info--500-rgb), 0.12);
        color: var(--info--600);
      }

      .cce-status-badge--brand {
        background: rgba(var(--color-brand-rgb), 0.12);
        color: var(--color-brand);
      }

      .cce-status-badge--neutral {
        background: var(--neutrals--100);
        color: var(--neutrals--600);
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadgeComponent {
  /** The status value to display (matched against `config`, case-insensitively). */
  readonly value = input<string | null | undefined>('');

  /** Status-value → {tone, labelKey} map (define it beside the status enum). */
  readonly config = input<StatusBadgeConfig>({});

  /** Resolved entry: exact key, then case-insensitive, else a neutral fallback
   *  that shows the raw value. */
  readonly entry = computed<StatusBadgeEntry>(() => {
    const value = (this.value() ?? '').toString();
    const config = this.config();
    const hit = config[value] ?? this.caseInsensitive(config, value);
    return hit ?? { tone: 'neutral', labelKey: value };
  });

  private caseInsensitive(config: StatusBadgeConfig, value: string): StatusBadgeEntry | undefined {
    const lower = value.toLowerCase();
    const key = Object.keys(config).find((k) => k.toLowerCase() === lower);
    return key ? config[key] : undefined;
  }
}

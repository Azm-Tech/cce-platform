import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

/** A single quick-start step displayed in the optional steps strip. */
export interface HeroStep {
  /** Display number ("1", "2", "3"…). Drives the colored badge gradient. */
  num: string;
  /** Already-translated step label. */
  label: string;
  /** Optional already-translated one-line description shown under the label. */
  desc?: string;
}

/**
 * Big centered hero banner extracted from the Interactive City scenario
 * builder. Used by workflow / workbench pages that benefit from a richer
 * landing-style header.
 *
 * Visual design:
 *  - Pill **eyebrow** (small ALL-CAPS text + a pulsing brand-green dot).
 *  - Large **gradient title** (deep-green → mid-green → gold).
 *  - Optional **subtitle** capped at ~660 px.
 *  - Optional **3-step quick-start strip** — a row of cards each with a
 *    colored numbered badge (gradient cycles by step index: green → amber →
 *    indigo) + label + description. Only rendered when `steps` is non-empty.
 *
 * Strings are passed in already-translated. Consumers handle i18n.
 *
 * @example
 * ```html
 * <cce-workbench-hero
 *   [eyebrow]="'news.eyebrow' | translate"
 *   [title]="'news.title' | translate"
 *   [subtitle]="'news.subtitle' | translate"
 * />
 *
 * <cce-workbench-hero
 *   eyebrow="INTERACTIVE CITY · SCENARIO BUILDER"
 *   [title]="'interactiveCity.builder.title' | translate"
 *   [subtitle]="'interactiveCity.builder.subtitle' | translate"
 *   [steps]="steps"
 * />
 * ```
 */
@Component({
  selector: 'cce-workbench-hero',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="cce-workbench-hero">
      <span class="cce-workbench-hero__eyebrow">
        <span class="cce-workbench-hero__eyebrow-dot" aria-hidden="true"></span>
        {{ eyebrow() }}
      </span>

      <h1 class="cce-workbench-hero__title">
        <span class="cce-workbench-hero__title-grad">{{ title() }}</span>
      </h1>

      @if (subtitle()) {
        <p class="cce-workbench-hero__subtitle">{{ subtitle() }}</p>
      }

      @if (steps().length > 0) {
        <ol class="cce-workbench-hero__steps">
          @for (step of steps(); track step.num) {
            <li class="cce-workbench-hero__step" [attr.data-step]="step.num">
              <span class="cce-workbench-hero__step-num">{{ step.num }}</span>
              <span class="cce-workbench-hero__step-text">
                <span class="cce-workbench-hero__step-label">{{ step.label }}</span>
                @if (step.desc) {
                  <span class="cce-workbench-hero__step-desc">{{ step.desc }}</span>
                }
              </span>
            </li>
          }
        </ol>
      }
    </header>
  `,
  styles: [`
    :host {
      display: block;
      margin-bottom: 2rem;
    }

    .cce-workbench-hero {
      padding-bottom: 1.5rem;
      border-bottom: 1px solid rgba(0, 0, 0, 0.08);
    }

    /* ─── Eyebrow chip with pulsing dot ───────────────────── */
    .cce-workbench-hero__eyebrow {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.7rem;
      font-weight: 800;
      letter-spacing: 0.18em;
      color: #006c4f;
      margin-bottom: 1rem;
      padding: 0.4rem 0.85rem;
      background: rgba(15, 139, 108, 0.08);
      border: 1px solid rgba(0, 108, 79, 0.18);
      border-radius: 999px;
      text-transform: uppercase;
    }

    .cce-workbench-hero__eyebrow-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #14b88f;
      box-shadow: 0 0 8px rgba(20, 184, 143, 0.65);
      animation: cceWorkbenchHeroDotPulse 2.4s ease-in-out infinite;
    }
    @keyframes cceWorkbenchHeroDotPulse {
      0%, 100% { opacity: 0.85; transform: scale(1); }
      50%      { opacity: 1; transform: scale(1.3); }
    }

    /* ─── Title + subtitle ────────────────────────────────── */
    .cce-workbench-hero__title {
      margin: 0 0 0.55rem;
      font-size: clamp(2rem, 4.5vw, 3.4rem);
      font-weight: 900;
      letter-spacing: -0.025em;
      line-height: 1;
      color: #1c2724;
    }
    .cce-workbench-hero__title-grad {
      background: linear-gradient(135deg, #006c4f 0%, #14b88f 50%, #c8a045 100%);
      -webkit-background-clip: text;
              background-clip: text;
      -webkit-text-fill-color: transparent;
    }

    .cce-workbench-hero__subtitle {
      margin: 0 0 1.5rem;
      font-size: 1.05rem;
      color: rgba(28, 39, 36, 0.62);
      line-height: 1.55;
      max-width: 660px;
    }

    /* ─── Quick-start steps strip (optional) ──────────────── */
    .cce-workbench-hero__steps {
      list-style: none;
      margin: 0;
      padding: 0;
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 0.65rem;
    }
    .cce-workbench-hero__step {
      display: grid;
      grid-template-columns: 38px 1fr;
      align-items: center;
      gap: 0.85rem;
      padding: 0.85rem 1rem;
      background: #ffffff;
      border: 1px solid rgba(0, 0, 0, 0.06);
      border-radius: 14px;
      box-shadow: 0 4px 14px -10px rgba(0, 48, 31, 0.10);
      transition: border-color 0.2s ease, transform 0.2s ease, box-shadow 0.2s ease;
    }
    .cce-workbench-hero__step:hover {
      border-color: rgba(0, 108, 79, 0.25);
      transform: translateY(-1px);
      box-shadow: 0 10px 22px -10px rgba(0, 48, 31, 0.18);
    }

    .cce-workbench-hero__step-num {
      width: 38px;
      height: 38px;
      border-radius: 12px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      font-weight: 800;
      font-size: 1.1rem;
      letter-spacing: -0.01em;
      color: #ffffff;
      background: linear-gradient(135deg, #006c4f, #14b88f);
      box-shadow:
        0 1px 0 rgba(255, 255, 255, 0.25) inset,
        0 6px 14px -4px rgba(15, 139, 108, 0.45);
    }
    .cce-workbench-hero__step[data-step="2"] .cce-workbench-hero__step-num {
      background: linear-gradient(135deg, #d97706, #fbbf24);
      box-shadow:
        0 1px 0 rgba(255, 255, 255, 0.25) inset,
        0 6px 14px -4px rgba(217, 119, 6, 0.40);
    }
    .cce-workbench-hero__step[data-step="3"] .cce-workbench-hero__step-num {
      background: linear-gradient(135deg, #4f46e5, #818cf8);
      box-shadow:
        0 1px 0 rgba(255, 255, 255, 0.25) inset,
        0 6px 14px -4px rgba(79, 70, 229, 0.40);
    }

    .cce-workbench-hero__step-text {
      display: flex;
      flex-direction: column;
      gap: 0.1rem;
      min-width: 0;
    }
    .cce-workbench-hero__step-label {
      font-weight: 700;
      font-size: 0.92rem;
      color: #1c2724;
      letter-spacing: -0.005em;
      line-height: 1.2;
    }
    .cce-workbench-hero__step-desc {
      font-size: 0.78rem;
      color: rgba(28, 39, 36, 0.55);
      line-height: 1.3;
    }

    @media (prefers-reduced-motion: reduce) {
      .cce-workbench-hero__eyebrow-dot,
      .cce-workbench-hero__step {
        animation: none !important;
        transition: none !important;
        transform: none !important;
      }
    }
  `],
})
export class WorkbenchHeroComponent {
  /** Already-translated eyebrow label. ALL-CAPS recommended. Required. */
  readonly eyebrow = input.required<string>();
  /** Already-translated H1 title. Rendered with brand gradient. Required. */
  readonly title = input.required<string>();
  /** Already-translated subtitle copy. Optional. */
  readonly subtitle = input<string>('');
  /** Optional 3-step quick-start strip. Pass an empty array (default) to hide. */
  readonly steps = input<readonly HeroStep[]>([]);
}
